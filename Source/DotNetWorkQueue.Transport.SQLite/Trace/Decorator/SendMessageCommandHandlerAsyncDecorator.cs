// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2022 Brian Lehnen
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
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using DotNetWorkQueue.Trace;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using OpenTelemetry.Trace;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Transport.Shared.Trace;

namespace DotNetWorkQueue.Transport.SQLite.Trace.Decorator
{
    /// <summary>
    /// 
    /// </summary>
    public class SendMessageCommandHandlerAsyncDecorator : ICommandHandlerWithOutputAsync<SendMessageCommand, long>
    {
        private readonly ICommandHandlerWithOutputAsync<SendMessageCommand, long> _handler;
        private readonly ActivitySource _tracer;
        private readonly IHeaders _headers;
        private readonly IConnectionInformation _connectionInformation;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendMessageCommandHandlerAsyncDecorator"/> class.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="tracer">The tracer.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="connectionInformation">The connection information.</param>
        public SendMessageCommandHandlerAsyncDecorator(ICommandHandlerWithOutputAsync<SendMessageCommand, long> handler, ActivitySource tracer,
            IHeaders headers, IConnectionInformation connectionInformation)
        {
            _handler = handler;
            _tracer = tracer;
            _headers = headers;
            _connectionInformation = connectionInformation;
        }


        /// <inheritdoc />
        public async Task<long> HandleAsync(SendMessageCommand command)
        {
            using (var scope = _tracer.StartActivity("SendMessage"))
            {
                scope?.AddCommonTags(command.MessageData, _connectionInformation);
                scope?.Add(command);
                if (scope?.Context != null)
                    command.MessageToSend.Inject(_tracer, scope.Context, _headers.StandardHeaders);
                try
                {
                    var id = await _handler.HandleAsync(command);
                    if (id == 0)
                        Activity.Current?.SetStatus(ActivityStatusCode.Error);
                    scope?.AddMessageIdTag(id);
                    return id;
                }
                catch (Exception e)
                {
                    scope?.AddException(e);
                    Activity.Current?.SetStatus(ActivityStatusCode.Error);
                    throw;
                }
            }
        }
    }
}
