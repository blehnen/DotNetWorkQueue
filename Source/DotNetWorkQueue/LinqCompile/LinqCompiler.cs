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
using System.Linq;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Messages;
namespace DotNetWorkQueue.LinqCompile
{
    /// <summary>
    /// Compiles Linq strings into actions and functions
    /// </summary>
    /// <seealso cref="ILinqCompiler" />
    public class LinqCompiler : ILinqCompiler
    {
        private static readonly string[] DefaultReferences = { "System.dll", "System.Core.dll", "DotNetWorkQueue.dll" };
        private static readonly string[] DefaultUsings = { "System", "System.Collections.Generic", "System.Linq", "System.Linq.Expressions", "DotNetWorkQueue", "DotNetWorkQueue.Messages" };
        
        /// <summary>
        /// Compiles the input linqExpression into a Linq expression tree
        /// </summary>
        /// <param name="linqExpression">The linqExpression.</param>
        /// <returns></returns>
        public Action<object, object> CompileAction(LinqExpressionToRun linqExpression)
        {
            Guard.NotNull(() => linqExpression, linqExpression);
            Guard.NotNullOrEmpty(() => linqExpression.Linq, linqExpression.Linq);
            var compiler = CreateCompiler(linqExpression);
            try
            {
                var data = compiler.ParseLambdaExpr<Action<object, object>>(linqExpression.Linq).Compile();
                return data;
            }
            catch (Exception error)
            {
                throw new CompileException($"Failed to compile linq expression {linqExpression.Linq}", error, linqExpression.Linq);
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
            var compiler = CreateCompiler(linqExpression);
            try
            {
                var data = compiler.ParseLambdaExpr<Func<object, object, object>>(linqExpression.Linq).Compile();
                return data;
            }
            catch (Exception error)
            {
                throw new CompileException($"Failed to compile linq expression [{linqExpression.Linq}]", error, linqExpression.Linq);
            }
        }

        /// <summary>
        /// Creates the Linq compiler.
        /// </summary>
        /// <param name="linqExpression">The linq expression.</param>
        /// <returns></returns>
        private JpLabs.DynamicCode.Compiler CreateCompiler(LinqExpressionToRun linqExpression)
        {
            return new JpLabs.DynamicCode.Compiler
            {
                References = DefaultReferences.Union(linqExpression.References).ToArray(),
                Usings = DefaultUsings.Union(linqExpression.Usings).ToArray()
            };
        }
    }
}
