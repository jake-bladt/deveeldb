﻿using System;

using Deveel.Data.Services;

namespace Deveel.Data.Sql.Tables {
	static class ScopeExtensions {
		public static void UseTables(this IScope scope) {
			scope.Bind<IObjectManager>()
				.To<TableManager>()
				.WithKey(DbObjectType.Table)
				.InTransactionScope();

			scope.Bind<ITableCompositeSetupCallback>()
				.To<TablesInit>()
				.InTransactionScope();

			scope.Bind<ITableCompositeCreateCallback>()
				.To<TablesInit>()
				.InTransactionScope();
		}
	}
}
