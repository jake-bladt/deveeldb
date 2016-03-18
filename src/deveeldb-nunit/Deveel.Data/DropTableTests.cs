﻿using System;

using Deveel.Data.Sql;
using Deveel.Data.Sql.Tables;
using Deveel.Data.Sql.Types;

using NUnit.Framework;

namespace Deveel.Data.Deveel.Data {
	[TestFixture]
	public sealed class DropTableTests : ContextBasedTest {
		protected override void OnSetUp(string testName) {
			CreateTestTables(Query);
		}

		private static void CreateTestTables(IQuery context) {
			var tn1 = ObjectName.Parse("APP.test_table1");
			var tableInfo1 = new TableInfo(tn1);
			tableInfo1.AddColumn(new ColumnInfo("id", PrimitiveTypes.Integer()));
			tableInfo1.AddColumn(new ColumnInfo("name", PrimitiveTypes.String()));
			tableInfo1.AddColumn(new ColumnInfo("date", PrimitiveTypes.DateTime()));
			context.Session.Access.CreateTable(tableInfo1);
			context.Session.Access.AddPrimaryKey(tn1, "id");

			var tn2 = ObjectName.Parse("APP.test_table2");
			var tableInfo2 = new TableInfo(tn2);
			tableInfo2.AddColumn(new ColumnInfo("id", PrimitiveTypes.Integer()));
			tableInfo2.AddColumn(new ColumnInfo("other_id", PrimitiveTypes.Integer()));
			tableInfo2.AddColumn(new ColumnInfo("count", PrimitiveTypes.Integer()));
			context.Session.Access.CreateTable(tableInfo2);
			context.Session.Access.AddPrimaryKey(tn2, "id");
			context.Session.Access.AddForeignKey(tn2, new[] { "other_id" }, tn1, new[] { "id" }, ForeignKeyAction.Cascade,
				ForeignKeyAction.Cascade, null);
		}

		[Test]
		public void DropNonReferencedTable() {
			var tableName = ObjectName.Parse("APP.test_table2");
			Query.DropTable(tableName);

			var exists = Query.Session.Access.TableExists(tableName);
			Assert.IsFalse(exists);
		}

		[Test]
		public void DropIfExists_TableExists() {
			var tableName = ObjectName.Parse("APP.test_table2");

			Query.DropTable(tableName, true);

			var exists = Query.Session.Access.TableExists(tableName);
			Assert.IsFalse(exists);
		}

		[Test]
		public void DropIfExists_TableNotExists() {
			var tableName = ObjectName.Parse("APP.test_table3");

			Query.DropTable(tableName, true);

			var exists = Query.Session.Access.TableExists(tableName);
			Assert.IsFalse(exists);
		}

		[Test]
		public void DropReferencedTable() {
			var tableName = ObjectName.Parse("APP.test_table1");

			Assert.Throws<ConstraintViolationException>(() => Query.DropTable(tableName));

			var exists = Query.Session.Access.TableExists(tableName);
			Assert.IsTrue(exists);
		}

	}
}
