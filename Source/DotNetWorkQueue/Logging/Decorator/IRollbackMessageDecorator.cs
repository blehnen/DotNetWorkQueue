// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Logging.Decorator
{
    internal class RollbackMessageDecorator: IRollbackMessage
    {
        private readonly ILog _log;
        private readonly IRollbackMessage _handler;

        /// <summary>
        /// Initializes a new instance of the <see cref="RollbackMessageDecorator" /> class.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="handler">The handler.</param>
        public RollbackMessageDecorator(ILogFactory log,
            IRollbackMessage handler)
        {
            Guard.NotNull(() => log, log);
            Guard.NotNull(() => handler, handler);

            _log = log.Create();
            _handler = handler;
        }

        public bool Rollback(IMessageContext context)
        {
            try
            {
                return _handler.Rollback(context);
            }
            catch (Exception e)
            {
                _log.ErrorException("An error has occurred while trying to rollback a message", e, null);
                return false;
            }
        }
    }
}
