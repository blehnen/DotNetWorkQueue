// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
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
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using DotNetWorkQueue.Messages;
namespace DotNetWorkQueue
{
    /// <summary>
    /// Sends linq methods to be executed.
    /// </summary>
    public interface IProducerMethodQueue : IProducerBaseQueue
    {
        /// <summary>
        /// Sends the specified linqExpression to be executed. Additional message meta data is optional.
        /// </summary>
        /// <param name="method">The linqExpression to execute.</param>
        /// <param name="data">The optional additional message data.</param>
        /// <returns></returns>
        IQueueOutputMessage Send(Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>> method, IAdditionalMessageData data = null);

        /// <summary>
        /// Sends the specified methods to be executed.
        /// </summary>
        /// <param name="methods">The methods to execute.</param>
        /// <returns></returns>
        IQueueOutputMessages Send(List<Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>> methods);

        /// <summary>
        /// Sends the specified methods to be executed.
        /// </summary>
        /// <param name="methods">The methods to execute.</param>
        /// <returns></returns>
        IQueueOutputMessages Send(List<QueueMessage<Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>, IAdditionalMessageData>> methods);

        /// <summary>
        /// Sends the specified linqExpression to be executed. Additional message meta data is optional.
        /// </summary>
        /// <param name="method">The linqExpression to execute.</param>
        /// <param name="data">The optional additional message data.</param>
        /// <returns></returns>
        Task<IQueueOutputMessage> SendAsync(Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>> method, IAdditionalMessageData data = null);

        /// <summary>
        /// Sends the specified methods to be executed.
        /// </summary>
        /// <param name="methods">The messages.</param>
        /// <returns></returns>
        Task<IQueueOutputMessages> SendAsync(List<Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>> methods);

        /// <summary>
        /// Sends the specified methods to be executed.
        /// </summary>
        /// <param name="methods">The messages.</param>
        /// <returns></returns>
        Task<IQueueOutputMessages> SendAsync(List<QueueMessage<Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>, IAdditionalMessageData>> methods);


        /// <summary>
        /// Sends the specified linqExpression to be executed. Additional message meta data is optional.
        /// </summary>
        /// <param name="linqExpression">The linqExpression to execute.</param>
        /// <param name="data">The optional additional message data.</param>
        /// <returns></returns>
        IQueueOutputMessage Send(LinqExpressionToRun linqExpression, IAdditionalMessageData data = null);

        /// <summary>
        /// Sends the specified methods to be executed.
        /// </summary>
        /// <param name="methods">The methods to execute.</param>
        /// <returns></returns>
        IQueueOutputMessages Send(List<LinqExpressionToRun> methods);

        /// <summary>
        /// Sends the specified methods to be executed.
        /// </summary>
        /// <param name="methods">The methods to execute.</param>
        /// <returns></returns>
        IQueueOutputMessages Send(List<QueueMessage<LinqExpressionToRun, IAdditionalMessageData>> methods);

        /// <summary>
        /// Sends the specified linqExpression to be executed. Additional message meta data is optional.
        /// </summary>
        /// <param name="linqExpression">The linqExpression to execute.</param>
        /// <param name="data">The optional additional message data.</param>
        /// <returns></returns>
        Task<IQueueOutputMessage> SendAsync(LinqExpressionToRun linqExpression, IAdditionalMessageData data = null);

        /// <summary>
        /// Sends the specified methods to be executed.
        /// </summary>
        /// <param name="methods">The messages.</param>
        /// <returns></returns>
        Task<IQueueOutputMessages> SendAsync(List<LinqExpressionToRun> methods);

        /// <summary>
        /// Sends the specified methods to be executed.
        /// </summary>
        /// <param name="methods">The messages.</param>
        /// <returns></returns>
        Task<IQueueOutputMessages> SendAsync(List<QueueMessage<LinqExpressionToRun, IAdditionalMessageData>> methods);
    }
}
