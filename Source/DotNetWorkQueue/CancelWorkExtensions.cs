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
namespace DotNetWorkQueue
{
    /// <summary>
    /// Helpers for <see cref="ICancelWork"/>.
    /// </summary>
    public static class CancelWorkExtensions
    {
        /// <summary>
        /// Whether any of the tokens has been cancelled.
        /// </summary>
        /// <param name="cancelWork">The tokens to check.</param>
        /// <returns><c>true</c> if work should stop.</returns>
        /// <remarks>
        /// A plain loop rather than <c>Tokens.Any(t =&gt; t.IsCancellationRequested)</c>. Every
        /// transport asks this twice per message received, and <see cref="System.Linq"/> reaches
        /// the list through <see cref="System.Collections.Generic.IEnumerable{T}"/>, which boxes
        /// the list's enumerator - an allocation per call to read a handful of booleans.
        /// </remarks>
        public static bool AnyCancellationRequested(this ICancelWork cancelWork)
        {
            if (cancelWork == null) return false;

            var tokens = cancelWork.Tokens;
            if (tokens == null) return false;

            for (var i = 0; i < tokens.Count; i++)
            {
                if (tokens[i].IsCancellationRequested) return true;
            }
            return false;
        }
    }
}
