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
namespace DotNetWorkQueue
{
    /// <summary>
    /// Allows caller to hold a reference to a created queue, to prevent it from going out of scope.
    /// </summary>
    /// <remarks>Generally speaking, this only affects non persistent queues that live only in memory.</remarks>
    /// <seealso cref="System.IDisposable" />
    public interface ICreationScope: IDisposable
    {
        /// <summary>
        /// Adds the scoped disposable object to the scope.
        /// </summary>
        /// <remarks>All objects added here will be disposed of when the scope is disposed</remarks>
        /// <param name="disposable">The disposable.</param>
        void AddScopedObject(IDisposable disposable);
    }
}
