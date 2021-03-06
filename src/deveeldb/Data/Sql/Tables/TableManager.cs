﻿// 
//  Copyright 2010-2018 Deveel
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
// 
//        http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Deveel.Data.Events;
using Deveel.Data.Transactions;

namespace Deveel.Data.Sql.Tables {
	public sealed class TableManager : IDbObjectManager {
		private readonly List<ITableContainer> tableContainers;
		private readonly Dictionary<ObjectName, ITable> tableCache;

		public TableManager(ITransaction transaction, ITableSystem tableSystem) {
			Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
			TableSystem = tableSystem ?? throw new ArgumentNullException(nameof(tableSystem));

			tableContainers = transaction.GetServices<ITableContainer>().ToList();

			tableCache = new Dictionary<ObjectName, ITable>();
		}

		DbObjectType IDbObjectManager.ObjectType => DbObjectType.Table;

		public ITransaction Transaction { get; }

		public ITableSystem TableSystem { get; }

		private TableInfo AssertTableInfo(IDbObjectInfo objectInfo) {
			if (objectInfo == null)
				throw new ArgumentNullException(nameof(objectInfo));
			if (!(objectInfo is TableInfo))
				throw new ArgumentException("The specified object info is invalid.");

			return (TableInfo) objectInfo;
		}

		private bool IsDynamic(ObjectName tableName)
			=> tableContainers.Any(x => x.ContainsTable(tableName));

		private ITable GetDynamicTable(ObjectName tableName) {
			foreach (var container in tableContainers) {
				var index = container.IndexOfTable(tableName);

				if (index != -1)
					return container.GetTable(index);
			}

			throw new ArgumentException($"Table '{tableName}' is not dynamic");
		}

		private string GetDynamicType(ObjectName tableName) {
			foreach (var container in tableContainers) {
				var index = container.IndexOfTable(tableName);

				if (index != -1)
					return container.GetTableType(index);
			}

			throw new ArgumentException($"Table '{tableName}' is not dynamic");

		}

		private TableInfo GetDynamicInfo(ObjectName tableName) {
			foreach (var container in tableContainers) {
				var index = container.IndexOfTable(tableName);

				if (index != -1)
					return container.GetTableInfo(index);
			}

			throw new ArgumentException($"Table '{tableName}' is not dynamic");

		}

		private IEnumerable<ObjectName> GetDynamicTableNames() {
			foreach (var container in tableContainers) {
				var count = container.TableCount;

				for (int i = 0; i < count; i++) {
					yield return container.GetTableName(i);
				}
			}
		}

		#region IDbObjectManager

		Task IDbObjectManager.CreateObjectAsync(IDbObjectInfo objInfo) {
			CreateTable(AssertTableInfo(objInfo));
			return Task.CompletedTask;
		}

		Task<bool> IDbObjectManager.RealObjectExistsAsync(ObjectName objName) {
			return Task.FromResult(RealTableExists(objName));
		}

		Task<bool> IDbObjectManager.ObjectExistsAsync(ObjectName objName) {
			return Task.FromResult(TableExists(objName));
		}

		Task<IDbObjectInfo> IDbObjectManager.GetObjectInfoAsync(ObjectName objectName) {
			return Task.FromResult<IDbObjectInfo>(GetTableInfo(objectName));
		}

		async Task<IDbObject> IDbObjectManager.GetObjectAsync(ObjectName objName) {
			return await GetTableAsync(objName);
		}

		Task<bool> IDbObjectManager.AlterObjectAsync(IDbObjectInfo objInfo) {
			return AlterTableAsync(AssertTableInfo(objInfo));
		}

		Task<bool> IDbObjectManager.DropObjectAsync(ObjectName objName) {
			return DropTableAsync(objName);
		}

		#endregion

		public bool RealTableExists(ObjectName tableName)
			=> Transaction.State.IsTableVisible(tableName);

		public bool TableExists(ObjectName tableName)
			=> IsDynamic(tableName) || RealTableExists(tableName);

		public string GetTableType(ObjectName tableName) {
			if (tableName == null)
				throw new ArgumentNullException(nameof(tableName));

			if (IsDynamic(tableName))
				return GetDynamicType(tableName);
			if (Transaction.State.IsTableVisible(tableName))
				return "TABLE";

			// No table found so report the error.
			throw new ArgumentException();
		}

		public TableInfo GetTableInfo(ObjectName tableName) {
			if (IsDynamic(tableName))
				return GetDynamicInfo(tableName);

			if (!Transaction.State.TryGetVisibleTable(tableName, out var source))
				return null;

			return source.TableInfo;
		}

		public async Task<ITable> GetTableAsync(ObjectName tableName) {
			if (tableCache.TryGetValue(tableName, out var table))
				return table;

			if (!Transaction.State.TryGetVisibleTable(tableName, out var source)) {
				if (IsDynamic(tableName))
					return GetDynamicTable(tableName);
			} else {
				table = await source.GetMutableTableAsync(Transaction);

				Transaction.State.AccessTable(table);

				Transaction.RaiseEvent<TableAccessedEvent>(source.TableInfo.TableName, source.TableId);

				tableCache[tableName] = table;
			}

			return table;
		}

		public void CreateTable(TableInfo tableInfo) {
			CreateTable(tableInfo, false);
		}

		public void CreateTable(TableInfo tableInfo, bool temporary) {
			if (tableInfo == null) throw new ArgumentNullException(nameof(tableInfo));

			if (Transaction.State.IsTableVisible(tableInfo.TableName))
				throw new ArgumentException($"Table '{tableInfo.TableName}' already exists");

			var source = TableSystem.CreateTableSource(tableInfo, temporary);

			Transaction.State.AddVisibleTable(source, source.CreateRowIndexSet());

			Transaction.RaiseEvent<TableCreatedEvent>(source.TableId, tableInfo.TableName);

			// TODO: Create a native sequence explicitly? Or delegate to Event Consume?
		}

		public Task<bool> AlterTableAsync(TableInfo tableInfo) {
			throw new NotImplementedException();
		}

		public async Task<ObjectName> ResolveNameAsync(ObjectName tableName, bool ignoreCase) {
			var resolved = Transaction.State.VisibleTables.FirstOrDefault(x => x.Equals(tableName, ignoreCase));

			if (resolved != null)
				return resolved;

			resolved = GetDynamicTableNames().FirstOrDefault(x => x.Equals(tableName, ignoreCase));

			if (resolved != null)
				return resolved;

			return null;
		}

		public async Task<bool> DropTableAsync(ObjectName tableName) {
			if (!Transaction.State.TryGetVisibleTable(tableName, out var source))
				return false;

			// Removes this table from the visible table list of this transaction
			Transaction.State.RemoveVisibleTable(source);
			tableCache.Remove(tableName);

			// Log in the journal that this transaction touched the table_id.
			int tableId = source.TableId;
			Transaction.RaiseEvent<TableDroppedEvent>(tableId, tableName);

			// TODO: Remove the native sequence explicitly? Or delegate to events?

			return true;
		}

		public void SelectTable(ObjectName tableName) {
			if (IsDynamic(tableName))
				return;

			if (!Transaction.State.TryGetVisibleTable(tableName, out var source))
				throw new InvalidOperationException();

			Transaction.State.SelectTable(source);
		}

		#region Native Sequences

		public async Task<SqlNumber> SetUniqueIdAsync(ObjectName tableName, SqlNumber value) {
			if (!Transaction.State.TryGetVisibleTable(tableName, out var tableSource))
				throw new InvalidOperationException($"Table with name '{tableName}' could not be found.");

			await tableSource.SetUniqueIdAsync((long) value);
			return value;
		}

		public async Task<SqlNumber> GetNextUniqueIdAsync(ObjectName tableName) {
			if (!Transaction.State.TryGetVisibleTable(tableName, out var tableSource))
				throw new InvalidOperationException($"Table with name '{tableName}' could not be found.");

			var value = await tableSource.GetNextUniqueIdAsync();
			return new SqlNumber(value);
		}

		public async Task<SqlNumber> GetCurrentUniqueIdAsync(ObjectName tableName) {
			if (!Transaction.State.TryGetVisibleTable(tableName, out var tableSource))
				throw new InvalidOperationException($"Table with name '{tableName}' could not be found.");

			var value = await tableSource.GetCurrentUniqueIdAsync();
			return new SqlNumber(value);
		}


		#endregion

		public void Dispose() {
			
		}
	}
}