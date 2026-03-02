// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
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
using System.Collections.Generic;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IoC;
using DotNetWorkQueue.Transport.Memory.Basic.CommandHandler;
using DotNetWorkQueue.Transport.Memory.Basic.QueryHandler;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Memory.Basic
{
    /// <summary>
    /// Extends <see cref="MemoryMessageQueueInit"/> with dashboard handler registrations.
    /// Users configure the dashboard with <c>AddConnection&lt;MemoryDashboardInit&gt;(...)</c>.
    /// </summary>
    public class MemoryDashboardInit : MemoryMessageQueueInit
    {
        /// <inheritdoc />
        public override void RegisterImplementations(IContainer container, RegistrationTypes registrationType,
            QueueConnection queueConnection)
        {
            Guard.NotNull(() => container, container);

            base.RegisterImplementations(container, registrationType, queueConnection);

            // Dashboard read handlers
            container.Register<IQueryHandlerAsync<GetDashboardStatusCountsQuery, DashboardStatusCounts>,
                GetDashboardStatusCountsQueryHandlerAsync>(LifeStyles.Singleton);
            container.Register<IQueryHandlerAsync<GetDashboardMessagesQuery, IReadOnlyList<DashboardMessage>>,
                GetDashboardMessagesQueryHandlerAsync>(LifeStyles.Singleton);
            container.Register<IQueryHandlerAsync<GetDashboardMessageCountQuery, long>,
                GetDashboardMessageCountQueryHandlerAsync>(LifeStyles.Singleton);
            container.Register<IQueryHandlerAsync<GetDashboardMessageDetailQuery, DashboardMessage>,
                GetDashboardMessageDetailQueryHandlerAsync>(LifeStyles.Singleton);
            container.Register<IQueryHandlerAsync<GetDashboardStaleMessagesQuery, IReadOnlyList<DashboardMessage>>,
                GetDashboardStaleMessagesQueryHandlerAsync>(LifeStyles.Singleton);
            container.Register<IQueryHandlerAsync<GetDashboardErrorMessagesQuery, IReadOnlyList<DashboardErrorMessage>>,
                GetDashboardErrorMessagesQueryHandlerAsync>(LifeStyles.Singleton);
            container.Register<IQueryHandlerAsync<GetDashboardErrorMessageCountQuery, long>,
                GetDashboardErrorMessageCountQueryHandlerAsync>(LifeStyles.Singleton);
            container.Register<IQueryHandlerAsync<GetDashboardErrorRetriesQuery, IReadOnlyList<DashboardErrorRetry>>,
                GetDashboardErrorRetriesQueryHandlerAsync>(LifeStyles.Singleton);
            container.Register<IQueryHandlerAsync<GetDashboardConfigurationQuery, byte[]>,
                GetDashboardConfigurationQueryHandlerAsync>(LifeStyles.Singleton);
            container.Register<IQueryHandlerAsync<GetDashboardJobsQuery, IReadOnlyList<DashboardJob>>,
                GetDashboardJobsQueryHandlerAsync>(LifeStyles.Singleton);
            container.Register<IQueryHandlerAsync<GetDashboardMessageBodyQuery, DashboardMessageBody>,
                GetDashboardMessageBodyQueryHandlerAsync>(LifeStyles.Singleton);
            container.Register<IQueryHandlerAsync<GetDashboardMessageHeadersQuery, DashboardMessageHeaders>,
                GetDashboardMessageHeadersQueryHandlerAsync>(LifeStyles.Singleton);

            // Dashboard write handlers
            container.Register<ICommandHandlerWithOutput<DashboardDeleteMessageCommand, long>,
                DashboardDeleteMessageCommandHandler>(LifeStyles.Singleton);
            container.Register<ICommandHandlerWithOutput<DashboardDeleteAllErrorMessagesCommand, long>,
                DashboardDeleteAllErrorMessagesCommandHandler>(LifeStyles.Singleton);
            container.Register<ICommandHandlerWithOutput<DashboardRequeueErrorMessageCommand, long>,
                DashboardRequeueErrorMessageCommandHandler>(LifeStyles.Singleton);
            container.Register<ICommandHandlerWithOutput<DashboardResetStaleMessageCommand, long>,
                DashboardResetStaleMessageCommandHandler>(LifeStyles.Singleton);
            container.Register<ICommandHandlerWithOutput<DashboardUpdateMessageBodyCommand, long>,
                DashboardUpdateMessageBodyCommandHandler>(LifeStyles.Singleton);
        }
    }
}
