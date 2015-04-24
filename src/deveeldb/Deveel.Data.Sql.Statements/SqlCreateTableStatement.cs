﻿// 
//  Copyright 2010-2015 Deveel
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

using Deveel.Data.DbSystem;
using Deveel.Data.Security;

namespace Deveel.Data.Sql.Statements {
	[Serializable]
	public sealed class SqlCreateTableStatement : SqlStatement {
		public SqlCreateTableStatement(ObjectName tableName, IEnumerable<SqlTableColumn> columns) {
			TableName = tableName;
			Columns = new List<SqlTableColumn>();
			if (columns != null) {
				foreach (var column in columns) {
					Columns.Add(column);
				}
			}
		}

		public ObjectName TableName { get; private set; }

		public IList<SqlTableColumn> Columns { get; private set; }

		public bool IfNotExists { get; set; }

		public bool Temporary { get; set; }

		public override StatementType StatementType {
			get { return StatementType.CreateTable; }
		}

		//private void VerifyIdentityColumn() {
		//	ColumnInfo idColumn = null;

		//	foreach (var columnInfo in Columns) {
		//		if (IsIdentity(columnInfo)) {
		//			if (idColumn != null)
		//				throw new InvalidOperationException(String.Format("Column {0} is already the identity of the table {1}.",
		//					idColumn.ColumnName, TableName));

		//			idColumn = columnInfo;
		//		}
		//	}

		//	if (idColumn != null) {
		//		if (!(idColumn.ColumnType is NumericType))
		//			throw new InvalidOperationException(String.Format("Identity column '{0}' must be a NUMERIC type."));

		//		var constraints = Constraints;
		//		if (constraints == null ||
		//			constraints.Count == 0)
		//			throw new InvalidOperationException(
		//				String.Format("Identity column '{0}' must be defined in a PRIMARY KEY constraint.", idColumn.ColumnName));
		//	}
		//}

		//private static bool IsIdentity(ColumnInfo columnInfo) {
		//	if (!columnInfo.HasDefaultExpression ||
		//		columnInfo.DefaultExpression.ExpressionType != SqlExpressionType.FunctionCall)
		//		return false;

		//	var functionName = ((SqlFunctionCallExpression) columnInfo.DefaultExpression).FunctioName.Name;
		//	return String.Equals(functionName, "uniquekey", StringComparison.OrdinalIgnoreCase);
		//}

		protected override SqlPreparedStatement PrepareStatement(IQueryContext context) {
			var tableInfo = CreateTableInfo(context);

			return new PreparedCreateTableStatement(tableInfo, IfNotExists, Temporary);
		}

		private TableInfo CreateTableInfo(IQueryContext context) {
			var tableName = TableName;

			// TODO: there are a lot of controls here to do before generating:
			//        1. Verify there are zero-or-one identity columns
			//        2. Assert every column name is well formatted
			//        3. Assert all the columns fall into the table domain
			//        4. Assert that DEFAULT expression of columns have no column
			//           references outside the table domain.

			tableName = context.ResolveTableName(tableName);

			var tableInfo = new TableInfo(tableName);

			foreach (var column in Columns) {
				var columnInfo = CreateColumnInfo(column);
				tableInfo.AddColumn(columnInfo);
			}

			return tableInfo;
		}

		private ColumnInfo CreateColumnInfo(SqlTableColumn column) {
			return new ColumnInfo(column.ColumnName, column.ColumnType) {
				DefaultExpression = column.DefaultExpression,
				IsNotNull = column.IsNotNull
			};
		}

		#region PreparedCreateTableStatement

		[Serializable]
		class PreparedCreateTableStatement : SqlPreparedStatement {
			private readonly TableInfo tableInfo;
			private readonly bool temporary;
			private readonly bool ifNotExists;

			public PreparedCreateTableStatement(TableInfo tableInfo, bool ifNotExists, bool temporary) {
				this.tableInfo = tableInfo;
				this.ifNotExists = ifNotExists;
				this.temporary = temporary;
			}

			public override ITable Evaluate(IQueryContext context) {
				if (!context.UserCanCreateTable(tableInfo.TableName))
					throw new MissingPrivilegesException(tableInfo.TableName,
						String.Format("User '{0}' has not enough privileges to create table '{1}'", context.User(), tableInfo.TableName));

				try {
					context.CreateTable(tableInfo, ifNotExists, temporary);
					return FunctionTable.ResultTable(context, 0);
				} catch (SecurityException ex) {
					throw;
				} catch (Exception ex) {
					// TODO: Send a specialized error
					throw;
				}
			}
		}

		#endregion
	}
}