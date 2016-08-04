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
using System.Collections.Generic;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Configuration
{
    /// <summary>
    /// Holds information about what exceptions should result in a message retry, how many times to retry and how to long to pause between retries.
    /// </summary>
    public class RetryDelay : IRetryDelay
    {
        private readonly IRetryInformationFactory _retryInformationFactory;
        internal readonly Dictionary<Type, List<TimeSpan>> RetryTypes = new Dictionary<Type, List<TimeSpan>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="RetryDelay"/> class.
        /// </summary>
        /// <param name="retryInformationFactory">The retry information factory.</param>
        public RetryDelay(IRetryInformationFactory retryInformationFactory)
        {
            _retryInformationFactory = retryInformationFactory;
        }
        /// <summary>
        /// Adds the specified exception type.
        /// </summary>
        /// <param name="exceptionType">Type of the exception.</param>
        /// <param name="timeSpans">The retry time spans.</param>
        public void Add(Type exceptionType, List<TimeSpan> timeSpans)
        {
            Guard.NotNull(() => exceptionType, exceptionType);
            Guard.NotNull(() => timeSpans, timeSpans);

            FailIfReadOnly();

            if (RetryTypes.ContainsKey(exceptionType))
            {
                throw new ArgumentException($"Duplicate type {exceptionType} specified; a type may only be added once");
            }
            RetryTypes.Add(exceptionType, timeSpans);
        }

        /// <summary>
        /// Returns information about how often to retry a particular exception.
        /// </summary>
        /// <typeparam name="T">The type of the exception</typeparam>
        /// <param name="exception">The exception.</param>
        /// <returns></returns>
        public IRetryInformation GetRetryAmount<T>(T exception) where T : Exception
        {
            Guard.NotNull(() => exception, exception);

            foreach (var exceptionType in GetAllExceptionTypes(exception.GetType()))
            {
                if (!RetryTypes.ContainsKey(exceptionType)) continue;
                var times = RetryTypes[exceptionType];
                return _retryInformationFactory.Create(exceptionType, times);
            }

            //no matching type was found - this will result in a retry count of 0
            return _retryInformationFactory.Create(typeof(Exception), new List<TimeSpan>());
        }
        /// <summary>
        /// Gets a value indicating whether this instance is read only.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is read only; otherwise, <c>false</c>.
        /// </value>
        public bool IsReadOnly { get; protected set; }

        /// <summary>
        /// Throws an exception if the read only flag is true.
        /// </summary>
        /// <exception cref="System.Data.ReadOnlyException"></exception>
        protected void FailIfReadOnly()
        {
            if (IsReadOnly) throw new InvalidOperationException();
        }

        /// <summary>
        /// Marks this instance as immutable
        /// </summary>
        public void SetReadOnly()
        {
            IsReadOnly = true;
        }

        /// <summary>
        /// Gets all exception types.
        /// </summary>
        /// <param name="exceptionType">Type of the exception.</param>
        /// <returns></returns>
        private IEnumerable<Type> GetAllExceptionTypes(Type exceptionType)
        {
            for (var current = exceptionType; current != null; current = current.BaseType)
                yield return current;
        }
    }
}
