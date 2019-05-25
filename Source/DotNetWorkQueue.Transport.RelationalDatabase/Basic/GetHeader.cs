// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2018 Brian Lehnen
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
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic
{
    /// <summary>
    /// Obtains header records
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.IGetHeader" />
    public class GetHeader: IGetHeader
    {
        #region Member Level Variables
        private readonly IQueryHandler<GetHeaderQuery, IDictionary<string, object>> _commandHandler;
        #endregion

        #region Constructor        
        /// <summary>
        /// Initializes a new instance of the <see cref="GetHeader"/> class.
        /// </summary>
        /// <param name="commandHandler">The command handler.</param>
        public GetHeader(IQueryHandler<GetHeaderQuery, IDictionary<string, object>> commandHandler)
        {
            Guard.NotNull(() => commandHandler, commandHandler);
            _commandHandler = commandHandler;
        }
        #endregion

        /// <inheritdoc />
        public IDictionary<string, object> GetHeaders(IMessageId id)
        {
            if(id != null && id.HasValue)
                return _commandHandler.Handle(new GetHeaderQuery((long)id.Id.Value));
            return null;
        }
    }
}
