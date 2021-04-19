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
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Messages
{
    /// <summary>
    /// Translates the internal <see cref="IReceivedMessageInternal"/> message into the user received message <see cref="IReceivedMessage{T}"/>
    /// </summary>
    /// <remarks>Since we never care what the user type is internally, we use dynamic to avoid having to pass the T around internally</remarks>
    public class GenerateReceivedMessage : IGenerateReceivedMessage
    {
        private readonly IGetPreviousMessageErrors _getErrors;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenerateReceivedMessage"/> class.
        /// </summary>
        /// <param name="getErrors">The get errors.</param>
        public GenerateReceivedMessage(IGetPreviousMessageErrors getErrors)
        {
            Guard.NotNull(() => getErrors, getErrors);
            _getErrors = getErrors;
        }

        /// <summary>
        /// Generates a <see cref="IReceivedMessage{T}" /> from a <see cref="IReceivedMessageInternal" />
        /// </summary>
        /// <param name="messageType">Type of the output message.</param>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        public dynamic GenerateMessage(Type messageType, IReceivedMessageInternal message)
        {
            var getHandlerGenericMethod = GetType().GetMethod("GetMessage", new[] { message.GetType() });
            if (getHandlerGenericMethod == null) throw new NullReferenceException("getHandlerGenericMethod is null");
            var generic = getHandlerGenericMethod.MakeGenericMethod(messageType);
            return generic.Invoke(this, new object[] {message});
        }
        /// <summary>
        /// Generates a <see cref="IReceivedMessage{T}" /> from a <see cref="IReceivedMessageInternal" />
        /// </summary>
        /// <typeparam name="T">The type of the output message</typeparam>
        /// <param name="internalMessage">The internal message.</param>
        /// <returns></returns>
        public IReceivedMessage<T> GetMessage<T>(IReceivedMessageInternal internalMessage) where T : class
        {
            var d1 = Type.GetType("DotNetWorkQueue.Messages.ReceivedMessage`1");
            Type[] typeArgs = {typeof(T) };

            //if d1 is null, just let it throw an exception; this would indicate that something strange has happened
            // ReSharper disable once PossibleNullReferenceException
            var make = d1.MakeGenericType(typeArgs);

            return (IReceivedMessage<T>)Activator.CreateInstance(make, internalMessage, _getErrors);
        }
    }
}
