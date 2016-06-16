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
using System.Runtime.Caching;
using System.Text;
using DotNetWorkQueue.Logging;
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
        private readonly ObjectCache _cache;
        private readonly ILog _log;
        private readonly CacheItemPolicy _itemPolicy;

        /// <summary>
        /// Initializes a new instance of the <see cref="LinqCompileCacheDecorator" /> class.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="cache">The cache.</param>
        /// <param name="log">The log.</param>
        /// <param name="cachePolicy">The cache policy.</param>
        public LinqCompileCacheDecorator(
            ILinqCompiler handler,
            ObjectCache cache,
            ILogFactory log,
            ICachePolicy<ILinqCompiler> cachePolicy)
        {
            _handler = handler;
            _cache = cache;
            _log = log.Create();
            _itemPolicy = new CacheItemPolicy {SlidingExpiration = cachePolicy.SlidingExpiration};
        }

        /// <summary>
        /// Compiles the input linqExpression into a Linq expression tree
        /// </summary>
        /// <param name="linqExpression">The linqExpression.</param>
        /// <returns></returns>
        public Action<object, object> CompileAction(LinqExpressionToRun linqExpression)
        {
            var key = GenerateKey(linqExpression);
            var result = (Action<object, object>)_cache[key];

            if (result != null) return result;

            _log.Log(LogLevel.Debug, () => $"No cache entry for key [{key}]");
            result = _handler.CompileAction(linqExpression);
            if (!_cache.Contains(key))
                _cache.Add(key, result, _itemPolicy);

            return result;
        }

        /// <summary>
        /// Compiles the input linqExpression into a Linq expression tree
        /// </summary>
        /// <param name="linqExpression">The linqExpression.</param>
        /// <returns></returns>
        public Func<object, object, object> CompileFunction(LinqExpressionToRun linqExpression)
        {
            var key = GenerateKey(linqExpression);
            var result = (Func<object, object, object>)_cache[key];

            if (result != null) return result;

            _log.Log(LogLevel.Debug, () => $"No cache entry for key [{key}]");

            result = _handler.CompileFunction(linqExpression);
            if (!_cache.Contains(key))
                _cache.Add(key, result, _itemPolicy);

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
    }
}
