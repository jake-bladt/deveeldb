﻿using System;
using System.Collections.Generic;
using System.Linq;

using Deveel.Data.Security;
using Deveel.Data.Sql.Expressions;
using Deveel.Data.Sql.Objects;
using Deveel.Data.Sql.Query;
using Deveel.Data.Types;

namespace Deveel.Data.Sql.Tables {
	public static partial class QueryExtensions {
		public static int DeleteFrom(this IQuery context, ObjectName tableName, SqlQueryExpression query) {
			return DeleteFrom(context, tableName, query, -1);
		}

		public static int DeleteFrom(this IQuery context, ObjectName tableName, SqlExpression expression) {
			return DeleteFrom(context, tableName, expression, -1);
		}

		public static int DeleteFrom(this IQuery context, ObjectName tableName, SqlExpression expression, int limit) {
			if (expression is SqlQueryExpression)
				return context.DeleteFrom(tableName, (SqlQueryExpression)expression, limit);

			var table = context.GetMutableTable(tableName);
			if (table == null)
				throw new ObjectNotFoundException(tableName);

			var queryExpression = new SqlQueryExpression(new List<SelectColumn> { SelectColumn.Glob("*") });
			queryExpression.FromClause.AddTable(tableName.Name);
			queryExpression.WhereExpression = expression;

			var planExpression = queryExpression.Evaluate(context.QueryContext, null);
			var plan = (SqlQueryObject)((SqlConstantExpression)planExpression).Value.Value;
			var deleteSet = plan.QueryPlan.Evaluate(context.QueryContext);

			return context.DeleteFrom(tableName, deleteSet, limit);
		}

		public static int DeleteFrom(this IQuery context, ObjectName tableName, SqlQueryExpression query, int limit) {
			IQueryPlanNode plan;

			try {
				var planValue = query.EvaluateToConstant(context.QueryContext, null);
				if (planValue == null)
					throw new InvalidOperationException();

				if (!(planValue.Type is QueryType))
					throw new InvalidOperationException();

				plan = ((SqlQueryObject)planValue.Value).QueryPlan;
			} catch (QueryException) {
				throw;
			} catch (SecurityException) {
				throw;
			} catch (Exception ex) {
				throw new InvalidOperationException(String.Format("Could not delete from table '{0}': unable to form the delete set.", tableName), ex);
			}

			var deleteSet = plan.Evaluate(context.QueryContext);
			return context.DeleteFrom(tableName, deleteSet, limit);
		}

		public static int DeleteFrom(this IQuery context, ObjectName tableName, ITable deleteSet, int limit) {
			if (!context.UserCanDeleteFromTable(tableName))
				throw new MissingPrivilegesException(context.UserName(), tableName, Privileges.Delete);

			var table = context.GetMutableTable(tableName);
			if (table == null)
				throw new ObjectNotFoundException(tableName);

			return table.Delete(deleteSet, limit);
		}

		public static int UpdateTable(this IQuery context, ObjectName tableName, IQueryPlanNode queryPlan,
			IEnumerable<SqlAssignExpression> assignments, int limit) {
			var columnNames = assignments.Select(x => x.ReferenceExpression)
				.Cast<SqlReferenceExpression>()
				.Select(x => x.ReferenceName.Name).ToArray();

			if (!context.UserCanUpdateTable(tableName, columnNames))
				throw new MissingPrivilegesException(context.UserName(), tableName, Privileges.Update);

			if (!context.UserCanSelectFromPlan(queryPlan))
				throw new InvalidOperationException();

			var table = context.GetMutableTable(tableName);
			if (table == null)
				throw new ObjectNotFoundException(tableName);

			var updateSet = queryPlan.Evaluate(context.QueryContext);
			return table.Update(context.QueryContext, updateSet, assignments, limit);
		}

		public static void InsertIntoTable(this IQuery context, ObjectName tableName, IEnumerable<SqlAssignExpression> assignments) {
			var columnNames =
				assignments.Select(x => x.ReferenceExpression)
					.Cast<SqlReferenceExpression>()
					.Select(x => x.ReferenceName.Name).ToArray();
			if (!context.UserCanInsertIntoTable(tableName, columnNames))
				throw new MissingPrivilegesException(context.UserName(), tableName, Privileges.Insert);

			var table = context.GetMutableTable(tableName);

			var row = table.NewRow();
			foreach (var expression in assignments) {
				row.EvaluateAssignment(expression, context.QueryContext);
			}

			table.AddRow(row);
		}

		public static int InsertIntoTable(this IQuery context, ObjectName tableName, IEnumerable<SqlAssignExpression[]> assignments) {
			int insertCount = 0;

			foreach (var assignment in assignments) {
				context.InsertIntoTable(tableName, assignment);
				insertCount++;
			}

			return insertCount;
		}

	}
}