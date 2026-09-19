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
namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// The marker the compiler requires to emit an <c>init</c> accessor.
    /// </summary>
    /// <remarks>
    /// Part of the framework from .NET 5 on. The compiler only needs the type to exist, and it has
    /// to exist in each assembly that uses <c>init</c> or a record, which is why this is linked into
    /// every project by <c>Directory.Build.props</c> rather than referenced from one (GitHub #252).
    /// </remarks>
    internal static class IsExternalInit
    {
    }
}
