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
using System.Data.Common;
using DotNetWorkQueue.Transport.Shared.Basic.Command;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command
{
    /// <summary>
    /// Relational-transport variant of <see cref="SendMessageCommand"/> that carries an
    /// optional caller-supplied <see cref="DbTransaction"/> and signals (via
    /// <see cref="IRetrySkippable"/>) that the retry decorator should bypass its Polly
    /// pipeline on this command. Constructed by <c>RelationalProducerQueue&lt;TMessage&gt;</c>
    /// when one of the tx-aware <c>Send</c>/<c>SendAsync</c> overloads is invoked.
    /// </summary>
    public class RelationalSendMessageCommand : SendMessageCommand, IRetrySkippable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RelationalSendMessageCommand"/>
        /// class.
        /// </summary>
        /// <param name="messageToSend">The message to send.</param>
        /// <param name="messageData">The additional message data.</param>
        /// <param name="externalTransaction">Caller-supplied transaction. May be null,
        /// in which case the command behaves identically to its base class (and
        /// <see cref="SkipRetry"/> evaluates to <c>false</c>).</param>
        public RelationalSendMessageCommand(IMessage messageToSend,
            IAdditionalMessageData messageData,
            DbTransaction externalTransaction)
            : base(messageToSend, messageData)
        {
            ExternalTransaction = externalTransaction;
        }

        /// <summary>
        /// New: signals the retry decorator to bypass its Polly pipeline whenever the
        /// caller supplied a transaction. The caller owns retry semantics on this path.
        /// </summary>
        public bool SkipRetry => ExternalTransaction != null;
    }
}
