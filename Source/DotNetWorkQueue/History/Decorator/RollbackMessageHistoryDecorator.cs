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
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DotNetWorkQueue.History.Decorator
{
    internal class RollbackMessageHistoryDecorator : IRollbackMessage
    {
        private readonly IRollbackMessage _handler;
        private readonly IWriteMessageHistory _history;
        private readonly IBaseTransportOptions _options;
        private readonly ILogger _log;

        public RollbackMessageHistoryDecorator(IRollbackMessage handler,
            IWriteMessageHistory history,
            IBaseTransportOptions options,
            ILogger log)
        {
            _handler = handler;
            _history = history;
            _options = options;
            _log = log;
        }

        public bool Rollback(IMessageContext context)
        {
            var result = _handler.Rollback(context);
            if (_options.EnableHistory && context.MessageId != null && context.MessageId.HasValue)
            {
                try
                {
                    _history.RecordRollback(context.MessageId.Id.Value.ToString());
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "Failed to record history for rollback of message {MessageId}", context.MessageId.Id.Value);
                }
            }
            return result;
        }

        /// <inheritdoc />
        /// <remarks>
        /// Mirrors its synchronous twin, including the absence of a <c>result</c> check that the commit
        /// decorator has - a rollback is recorded whether or not the handler reported success - and the
        /// absence of a per-event flag, since there is no TrackRollback option to check.
        /// </remarks>
        public async Task<bool> RollbackAsync(IMessageContext context)
        {
            var result = await _handler.RollbackAsync(context).ConfigureAwait(false);
            if (_options.EnableHistory && context.MessageId != null && context.MessageId.HasValue)
            {
                try
                {
                    await _history.RecordRollbackAsync(context.MessageId.Id.Value.ToString()).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "Failed to record history for rollback of message {MessageId}", context.MessageId.Id.Value);
                }
            }
            return result;
        }
    }
}
