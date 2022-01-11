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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Validation;
using Microsoft.Extensions.Logging;

namespace DotNetWorkQueue.Messages
{
    /// <summary>
    /// A received message
    /// </summary>
    /// <typeparam name="T">The type of the message</typeparam>
    public class ReceivedMessage<T> : IReceivedMessage<T> where T : class
    {
        private IReadOnlyDictionary<string, int> _previousErrors = null;
        private readonly IGetPreviousMessageErrors _getErrors;
        private readonly ILogger _logger;

        /// <summary>Initializes a new instance of the <see cref="IReceivedMessage{T}" /> class.</summary>
        /// <param name="message">The internal message.</param>
        /// <param name="getErrors">Gets any previous errors that have occurred for this specific message</param>
        /// <param name="logger">logger</param>
        public ReceivedMessage(IReceivedMessageInternal message, IGetPreviousMessageErrors getErrors,
            ILogger logger)
        {
            Guard.NotNull(() => message, message);
            Guard.NotNull(() => getErrors, getErrors);
            Guard.NotNull(() => logger, logger);

            _getErrors = getErrors;
            _logger = logger;

            Body = (T) message.Body;
            Headers = new ReadOnlyDictionary<string, object>(message.Headers.ToDictionary(entry => entry.Key,
                entry => entry.Value));
            MessageId = message.MessageId;
            CorrelationId = message.CorrelationId;
        }

        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public IMessageId MessageId { get; }

        /// <summary>
        /// Gets or sets the correlation identifier.
        /// </summary>
        /// <value>
        /// The correlation identifier.
        /// </value>
        public ICorrelationId CorrelationId { get; }

        /// <summary>
        /// Gets the body of the message.
        /// </summary>
        /// <value>
        /// The body.
        /// </value>
        public T Body { get; }

        /// <summary>
        /// Gets the headers.
        /// </summary>
        /// <value>
        /// The headers.
        /// </value>
        public IReadOnlyDictionary<string, object> Headers { get; }

        /// <inheritdoc/>
        public bool PreviousErrorsLoaded { get; private set; }

        /// <inheritdoc/>
        public IReadOnlyDictionary<string, int> PreviousErrors 
        {
            get
            {
                if (_previousErrors != null)
                    return _previousErrors;

                try
                { //failing to load the previous error messages should not throw an exception
                    _previousErrors = _getErrors.Get(
                        MessageId);
                    PreviousErrorsLoaded = true;
                }
                catch (Exception e)
                {
                    _logger.LogWarning($"Failed to load the previous error messages {System.Environment.NewLine}{e}");
                }
                return _previousErrors;
            }
        }

    /// <summary>
        /// Returns typed data from the headers collection
        /// </summary>
        /// <typeparam name="THeader">data type</typeparam>
        /// <param name="property">The property.</param>
        /// <returns></returns>
        public THeader GetHeader<THeader>(IMessageContextData<THeader> property)
            where THeader : class
        {
            if (Headers.ContainsKey(property.Name))
            {
                return (THeader) Headers[property.Name];
            }
            return property.Default;
        }
    }
}
