﻿using System;
using System.Data;
using System.IO;

using Deveel.Data.Protocol;
using Deveel.Data.Security;
using Deveel.Data.Sql;
using Deveel.Data.Transactions;

namespace Deveel.Data.DbSystem {
	public sealed class Database : IDatabase {
		public Database(IDatabaseContext context) {
			Context = context;

			DiscoverDataVersion();

			TableComposite = new TableSourceComposite(this);

			ActiveUsers = new ActiveUserList(this);

			// Create the single row table
			var t = new TemporaryTable(context, "SINGLE_ROW_TABLE", new ColumnInfo[0]);
			t.NewRow();
			SingleRowTable = t;

			OpenTransactions = new TransactionCollection(this);
		}

		~Database() {
			Dispose(false);
		}

		IDatabase ITransactionContext.Database {
			get { return this; }
		}

		public TransactionCollection OpenTransactions { get; private set; }

		ITransaction ITransactionContext.CreateTransaction(TransactionIsolation isolation) {
			return CreateTransaction(isolation);
		}

		private void DiscoverDataVersion() {
			var dataVerion = Attribute.GetCustomAttribute(typeof (Database).Assembly, typeof (DataVersionAttribute))
				as DataVersionAttribute;
			if (dataVerion != null)
				Version = dataVerion.Version;
		}

		private ITransaction CreateTransaction(TransactionIsolation isolation) {
			if (IsOpen)
				throw new InvalidOperationException(String.Format("Database {0} is not open.", this.Name()));

			lock (this) {
				ITransaction transaction;

				try {
					transaction = TableComposite.CreateTransaction(isolation);
				} catch (DatabaseSystemException) {
					throw;
				} catch (Exception ex) {
					throw new DatabaseSystemException("Unable to create a transaction.", ex);
				}

				return transaction;
			}
		}

		public IUserSession CreateSession(User user, ConnectionEndPoint userEndPoint) {
			return CreateSession(user, userEndPoint, TransactionIsolation.Serializable);
		}

		public IUserSession CreateSession(User user, ConnectionEndPoint userEndPoint, TransactionIsolation isolation) {
			if (user == null)
				user = User.System;

			var transaction = CreateTransaction(isolation);
			return new UserSession(this, transaction, user, userEndPoint);
		}

		public IUserSession OpenSession(User user, ConnectionEndPoint userEndPoint, int commitId) {
			var transaction = OpenTransactions.FindById(commitId);
			if (transaction == null)
				throw new InvalidOperationException(String.Format("The request transaction with ID '{0}' is not open.", commitId));

			return new UserSession(this, transaction, user, userEndPoint);
		}

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing) {
			if (disposing) {
				if (IsOpen) {
					// TODO: Report the error
				}

				TableComposite.Dispose();
				Context.Dispose();
			}

			TableComposite = null;
			Context = null;
		}

		public IDatabaseContext Context { get; private set; }

		public Version Version { get; private set; }

		public ActiveUserList ActiveUsers { get; private set; }

		public bool Exists {
			get {
				if (IsOpen)
					//throw new Exception("The database is initialised, so no point testing it's existance.");
					return true;

				try {
					return TableComposite.Exists();
				} catch (IOException e) {
					throw new Exception("An error occurred while testing database existance.", e);
				}
			}
		}

		public bool IsOpen { get; private set; }

		public TableSourceComposite TableComposite { get; private set; }

		public ITable SingleRowTable { get; private set; }

		public string StateResourceName {
			get { return String.Format("{0}_sf", this.Name()); }
		}

		public void Create(string adminName, string adminPassword) {
			if (Context.ReadOnly())
				throw new DatabaseSystemException("Can not create database in Read only mode.");

			if (String.IsNullOrEmpty(adminName))
				throw new ArgumentNullException("adminName");
			if (String.IsNullOrEmpty(adminPassword))
				throw new ArgumentNullException("adminPassword");

			try {
				// Create the conglomerate
				TableComposite.Create();

				using (var session = this.CreateSystemSession()) {
					session.AutoCommit(false);

					var context = new SessionQueryContext(session);
					session.ExclusiveLock();
					session.CurrentSchema(SystemSchema.Name);

					// Create the schema information tables
					CreateSchemata(session);

					// The system tables that are present in every conglomerate.
					SystemSchema.CreateTables(session);

					// Create the system views
					// TODO: InformationSchema.CreateSystemViews(session);

					CreateAdminUser(context, adminName, adminPassword);

					SetCurrentDataVersion(session);

					// Set all default system procedures.
					// TODO: SystemSchema.SetupSystemFunctions(session, username);

					try {
						// Close and commit this transaction.
						session.Commit();
					} catch (TransactionException e) {
						throw new DatabaseSystemException("Could not commit the initial information", e);
					}
				}

				// Close the conglomerate.
				TableComposite.Close();
			} catch (DatabaseSystemException e) {
				throw;
			} catch (Exception e) {
				throw new DatabaseSystemException("An error occurred while creating the database.", e);
			}
		}

		private void SetCurrentDataVersion(IUserSession session) {
			// TODO: Get the data version and then set it to the database table 'vars'
		}

		private void CreateSchemata(IUserSession session) {
			try {
				// TODO: Create INFORMATION_SCHEMA
				session.CreateSchema(Context.DefaultSchema(), SchemaTypes.Default);
			} catch (DatabaseSystemException) {
				throw;
			} catch (Exception ex) {
				throw new DatabaseSystemException("Unable to create the default schema for the database.", ex);
			}
		}

		private void CreateAdminUser(IQueryContext context, string adminName, string adminPassword) {
			try {
				var user = context.CreateUser(adminName, adminPassword);

				// This is the admin user so add to the 'secure access' table.
				context.AddUserToGroup(adminName, SystemGroupNames.SecureGroup);

				context.GrantHostAccessToUser(adminName, KnownConnectionProtocols.TcpIp, "%");
				context.GrantHostAccessToUser(adminName, KnownConnectionProtocols.Local, "%");

				context.GrantToUserOnSchema(Context.DefaultSchema(), user, User.System, Privileges.SchemaAll, true);
				context.GrantToUserOnSchema(SystemSchema.Name, user, User.System, Privileges.SchemaRead);

				// TODO: Grant READ on INFORMATION_SCHEMA

				context.GrantToUserOnSchemaTables(SystemSchema.Name, user, User.System, Privileges.TableRead);
			} catch (DatabaseSystemException) {
				throw;
			} catch (Exception ex) {
				throw new DatabaseSystemException("Could not create the database administrator.", ex);
			}
		}

		public void Open() {
			if (IsOpen)
				throw new DatabaseSystemException("The database was already initialized.");

			try {
				// Check if the state file exists.  If it doesn't, we need to report
				// incorrect version.
				if (!Context.StoreSystem.StoreExists(StateResourceName))
					// If neither store or state file exist, assume database doesn't
					// exist.
					throw new DatabaseSystemException(String.Format("The database {0} does not exist.", this.Name()));

				// Open the conglomerate
				TableComposite.Open();

				AssertDataVersion();
			} catch (DatabaseSystemException e) {
				throw;
			} catch (Exception e) {
				throw new DatabaseSystemException("An error occurred when initializing the database.", e);
			}

			IsOpen = true;
		}

		private void AssertDataVersion() {
			// TODO:
		}

		public void Close() {
			if (!IsOpen)
				throw new DatabaseSystemException("The database is not initialized.");

			try {
				if (Context.DeleteOnClose()) {
					// Delete the tables if the database is set to delete on
					// shutdown.
					TableComposite.Delete();
				} else {
					// Otherwise close the conglomerate.
					TableComposite.Close();
				}
			} catch (DatabaseSystemException e) {
				throw;
			} catch (Exception e) {
				throw new DatabaseSystemException("An error occurred during database shutdown.", e);
			} finally {
				IsOpen = false;
			}
		}
	}
}
