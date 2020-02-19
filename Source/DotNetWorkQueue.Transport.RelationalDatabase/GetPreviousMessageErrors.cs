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
