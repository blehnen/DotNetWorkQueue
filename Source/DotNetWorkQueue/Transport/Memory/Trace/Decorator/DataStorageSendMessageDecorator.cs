// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2021 Brian Lehnen
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
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Trace;
using OpenTelemetry.Trace;

namespace DotNetWorkQueue.Transport.Memory.Trace.Decorator
{
    /// <summary>
    /// Tracing for sending a message
    /// </summary>
    public class DataStorageSendMessageDecorator : IDataStorageSendMessage
    {
        private readonly IDataStorageSendMessage _handler;
        private readonly ActivitySource _tracer;
        private readonly IHeaders _headers;
        private readonly IConnectionInformation _connectionInformation;

        /// <summary>Initializes a new instance of the <see cref="DataStorageSendMessageDecorator" /> class.</summary>
        /// <param name="handler">The handler.</param>
        /// <param name="tracer">The tracer.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="connectionInformation">The connection information.</param>
        public DataStorageSendMessageDecorator(IDataStorageSendMessage handler, ActivitySource tracer,
            IHeaders headers, IConnectionInformation connectionInformation)
        {
            _handler = handler;
            _tracer = tracer;
            _headers = headers;
            _connectionInformation = connectionInformation;
        }

        /// <inheritdoc />
        public Guid SendMessage(IMessage messageToSend, IAdditionalMessageData data)
        {
            using (var scope = _tracer.StartActivity("SendMessage"))
            {
                scope?.AddCommonTags(data, _connectionInformation);
                if(scope?.Context != null)
                    messageToSend.Inject(_tracer, scope.Context, _headers.StandardHeaders);
                try
                {
                    var id = _handler.SendMessage(messageToSend, data);
                    if(id == Guid.Empty)
                        scope?.SetStatus(Status.Error);;
                    scope?.AddMessageIdTag(id.ToString());
                    return id;
                }
                catch (Exception e)
                {
                    scope?.SetStatus(Status.Error);;
                    scope?.RecordException(e);
                    throw;
                }
            }
        }

        /// <inheritdoc />
        public async Task<Guid> SendMessageAsync(IMessage messageToSend, IAdditionalMessageData data)
        {
            using (var scope = _tracer.StartActivity("SendMessage"))
            {
                scope?.AddCommonTags(data, _connectionInformation);
                if(scope?.Context != null)
                    messageToSend.Inject(_tracer, scope.Context, _headers.StandardHeaders);
                try
                {
                    var id = await _handler.SendMessageAsync(messageToSend, data).ConfigureAwait(false);
                    if (id == Guid.Empty)
                        scope?.SetStatus(Status.Error);;
                    scope?.AddMessageIdTag(id.ToString());
                    return id;
                }
                catch (Exception e)
                {
                    scope?.SetStatus(Status.Error);;
                    scope?.RecordException(e);
                    throw;
                }
            }
        }
    }
}
