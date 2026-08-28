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
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace DotNetWorkQueue.Validation
{
    /// <summary>
    /// Argument validation that costs nothing when the argument is valid.
    /// </summary>
    /// <remarks>
    /// These replace the <c>Expression&lt;Func&lt;T&gt;&gt;</c> overloads in the vendored half of
    /// this class. Those take the parameter name from an expression tree, which means the compiler
    /// emits tree-building code at every call site and runs it on every call - including the call
    /// where the argument is fine and nothing is thrown. Measured at 43 ns and 154 bytes per call,
    /// against 14 calls for a single send and roughly 19 per message consumed, that was around
    /// 2.7 KB of garbage per message for a parameter name that is a compile-time constant.
    /// <para>
    /// <see cref="CallerArgumentExpressionAttribute"/> gives the same name as a literal baked in by
    /// the compiler, so the valid path is a null comparison and nothing else. Every converted call
    /// site produces the identical <see cref="ArgumentException.ParamName"/> it did before.
    /// </para>
    /// </remarks>
    public static partial class Guard
    {
        /// <summary>
        /// Ensures the given <paramref name="value"/> is not null.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <param name="name">
        /// The argument name; supplied by the compiler from the expression passed as
        /// <paramref name="value"/>. Do not pass this explicitly.
        /// </param>
        /// <returns><paramref name="value"/>, so the check can be inlined into an assignment.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
        public static T NotNull<T>(T value, [CallerArgumentExpression(nameof(value))] string name = null)
        {
            //ReferenceEquals rather than "value == null": T is unconstrained, so the == operator
            //trips S2955. This is behavior-identical (value types box to a non-null reference; a
            //null Nullable<T> boxes to null) while only checking for a genuine null reference.
            if (ReferenceEquals(value, null))
                ThrowNull(name);

            return value;
        }

        /// <summary>
        /// Ensures the given string <paramref name="value"/> is neither null nor empty.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <param name="name">
        /// The argument name; supplied by the compiler from the expression passed as
        /// <paramref name="value"/>. Do not pass this explicitly.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> is empty.</exception>
        public static void NotNullOrEmpty(string value, [CallerArgumentExpression(nameof(value))] string name = null)
        {
            if (value == null)
                ThrowNull(name);
            if (value.Length == 0)
                throw new ArgumentException("Parameter cannot be empty.", name);
        }

        /// <summary>
        /// Ensures the given <paramref name="value"/> satisfies <paramref name="validate"/>.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <param name="validate">The predicate the value must satisfy.</param>
        /// <param name="message">The message for the exception thrown when it does not.</param>
        /// <param name="name">
        /// The argument name; supplied by the compiler from the expression passed as
        /// <paramref name="value"/>. Do not pass this explicitly.
        /// </param>
        /// <exception cref="ArgumentException"><paramref name="value"/> is not valid.</exception>
        public static void IsValid<T>(T value, Func<T, bool> validate, string message,
            [CallerArgumentExpression(nameof(value))] string name = null)
        {
            if (!validate(value))
                throw new ArgumentException(message, name);
        }

        /// <summary>
        /// Kept out of line so the calling method stays small enough for the JIT to inline it; the
        /// throw is the cold path and does not belong in the caller's body.
        /// </summary>
        [DoesNotReturn]
        private static void ThrowNull(string name)
        {
            throw new ArgumentNullException(name, "Parameter cannot be null.");
        }
    }
}
