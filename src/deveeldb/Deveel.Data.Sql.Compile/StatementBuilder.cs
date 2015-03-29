﻿using System;
using System.Collections.Generic;
using System.Linq;

using Deveel.Data.Deveel.Data.Sql.Compile;
using Deveel.Data.Sql.Expressions;
using Deveel.Data.Sql.Statements;
using Deveel.Data.Types;

namespace Deveel.Data.Sql.Compile {
	public sealed class StatementBuilder : SqlNodeVisitor {
		private readonly IDataTypeResolver typeResolver;
		private readonly List<StatementTree> statements;

		public StatementBuilder() 
			: this(null) {
		}

		public StatementBuilder(IDataTypeResolver typeResolver) {
			this.typeResolver = typeResolver;
			statements = new List<StatementTree>();
		}

		private SqlExpression VisitExpression(IExpressionNode node) {
			var visitor = new ExpressionBuilder();
			return visitor.Build(node);
		}

		protected override void VisitNode(ISqlNode node) {
			if (node is CreateTableNode)
				VisitCreateTable((CreateTableNode) node);
			if (node is CreateViewNode)
				VisitCreateView((CreateViewNode) node);
			if (node is CreateTriggerNode)
				VisitCreateTrigger((CreateTriggerNode) node);

			if (node is SelectStatementNode)
				VisitSelect((SelectStatementNode) node);

			if (node is SequenceOfStatementsNode)
				VisitSequenceOfStatements((SequenceOfStatementsNode) node);

			base.VisitNode(node);
		}

		private void VisitSequenceOfStatements(SequenceOfStatementsNode node) {
			foreach (var statementNode in node.Statements) {
				VisitNode(statementNode);
			}
		}

		private void VisitSelect(SelectStatementNode node) {
			var queryExpression = (SqlQueryExpression)VisitExpression(node.QueryExpression);
			var tree = new StatementTree(typeof(SelectStatement));
			tree.SetValue(SelectStatement.Keys.QueryExpression, queryExpression);
			statements.Add(tree);
		}

		private void VisitCreateTrigger(CreateTriggerNode node) {
			
		}

		private void VisitCreateView(CreateViewNode node) {
			
		}

		private void VisitCreateTable(CreateTableNode node) {
			CreateTable.Build(typeResolver, node, statements);
		}

		public IEnumerable<StatementTree> Build(ISqlNode rootNode, SqlQuery query) {
			VisitNode(rootNode);
			return statements.AsReadOnly();
		}

		public IEnumerable<StatementTree> Build(ISqlNode rootNode, string query) {
			return Build(rootNode, new SqlQuery(query));
		}

		#region CreateTable

		static class CreateTable {
			public static void Build(IDataTypeResolver typeResolver, CreateTableNode node, ICollection<StatementTree> statements) {
				string idColumn = null;

				var dataTypeBuilder = new DataTypeBuilder();

				var tableName = node.TableName;
				var constraints = new List<ConstraintInfo>();
				var columns = new List<ColumnInfo>();

				var expBuilder = new ExpressionBuilder();

				foreach (var column in node.Columns) {
					var dataType = dataTypeBuilder.Build(typeResolver, column.DataType);

					var columnInfo = new ColumnInfo(column.ColumnName, dataType);

					if (column.Default != null)
						columnInfo.DefaultExpression = expBuilder.Build(column.Default);

					if (column.IsIdentity) {
						if (!String.IsNullOrEmpty(idColumn))
							throw new InvalidOperationException(String.Format("Table {0} defines already {1} as identity column.",
								node.TableName, idColumn));

						if (column.Default != null)
							throw new InvalidOperationException(String.Format("The identity column {0} cannot have a DEFAULT constraint.",
								idColumn));

						idColumn = column.ColumnName;

						columnInfo.DefaultExpression = SqlExpression.FunctionCall("UNIQUEKEY",
							new[] {SqlExpression.Constant(node.TableName.FullName)});
					}

					foreach (var constraint in column.Constraints) {
						if (String.Equals(ConstraintTypeNames.Check, constraint.ConstraintType, StringComparison.OrdinalIgnoreCase)) {
							var exp = expBuilder.Build(constraint.CheckExpression);
							constraints.Add(ConstraintInfo.Check(tableName, exp, column.ColumnName));
						} else if (String.Equals(ConstraintTypeNames.ForeignKey, constraint.ConstraintType, StringComparison.OrdinalIgnoreCase)) {
							var fTable = constraint.ReferencedTable.Name;
							var fColumn = constraint.ReferencedColumn.Text;
							var fkey = ConstraintInfo.ForeignKey(tableName, column.ColumnName, fTable, fColumn);
							if (!String.IsNullOrEmpty(constraint.OnDeleteAction))
								fkey.OnDelete = GetForeignKeyAction(constraint.OnDeleteAction);
							if (!String.IsNullOrEmpty(constraint.OnUpdateAction))
								fkey.OnUpdate = GetForeignKeyAction(constraint.OnUpdateAction);

							constraints.Add(fkey);
						} else if (String.Equals(ConstraintTypeNames.PrimaryKey, constraint.ConstraintType, StringComparison.OrdinalIgnoreCase)) {
							constraints.Add(ConstraintInfo.PrimaryKey(tableName, column.ColumnName));
						} else if (String.Equals(ConstraintTypeNames.UniqueKey, constraint.ConstraintType, StringComparison.OrdinalIgnoreCase)) {
							constraints.Add(ConstraintInfo.Unique(tableName, column.ColumnName));
						}
					}

					columns.Add(columnInfo);
				}

				foreach (var constraint in node.Constraints) {
					if (String.Equals(ConstraintTypeNames.Check, constraint.ConstraintType, StringComparison.OrdinalIgnoreCase)) {
						var exp = expBuilder.Build(constraint.CheckExpression);
						constraints.Add(ConstraintInfo.Check(constraint.ConstraintName, tableName, exp, constraint.Columns.ToArray()));
					} else if (String.Equals(ConstraintTypeNames.PrimaryKey, constraint.ConstraintType, StringComparison.OrdinalIgnoreCase)) {
						constraints.Add(ConstraintInfo.PrimaryKey(constraint.ConstraintName, tableName, constraint.Columns.ToArray()));
					} else if (String.Equals(ConstraintTypeNames.UniqueKey, constraint.ConstraintType, StringComparison.OrdinalIgnoreCase)) {
						constraints.Add(ConstraintInfo.Unique(constraint.ConstraintName, tableName, constraint.Columns.ToArray()));
					} else if (String.Equals(ConstraintTypeNames.ForeignKey, constraint.ConstraintType, StringComparison.OrdinalIgnoreCase)) {
						var fTable = constraint.ReferencedTableName.Name;
						var fColumns = constraint.ReferencedColumns;
						var fkey = ConstraintInfo.ForeignKey(constraint.ConstraintName, tableName, constraint.Columns.ToArray(), fTable,
							fColumns.ToArray());
						if (!String.IsNullOrEmpty(constraint.OnDeleteAction))
							fkey.OnDelete = GetForeignKeyAction(constraint.OnDeleteAction);
						if (!String.IsNullOrEmpty(constraint.OnUpdateAction))
							fkey.OnUpdate = GetForeignKeyAction(constraint.OnUpdateAction);

						constraints.Add(fkey);
					}
				}

				//TODO: Optimization: merge same constraints

				statements.Add(MakeCreateTable(tableName, columns, node.IfNotExists, node.Temporary));

				foreach (var constraint in constraints) {
					statements.Add(MakeAlterTableAddConstraint(tableName, constraint));
				}
			}

			private static ForeignKeyAction GetForeignKeyAction(string actionName) {
				if (String.Equals("NO ACTION", actionName, StringComparison.OrdinalIgnoreCase) ||
					String.Equals("NOACTION", actionName, StringComparison.OrdinalIgnoreCase))
					return ForeignKeyAction.NoAction;
				if (String.Equals("CASCADE", actionName, StringComparison.OrdinalIgnoreCase))
					return ForeignKeyAction.Cascade;
				if (String.Equals("SET DEFAULT", actionName, StringComparison.OrdinalIgnoreCase) ||
					String.Equals("SETDEFAULT", actionName, StringComparison.OrdinalIgnoreCase))
					return ForeignKeyAction.SetDefault;
				if (String.Equals("SET NULL", actionName, StringComparison.OrdinalIgnoreCase) ||
					String.Equals("SETNULL", actionName, StringComparison.OrdinalIgnoreCase))
					return ForeignKeyAction.SetNull;

				throw new NotSupportedException();
			}

			private static StatementTree MakeAlterTableAddConstraint(ObjectName tableName, ConstraintInfo constraint) {
				var action = new AddConstraintAction(constraint);

				var tree = new StatementTree(typeof (AlterTableStatement));
				tree.SetValue(AlterTableStatement.Keys.TableName, tableName);
				tree.SetValue(AlterTableStatement.Keys.Actions, new List<IAlterTableAction> {action});
				return tree;
			}

			private static StatementTree MakeCreateTable(ObjectName tableName, IEnumerable<ColumnInfo> columns, bool ifNotExists, bool temporary) {
				var tree = new StatementTree(typeof (CreateTableStatement));
				tree.SetValue(CreateTableStatement.Keys.TableName, tableName);
				tree.SetValue(CreateTableStatement.Keys.Columns, columns.ToList());
				tree.SetValue(CreateTableStatement.Keys.IfNotExists, ifNotExists);
				tree.SetValue(CreateTableStatement.Keys.Temporary, temporary);
				return tree;
			}
		}

		#endregion
	}
}