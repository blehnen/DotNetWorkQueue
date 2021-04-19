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
using System.Collections.Generic;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase
{
    /// <summary>
    /// Finds any previous errors that have occurred while processing a message
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.IGetPreviousMessageErrors" />
    public class GetPreviousMessageErrors<T> : IGetPreviousMessageErrors
    {
        #region Member Level Variables
        private readonly IQueryHandler<GetMessageErrorsQuery<T>, Dictionary<string, int>> _getErrorMessageQueryHandler;
        #endregion

        #region Constructor        
        /// <summary>
        /// Initializes a new instance of the <see cref="GetPreviousMessageErrors{T}"/> class.
        /// </summary>
        /// <param name="getErrorMessageQueryHandler">The get error message query handler.</param>
        public GetPreviousMessageErrors(IQueryHandler<GetMessageErrorsQuery<T>, Dictionary<string, int>> getErrorMessageQueryHandler)
        {
            Guard.NotNull(() => getErrorMessageQueryHandler, getErrorMessageQueryHandler);
            _getErrorMessageQueryHandler = getErrorMessageQueryHandler;
        }
        #endregion

        #region IGetPreviousMessageErrors
        /// <inheritdoc />
        public IReadOnlyDictionary<string, int> Get(IMessageId id)
        {
            return !id.HasValue 
                ? new Dictionary<string, int>() 
                : _getErrorMessageQueryHandler.Handle(new GetMessageErrorsQuery<T>((T)id.Id.Value));
        }
        #endregion
    }
}
