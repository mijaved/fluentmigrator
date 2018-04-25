#region License

// Copyright (c) 2007-2018, Sean Chambers <schambers80@gmail.com>
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics;

using FluentMigrator.Runner.Initialization;

using JetBrains.Annotations;

namespace FluentMigrator.Runner.Processors
{
    public abstract class GenericProcessorBase : ProcessorBase
    {
        [Obsolete]
        private readonly string _connectionString;

        [NotNull, ItemCanBeNull]
        private readonly Lazy<DbProviderFactory> _dbProviderFactory;

        [NotNull, ItemCanBeNull]
        private readonly Lazy<IDbConnection> _lazyConnection;

        [CanBeNull]
        private IDbConnection _connection;

        [Obsolete]
        protected GenericProcessorBase(
            IDbConnection connection,
            IDbFactory factory,
            IMigrationGenerator generator,
            IAnnouncer announcer,
            [NotNull] IMigrationProcessorOptions options)
            : base(generator, announcer, options)
        {
            _dbProviderFactory = new Lazy<DbProviderFactory>(() => (factory as DbFactoryBase)?.Factory);

            // Set the connection string, because it cannot be set by
            // the base class (due to the missing information)
            Options.ConnectionString = connection?.ConnectionString;

            // Prefetch connectionstring as after opening the security info could no longer be present
            // for instance on sql server
            _connectionString = connection?.ConnectionString;

            Factory = factory;

            _lazyConnection = new Lazy<IDbConnection>(() => connection);
        }

        protected GenericProcessorBase(
            [CanBeNull] Func<DbProviderFactory> factoryAccessor,
            [NotNull] IMigrationGenerator generator,
            [NotNull] IAnnouncer announcer,
            [NotNull] ProcessorOptions options,
            [NotNull] IConnectionStringAccessor connectionStringAccessor)
            : base(generator, announcer, options)
        {
            _dbProviderFactory = new Lazy<DbProviderFactory>(() => factoryAccessor?.Invoke());

            var connectionString = connectionStringAccessor.ConnectionString;

#pragma warning disable 612
            var legacyFactory = new DbFactoryWrapper(this);

            // Prefetch connectionstring as after opening the security info could no longer be present
            // for instance on sql server
            _connectionString = connectionString;

            Factory = legacyFactory;
#pragma warning restore 612

            _lazyConnection = new Lazy<IDbConnection>(
                () =>
                {
                    if (DbProviderFactory == null)
                        return null;
                    Connection = DbProviderFactory.CreateConnection();
                    Debug.Assert(Connection != null, nameof(Connection) + " != null");
                    Connection.ConnectionString = connectionString;
                    return Connection;
                });
        }

        [Obsolete("Will change from public to protected")]
        public override string ConnectionString => _connectionString;

        public IDbConnection Connection
        {
            get => _connection ?? _lazyConnection.Value;
            protected set => _connection = value;
        }

        [Obsolete]
        [NotNull]
        public IDbFactory Factory { get; protected set; }

        [CanBeNull]
        public IDbTransaction Transaction { get; protected set; }

        [CanBeNull]
        protected DbProviderFactory DbProviderFactory => _dbProviderFactory.Value;

        protected virtual void EnsureConnectionIsOpen()
        {
            if (Connection != null && Connection.State != ConnectionState.Open)
            {
                Connection.Open();
            }
        }

        protected virtual void EnsureConnectionIsClosed()
        {
            if ((_connection != null || (_lazyConnection.IsValueCreated && Connection != null)) && Connection.State != ConnectionState.Closed)
            {
                Connection.Close();
            }
        }

        public override void BeginTransaction()
        {
            if (Transaction != null) return;

            EnsureConnectionIsOpen();

            Announcer.Say("Beginning Transaction");

            Transaction = Connection?.BeginTransaction();
        }

        public override void RollbackTransaction()
        {
            if (Transaction == null) return;

            Announcer.Say("Rolling back transaction");
            Transaction.Rollback();
            Transaction.Dispose();
            WasCommitted = true;
            Transaction = null;
        }

        public override void CommitTransaction()
        {
            if (Transaction == null) return;

            Announcer.Say("Committing Transaction");
            Transaction.Commit();
            Transaction.Dispose();
            WasCommitted = true;
            Transaction = null;
        }

        protected override void Dispose(bool isDisposing)
        {
            RollbackTransaction();
            EnsureConnectionIsClosed();
        }

        protected virtual IDbCommand CreateCommand(string commandText)
        {
            return CreateCommand(commandText, Connection, Transaction);
        }

        protected virtual IDbCommand CreateCommand(string commandText, IDbConnection connection, IDbTransaction transaction)
        {
            IDbCommand result;
            if (DbProviderFactory != null)
            {
                result = DbProviderFactory.CreateCommand();
                Debug.Assert(result != null, nameof(result) + " != null");
                result.Connection = connection;
                if (transaction != null)
                    result.Transaction = transaction;
                result.CommandText = commandText;
            }
            else
            {
#pragma warning disable 612
                result = Factory.CreateCommand(commandText, connection, transaction, Options);
#pragma warning restore 612
            }

            if (Options.Timeout != null)
            {
                result.CommandTimeout = (int) Options.Timeout.Value.TotalSeconds;
            }

            return result;
        }

        [Obsolete]
        private class DbFactoryWrapper : IDbFactory
        {
            private readonly GenericProcessorBase _processor;

            public DbFactoryWrapper(GenericProcessorBase processor)
            {
                _processor = processor;
            }

            /// <inheritdoc />
            public IDbConnection CreateConnection(string connectionString)
            {
                Debug.Assert(_processor.DbProviderFactory != null, "_processor.DbProviderFactory != null");
                var result = _processor.DbProviderFactory.CreateConnection();
                Debug.Assert(result != null, nameof(result) + " != null");
                result.ConnectionString = connectionString;
                return result;
            }

            /// <inheritdoc />
            [Obsolete]
            public IDbCommand CreateCommand(
                string commandText,
                IDbConnection connection,
                IDbTransaction transaction,
                IMigrationProcessorOptions options)
            {
                return _processor.CreateCommand(commandText);
            }
        }
    }
}
