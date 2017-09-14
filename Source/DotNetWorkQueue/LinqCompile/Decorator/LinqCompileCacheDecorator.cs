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
using System.Text;
using CacheManager.Core;
using DotNetWorkQueue.Messages;

namespace DotNetWorkQueue.LinqCompile.Decorator
{
    /// <summary>
    /// Cache decorator for linq compiles
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.ILinqCompiler" />
    public class LinqCompileCacheDecorator: ILinqCompiler
    {
        private readonly ILinqCompiler _handler;
        private readonly ICacheManager<object> _cache;
        private readonly ICachePolicy<ILinqCompiler> _itemPolicy;

        private readonly ICounter _counterActionCacheHit;
        private readonly ICounter _counterActionCacheMiss;
        private readonly ICounter _counterActionCacheUnique;

        private readonly ICounter _counterFunctionCacheHit;
        private readonly ICounter _counterFunctionCacheMiss;
        private readonly ICounter _counterFunctionCacheUnique;

        /// <summary>
        /// Initializes a new instance of the <see cref="LinqCompileCacheDecorator" /> class.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="cache">The cache.</param>
        /// <param name="cachePolicy">The cache policy.</param>
        /// <param name="metrics">The metrics.</param>
        /// <param name="connectionInformation">The connection information.</param>
        public LinqCompileCacheDecorator(
            ILinqCompiler handler,
            ICacheManager<object> cache,
            ICachePolicy<ILinqCompiler> cachePolicy,
            IMetrics metrics,
             IConnectionInformation connectionInformation)
        {
            _handler = handler;
            _cache = cache;
            _itemPolicy = cachePolicy;
            var name = handler.GetType().Name;

            _counterActionCacheHit = metrics.Counter($"{connectionInformation.QueueName}.{name}.LinqActionCacheHitCounter", Units.Items);
            _counterActionCacheMiss = metrics.Counter($"{connectionInformation.QueueName}.{name}.LinqActionCacheMissCounter", Units.Items);
            _counterActionCacheUnique = metrics.Counter($"{connectionInformation.QueueName}.{name}.LinqActionUniqueFlaggedCounter", Units.Items);

            _counterFunctionCacheHit = metrics.Counter($"{connectionInformation.QueueName}.{name}.LinqFunctionCacheHitCounter", Units.Items);
            _counterFunctionCacheMiss = metrics.Counter($"{connectionInformation.QueueName}.{name}.LinqFunctionCacheMissCounter", Units.Items);
            _counterFunctionCacheUnique = metrics.Counter($"{connectionInformation.QueueName}.{name}.LinqFunctionUniqueFlaggedCounter", Units.Items);

        }

        /// <summary>
        /// Compiles the input linqExpression into a Linq expression tree
        /// </summary>
        /// <param name="linqExpression">The linqExpression.</param>
        /// <returns></returns>
        public Action<object, object> CompileAction(LinqExpressionToRun linqExpression)
        {
            if (linqExpression.Unique) //don't bother caching
            {
                _counterActionCacheUnique.Increment();
                return _handler.CompileAction(linqExpression);
            }

            var key = GenerateKey(linqExpression);
            var result = (Action<object, object>)_cache[key];

            if (result != null)
            {
                _counterActionCacheHit.Increment(key);
                return result;
            }

            result = _handler.CompileAction(linqExpression);
            if (!_cache.Exists(key))
            {
                _counterActionCacheMiss.Increment(key);
                _cache.Add(key, result);
                _cache.Expire(key, ExpirationMode.Sliding, _itemPolicy.SlidingExpiration);
            }

            return result;
        }

        /// <summary>
        /// Compiles the input linqExpression into a Linq expression tree
        /// </summary>
        /// <param name="linqExpression">The linqExpression.</param>
        /// <returns></returns>
        public Func<object, object, object> CompileFunction(LinqExpressionToRun linqExpression)
        {
            if (linqExpression.Unique) //don't bother caching
            {
                _counterFunctionCacheUnique.Increment();
                return _handler.CompileFunction(linqExpression);
            }

            var key = GenerateKey(linqExpression);
            var result = (Func<object, object, object>)_cache[key];

            if (result != null)
            {
                _counterFunctionCacheHit.Increment(key);
                return result;
            }

            result = _handler.CompileFunction(linqExpression);
            if (!_cache.Exists(key))
            {
                _counterFunctionCacheMiss.Increment(key);
                _cache.Add(key, result);
                _cache.Expire(key, _itemPolicy.SlidingExpiration);
            }

            return result;
        }

        /// <summary>
        /// Generates a key for the cache
        /// </summary>
        /// <param name="linqExpression">The linq expression.</param>
        /// <returns></returns>
        private string GenerateKey(LinqExpressionToRun linqExpression)
        {
            var builder = new StringBuilder();
            builder.Append(linqExpression.Linq);
            foreach (var field in linqExpression.References)
            {
                builder.Append(field);
                builder.Append("|");
            }
            foreach (var field in linqExpression.Usings)
            {
                builder.Append(field);
                builder.Append("|");
            }
            return builder.ToString();
        }

        #region IDisposable Support
        private bool _disposedValue; // To detect redundant calls
        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _handler.Dispose();
                }
                _disposedValue = true;
            }
        }
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
