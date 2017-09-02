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
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.LinqCompile
{
    /// <summary>
    /// Compiles Linq strings into actions and functions
    /// </summary>
    /// <seealso cref="ILinqCompiler" />
    internal class LinqCompiler : ILinqCompiler
    {
        private readonly IObjectPool<DynamicCodeCompiler> _objectPool;
        /// <summary>
        /// Initializes a new instance of the <see cref="LinqCompiler" /> class.
        /// </summary>
        /// <param name="objectPool">The object pool.</param>
        public LinqCompiler(IObjectPool<DynamicCodeCompiler> objectPool)
        {
            _objectPool = objectPool;
        }

        /// <summary>
        /// Compiles the input linqExpression into a Linq expression tree
        /// </summary>
        /// <param name="linqExpression">The linqExpression.</param>
        /// <returns></returns>
        public Action<object, object> CompileAction(LinqExpressionToRun linqExpression)
        {
            Guard.NotNull(() => linqExpression, linqExpression);
            Guard.NotNullOrEmpty(() => linqExpression.Linq, linqExpression.Linq);
            var compiler = _objectPool.GetObject();
            try
            {
                return compiler.CompileAction(linqExpression);
            }
            catch (Exception error)
            {
                throw new CompileException($"Failed to compile linq expression {linqExpression.Linq}", error,
                    linqExpression.Linq);
            }
            finally
            {
                _objectPool.ReturnObject(compiler);
            }
        }

        /// <summary>
        /// Compiles the input linqExpression into a Linq expression tree
        /// </summary>
        /// <param name="linqExpression">The linqExpression.</param>
        /// <returns></returns>
        public Func<object, object, object> CompileFunction(LinqExpressionToRun linqExpression)
        {
            Guard.NotNull(() => linqExpression, linqExpression);
            Guard.NotNullOrEmpty(() => linqExpression.Linq, linqExpression.Linq);
            var compiler = _objectPool.GetObject();
            try
            {
                return compiler.CompileFunction(linqExpression);
            }
            catch (Exception error)
            {
                throw new CompileException($"Failed to compile linq expression [{linqExpression.Linq}]", error,
                    linqExpression.Linq);
            }
            finally
            {
                _objectPool.ReturnObject(compiler);
            }
        }

        #region IDisposable Support
        private bool _disposedValue;
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
                    _objectPool.Dispose();
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
