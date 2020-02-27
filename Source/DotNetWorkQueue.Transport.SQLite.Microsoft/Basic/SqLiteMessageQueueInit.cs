// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2020 Brian Lehnen
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// ---------------------------------------------------------------------
using System.Reflection;
using DotNetWorkQueue.IoC;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.SQLite.Microsoft.Decorator;
using DotNetWorkQueue.Transport.SQLite.Shared;
using DotNetWorkQueue.Transport.SQLite.Shared.Basic;

namespace DotNetWorkQueue.Transport.SQLite.Microsoft.Basic
{
    /// <inheritdoc />
    public class SqLiteMessageQueueInit : SqLiteMessageQueueSharedInit
    {
        /// <inheritdoc />
        public override void RegisterImplementations(IContainer container, RegistrationTypes registrationType,
            string connection, string queue)
        {
            var myType = Assembly.GetAssembly(GetType());
            var baseType = Assembly.GetAssembly(typeof(IDbFactory));

            base.RegisterImplementations(container, registrationType, connection, queue, myType, baseType);

            container.Register<IDbDataSource, DbDataSource>(LifeStyles.Singleton);
            container.Register<IGetFileNameFromConnectionString, GetFileNameFromConnectionString>(LifeStyles.Singleton);
            container.Register<IDbFactory, DbFactory>(LifeStyles.Singleton);
            container.Register<IReaderAsync, ReaderAsync>(LifeStyles.Singleton);
            container.Register<DatabaseExists>(LifeStyles.Singleton);

            //register our decorator that handles table creation errors
            container.RegisterDecorator(
                typeof(ICommandHandlerWithOutput<CreateJobTablesCommand<ITable>, QueueCreationResult>),
                typeof(CreateJobTablesCommandDecorator), LifeStyles.Singleton);

            //register our decorator that handles table creation errors
            container.RegisterDecorator(
                typeof(ICommandHandlerWithOutput<CreateQueueTablesAndSaveConfigurationCommand<ITable>,
                    QueueCreationResult>),
                typeof(CreateQueueTablesAndSaveConfigurationDecorator), LifeStyles.Singleton);
        }

        /// <inheritdoc />
        protected override void SetupPolicy(IContainer container)
        {
            RetryTransactionPolicyCreation.Register(container);
        }
    }
}
