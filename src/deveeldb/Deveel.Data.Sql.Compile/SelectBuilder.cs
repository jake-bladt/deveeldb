﻿using System;
using System.Linq;

using Antlr4.Runtime.Misc;

using Deveel.Data.Sql.Expressions;
using Deveel.Data.Sql.Statements;

namespace Deveel.Data.Sql.Compile {
	static class SelectBuilder {
		public static SqlStatement Build(PlSqlParser.SelectStatementContext context) {
			IntoClause into;
			var query = Subquery.Form(context.subquery(), out into);

			if (into != null) {
				SqlExpression reference;
				if (into.TableName != null) {
					reference = SqlExpression.Reference(into.TableName);
				} else {
					var vars = into.Variables;
					reference = SqlExpression.Tuple(vars.Select(SqlExpression.VariableReference).Cast<SqlExpression>().ToArray());
				}

				return new SelectIntoStatement(query, reference);
			}

			var statement = new SelectStatement(query);

			var orderBy = context.order_by_clause();
			var forUpdate = context.for_update_clause();

			if (orderBy != null) {
				var sortColumns = orderBy.order_by_elements().Select(x => {
					bool asc = x.DESC() == null;
					var exp = Expression.Build(x.expression());
					return new SortColumn(exp, asc);
				});

				statement.OrderBy = sortColumns;
			}

			if (forUpdate != null) {
				// TODO: support FOR UPDATE in Select
				throw new NotImplementedException();
			}

			var limit = context.queryLimitClause();
			if (limit != null) {
				var n1 = Number.PositiveInteger(limit.n1);
				var n2 = Number.PositiveInteger(limit.n2);

				if (n1 == null)
					throw new ParseCanceledException("Invalid LIMIT clause");

				if (n2 != null) {
					statement.Limit = new QueryLimit(n1.Value, n2.Value);
				} else {
					statement.Limit = new QueryLimit(n1.Value);
				}
			}

			return statement;
		}
	}
}