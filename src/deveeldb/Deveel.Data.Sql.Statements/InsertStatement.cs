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
using System.IO;
using System.Linq;

using Deveel.Data;
using Deveel.Data.Serialization;
using Deveel.Data.Sql.Expressions;

namespace Deveel.Data.Sql.Statements {
	public sealed class InsertStatement : SqlStatement {
		public InsertStatement(string tableName, IEnumerable<string> columnNames, IEnumerable<SqlExpression[]> values) {
			if (columnNames == null)
				throw new ArgumentNullException("columnNames");
			if (values == null)
				throw new ArgumentNullException("values");
			if (String.IsNullOrEmpty(tableName))
				throw new ArgumentNullException("tableName");

			TableName = tableName;
			ColumnNames = columnNames;
			Values = values;
		}

		public string TableName { get; private set; }

		public IEnumerable<string> ColumnNames { get; private set; } 

		public IEnumerable<SqlExpression[]> Values { get; private set; } 

		protected override SqlStatement PrepareStatement(IExpressionPreparer preparer, IQueryContext context) {
			var tableName = context.ResolveTableName(TableName);

			var table = context.GetTable(tableName);
			if (table == null)
				throw new InvalidOperationException();

			if (Values.Any(x => x.OfType<SqlQueryExpression>().Any()))
				throw new InvalidOperationException("Cannot set a value from a query.");

			var columnInfos = new List<ColumnInfo>();
			foreach (var name in ColumnNames) {
				var columnName = ObjectName.Parse(name);
				var colIndex = table.FindColumn(columnName);
				if (colIndex < 0)
					throw new InvalidOperationException(String.Format("Cannot find column '{0}' in table '{1}'", columnName, table.FullName));

				columnInfos.Add(table.TableInfo[colIndex]);
			}

			var assignments = new List<SqlAssignExpression[]>();

			foreach (var valueSet in Values) {
				var valueAssign = new SqlAssignExpression[valueSet.Length];

				for (int i = 0; i < valueSet.Length; i++) {
					var columnInfo = columnInfos[i];

					var value = valueSet[i];
					if (value != null) {
						// TODO: Deference columns with a preparer
					}

					if (value != null) {
						var expReturnType = value.ReturnType(context, null);
						if (!columnInfo.ColumnType.IsComparable(expReturnType))
							throw new InvalidOperationException();
					}

					valueAssign[i] = SqlExpression.Assign(SqlExpression.Reference(columnInfo.FullColumnName), value);
				}

				assignments.Add(valueAssign);
			}

			return new Prepared(tableName, assignments);
		}

		#region Prepared

		internal class Prepared : SqlStatement {
			internal Prepared(ObjectName tableName, IList<SqlAssignExpression[]> assignments) {
				TableName = tableName;
				Assignments = assignments;
			}

			public ObjectName TableName { get; private set; }

			public IList<SqlAssignExpression[]> Assignments { get; private set; }

			protected override bool IsPreparable {
				get { return false; }
			}

			protected override ITable ExecuteStatement(IQueryContext context) {
				var insertCount = context.InsertIntoTable(TableName, Assignments);
				return FunctionTable.ResultTable(context, insertCount);
			}
		}

		#endregion

		#region PreparedSerializer

		internal class PreparedSerializer : ObjectBinarySerializer<Prepared> {
			public override void Serialize(Prepared obj, BinaryWriter writer) {
				ObjectName.Serialize(obj.TableName, writer);

				var setListCount = obj.Assignments.Count;
				writer.Write(setListCount);

				for (int i = 0; i < setListCount; i++) {
					var set = obj.Assignments[i];
					var setCount = set.Length;

					writer.Write(setCount);

					for (int j = 0; j < setCount; j++) {
						SqlExpression.Serialize(obj.Assignments[i][j], writer);
					}
				}
			}

			public override Prepared Deserialize(BinaryReader reader) {
				var tableName = ObjectName.Deserialize(reader);

				var listCount = reader.ReadInt32();

				var setList = new List<SqlAssignExpression[]>(listCount);

				for (int i = 0; i < listCount; i++) {
					var setCount = reader.ReadInt32();

					var exps = new SqlAssignExpression[setCount];

					for (int j = 0; j < setCount; j++) {
						exps[j] = (SqlAssignExpression) SqlExpression.Deserialize(reader);
					}

					setList.Add(exps);
				}

				return new Prepared(tableName, setList);
			}
		}

		#endregion
	}
}
