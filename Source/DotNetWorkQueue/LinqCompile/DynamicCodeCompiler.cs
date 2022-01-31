﻿// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2022 Brian Lehnen
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
using System.Linq;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Messages;
using JpLabs.DynamicCode;
using Microsoft.Extensions.Logging;

namespace DotNetWorkQueue.LinqCompile
{
    /// <summary>
    /// A wrapper for <see cref="Compiler"/> that allows for caching of application domain instances
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.IPooledObject" />
    /// <seealso cref="System.IDisposable" />
    /// <remarks>This class is not thread safe - the caller must ensure that only one thread acts on an single instance.</remarks>
    internal class DynamicCodeCompiler : IPooledObject, IDisposable
    {
        private static readonly string[] DefaultReferences = { "System.dll", "System.Core.dll", "DotNetWorkQueue.dll" };
        private static readonly string[] DefaultUsing = { "System", "System.Collections.Generic", "System.Linq", "System.Linq.Expressions", "DotNetWorkQueue", "DotNetWorkQueue.Messages" };
        private readonly Compiler _compiler;
        private readonly ILogger _log;

        private const int MaxAssemblyCount = 10;

        public DynamicCodeCompiler(ILogger log)
        {
            _compiler = new Compiler();
            _log = log;
        }
        /// <summary>
        /// Compiles the input linqExpression into a Linq expression tree
        /// </summary>
        /// <param name="linqExpression">The linqExpression.</param>
        /// <returns></returns>
        public Action<object, object> CompileAction(LinqExpressionToRun linqExpression)
        {
            _compiler.References = DefaultReferences.Union(linqExpression.References).ToArray();
            _compiler.Usings = DefaultUsing.Union(linqExpression.Usings).ToArray();
            return _compiler.ParseLambdaExpr<Action<object, object>>(linqExpression.Linq).Compile();
        }
        /// <inheritdoc />
        public void ResetState()
        {
            if (_compiler.DynamicAssembliesCount < MaxAssemblyCount) return;
            _log.LogDebug($"Max dynamic assembly count {MaxAssemblyCount} reached; Recycling AppDomain");
            _compiler.RecycleAppDomain();
        }

        #region IDisposable Support
        private bool _disposedValue; // To detect redundant calls
        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected void Dispose(bool disposing)
        {
            if (_disposedValue) return;
            if (disposing)
            {
                _compiler.Dispose();
            }
            _disposedValue = true;
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
