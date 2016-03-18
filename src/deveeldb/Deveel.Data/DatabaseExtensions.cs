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

using Deveel.Data.Security;
using Deveel.Data.Sql.Schemas;
using Deveel.Data.Transactions;

namespace Deveel.Data {
	public static class DatabaseExtensions {
		#region Transactions

		public static ITransaction CreateTransaction(this IDatabase database, IsolationLevel isolation) {
			if (!database.IsOpen)
				throw new InvalidOperationException(String.Format("Database '{0}' is not open.", database.Name));

			return database.CreateSafeTransaction(isolation);
		}

		public static ITransaction FindTransactionById(this IDatabase database, int commidId) {
			return database.TransactionFactory.OpenTransactions.FindById(commidId);
		}

		internal static ITransaction CreateSafeTransaction(this IDatabase database, IsolationLevel isolation) {
			return database.TransactionFactory.CreateTransaction(isolation);
		}

		#endregion

		#region Sessions

		private static ISession CreateUserSession(this IDatabase database, string userName, IsolationLevel isolation) {
			if (String.IsNullOrEmpty(userName))
				throw new ArgumentNullException("userName");

			// TODO: if the isolation is not specified, use a configured default one
			if (isolation == IsolationLevel.Unspecified)
				isolation = IsolationLevel.Serializable;

			var transaction = database.CreateTransaction(isolation);
			return new Session(transaction, userName);
		}

		static ISession CreateSystemSession(this IDatabase database, IsolationLevel isolation) {
			var transaction = database.CreateTransaction(isolation);
			return new SystemSession(transaction, SystemSchema.Name);
		}

		internal static ISession CreateInitialSystemSession(this IDatabase database) {
			var transaction = database.CreateSafeTransaction(IsolationLevel.Serializable);
			return new SystemSession(transaction, SystemSchema.Name);
		}

		internal static ISession CreateSystemSession(this IDatabase database) {
			return database.CreateSystemSession(IsolationLevel.Serializable);
		}

		public static ISession CreateUserSession(this IDatabase database, string userName, string password) {
			return CreateUserSession(database, userName, password, IsolationLevel.Unspecified);
		}

		public static ISession CreateUserSession(this IDatabase database, string userName, string password, IsolationLevel isolation) {
			if (!database.Authenticate(userName, password))
				throw new InvalidOperationException(String.Format("Unable to create a session for user '{0}': not authenticated.", userName));

			return database.CreateUserSession(userName, isolation);
		}

		static ISession OpenUserSession(this IDatabase database, int commitId, string userName) {
			if (commitId < 0)
				throw new ArgumentException("Invalid commit reference specified.");

			var transaction = database.FindTransactionById(commitId);
			if (transaction == null)
				throw new InvalidOperationException(String.Format("The request transaction with ID '{0}' is not open.", commitId));

			return new Session(transaction, userName);
		}

		#endregion

		#region Security

		public static void CreateAdminUser(this IDatabase database, IQuery context, string adminName, string adminPassword) {
			try {
				context.Access.CreateUser(adminName, adminPassword);

				// This is the admin user so add to the 'secure access' table.
				context.Access.AddUserToRole(adminName, SystemRoles.SecureAccessRole);

				context.Access.GrantOnSchema(database.Context.DefaultSchema(), adminName, Privileges.SchemaAll, true);
				context.Access.GrantOnSchema(SystemSchema.Name, adminName, Privileges.SchemaRead);
				context.Access.GrantOnSchema(InformationSchema.SchemaName, adminName, Privileges.SchemaRead);

				SystemSchema.GrantToPublic(context);
			} catch (DatabaseSystemException) {
				throw;
			} catch (Exception ex) {
				throw new DatabaseSystemException("Could not create the database administrator.", ex);
			}
		}

		public static bool Authenticate(this IDatabase database, string username, string password) {
			// Create a temporary connection for authentication only...
			using (var session = database.CreateSystemSession()) {
				session.CurrentSchema(SystemSchema.Name);
				return session.Access.Authenticate(username, password);
			}
		}

		#endregion
	}
}
