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
using System.Linq.Expressions;
using System.Threading.Tasks;
using DotNetWorkQueue.Messages;

namespace DotNetWorkQueue
{
    /// <summary>
    /// Allows executing remote calls as a linq expression and returning the result of the expression.
    /// </summary>
    public interface IRpcMethodQueue : IRpcBaseQueue
    {
        /// <summary>
        /// Sends the specified linqExpression for execution.
        /// </summary>
        /// <param name="method">The linqExpression.</param>
        /// <param name="timeOut">The time out.</param>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        /// <remarks>Your expression must return a type of object, or the JSON serializer may throw casting errors</remarks>
        IReceivedMessage<object> Send(Expression<Func<IReceivedMessage<MessageExpression>, IWorkerNotification, object>> method, TimeSpan timeOut, IAdditionalMessageData data = null);

        /// <summary>
        /// Sends the specified linqExpression for execution.
        /// </summary>
        /// <param name="method">The linqExpression.</param>
        /// <param name="timeOut">The time out.</param>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        /// <remarks>Your expression must return a type of object, or the JSON serializer may throw casting errors</remarks>
        Task<IReceivedMessage<object>> SendAsync(Expression<Func<IReceivedMessage<MessageExpression>, IWorkerNotification, object>> method, TimeSpan timeOut, IAdditionalMessageData data = null);

        /// <summary>
        /// Sends the specified linqExpression for execution.
        /// </summary>
        /// <param name="linqExpression">The linqExpression.</param>
        /// <param name="timeOut">The time out.</param>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        /// <remarks>Your expression must return a type of object, or the JSON serializer may throw casting errors</remarks>
        IReceivedMessage<object> Send(LinqExpressionToRun linqExpression, TimeSpan timeOut, IAdditionalMessageData data = null);

        /// <summary>
        /// Sends the specified linqExpression for execution.
        /// </summary>
        /// <param name="linqExpression">The linqExpression.</param>
        /// <param name="timeOut">The time out.</param>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        /// <remarks>Your expression must return a type of object, or the JSON serializer may throw casting errors</remarks>
        Task<IReceivedMessage<object>> SendAsync(LinqExpressionToRun linqExpression, TimeSpan timeOut, IAdditionalMessageData data = null);
    }
}
