// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
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
using DotNetWorkQueue.Messages;

namespace DotNetWorkQueue.LinqCompile
{
    /// <summary>
    /// Compiles Linq strings into actions and functions
    /// </summary>
    /// <seealso cref="ILinqCompiler" />
    internal class LinqCompiler : ILinqCompiler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LinqCompiler" /> class.
        /// </summary>
        public LinqCompiler()
        {
        }

        /// <summary>
        /// Compiles the input linqExpression into a Linq expression tree
        /// </summary>
        /// <param name="linqExpression">The linqExpression.</param>
        /// <returns></returns>
        public Action<object, object> CompileAction(LinqExpressionToRun linqExpression)
        {
            throw new NotSupportedException("Dynamic LINQ string compilation is no longer supported. Use compiled Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>> instead.");
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
        }
    }
}
