// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2021 Brian Lehnen
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
namespace DotNetWorkQueue.Transport.Shared.Basic.Factory
{
    /// <inheritdoc />
    public class CorrelationIdFactory<T> : ICorrelationIdFactory
        where T : struct, IComparable<T>
    {
        private readonly bool _isGuid;

        /// <summary>
        /// Initializes a new instance of the <see cref="CorrelationIdFactory{T}"/> class.
        /// </summary>
        public CorrelationIdFactory()
        {
            // new T() for guid returns Guid.Empty
            if (typeof(T) == typeof(Guid))
            {
                _isGuid = true;
            }
        }
        /// <inheritdoc />
        public ICorrelationId Create()
        {
            return _isGuid
                ? (ICorrelationId) new MessageCorrelationId<Guid>(Guid.NewGuid())
                : new MessageCorrelationId<T>(new T());
        }
    }
}
