﻿// 
//  Copyright 2011-2013 Deveel
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

using System;

using Deveel.Data.DbSystem;

using NUnit.Framework;

namespace Deveel.Data.Sql {
	[TestFixture]
	public sealed class CreateTableTest : TestBase {
		[Test(Description = "Creates a simple table without constraints nor identities")]
		public void CreateSimpleTable() {
			ExecuteNonQuery("CREATE TABLE Test (field1 INT, field2 VARCHAR(200), field3 DATE);", true);

			Assert.AreEqual(0, ErrorCount);

			Table table = GetTable("Test");
			Assert.IsNotNull(table);
			Assert.AreEqual(3, table.TableInfo.ColumnCount);
			Assert.AreEqual("field1", table.TableInfo[0].Name);
			Assert.AreEqual(SqlType.Integer, table.TableInfo[0].SqlType);

			ExecuteNonQuery("DROP TABLE Test", true);
			Assert.AreEqual(0, ErrorCount);
		}

		[Test]
		public void CreateTableWithIdentity() {
			ExecuteNonQuery("CREATE TABLE Test (id IDENTITY, name VARCHAR)", true);

			Assert.AreEqual(0, ErrorCount);

			Table table = GetTable("Test");
			Assert.IsNotNull(table);
			Assert.IsTrue(table.TableInfo.FindColumnName("id") == 0);
			Assert.AreEqual(SqlType.Identity, table.TableInfo[0].SqlType);

			ExecuteNonQuery("DROP TABLE Test", true);
			Assert.AreEqual(0, ErrorCount);
		}

		[Test]
		public void CreateTableWithUniqueGroup() {
			SafeExecuteNonQuery("CREATE TABLE Test (name VARCHAR(30), age INT, UNIQUE(name))");

			Assert.AreEqual(0, ErrorCount);

			var connection = CreateDatabaseConnection();
			DataConstraintInfo[] constraints = connection.QueryTableUniqueGroups(new TableName("APP", "Test"));
			Assert.IsNotNull(constraints);
			Assert.AreEqual(1, constraints.Length);
			Assert.AreEqual(1, constraints[0].Columns.Length);
			Assert.AreEqual("name", constraints[0].Columns[0]);

			SafeExecuteNonQuery("DROP TABLE Test");
		}

		[Test]
		public void CreateTableWithPrimaryGroup() {
			ExecuteNonQuery("CREATE TABLE Test (id INT, name VARCHAR, PRIMARY KEY(id))", true);

			var connection = CreateDatabaseConnection();
			DataConstraintInfo constraint = connection.QueryTablePrimaryKeyGroup(new TableName("APP", "Test"));
			Assert.IsNotNull(constraint);
			Assert.AreEqual(1, constraint.Columns.Length);
			Assert.AreEqual("id", constraint.Columns[0]);

			ExecuteNonQuery("DROP TABLE Test", true);
		}

		[Test]
		public void CreateTableWithPrimaryAndUniqueGroups() {
			ExecuteNonQuery("CREATE TABLE Test (id INT, name VARCHAR, age INT, UNIQUE(name), PRIMARY KEY(id))",true );

			var connection = CreateDatabaseConnection();
			DataConstraintInfo pkey = connection.QueryTablePrimaryKeyGroup(new TableName("APP", "Test"));
			DataConstraintInfo[] unique = connection.QueryTableUniqueGroups(new TableName("APP", "Test"));

			Assert.IsNotNull(pkey);
			Assert.AreEqual(1, pkey.Columns.Length);
			Assert.AreEqual("id", pkey.Columns[0]);

			Assert.IsNotNull(unique);
			Assert.AreEqual(1, unique.Length);
			Assert.AreEqual("name", unique[0].Columns[0]);

			ExecuteNonQuery("DROP TABLE Test", true);
		}
	}
}