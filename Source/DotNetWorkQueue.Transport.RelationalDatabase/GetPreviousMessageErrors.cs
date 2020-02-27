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
using System.Collections.Generic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase
{
    public class GetPreviousMessageErrors: IGetPreviousMessageErrors
    {
        #region Member Level Variables
        private readonly IQueryHandler<GetMessageErrorsQuery, Dictionary<string, int>> _getErrorMessageQueryHandler;
        #endregion

        #region Constructor
        public GetPreviousMessageErrors(IQueryHandler<GetMessageErrorsQuery, Dictionary<string, int>> getErrorMessageQueryHandler)
        {
            Guard.NotNull(() => getErrorMessageQueryHandler, getErrorMessageQueryHandler);
            _getErrorMessageQueryHandler = getErrorMessageQueryHandler;
        }
        #endregion

        #region IGetPreviousMessageErrors
        public IReadOnlyDictionary<string, int> Get(IMessageId id)
        {
            return !id.HasValue 
                ? new Dictionary<string, int>() 
                : _getErrorMessageQueryHandler.Handle(new GetMessageErrorsQuery((long)id.Id.Value));
        }
        #endregion
    }
}
