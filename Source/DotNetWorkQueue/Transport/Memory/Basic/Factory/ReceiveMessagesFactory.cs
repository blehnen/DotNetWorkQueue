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
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Memory.Basic.Factory
{
    /// <summary>
    /// Creates new instances of <see cref="IReceiveMessages"/>
    /// </summary>
    public class ReceiveMessagesFactory : IReceiveMessagesFactory
    {
        private readonly IContainerFactory _container;
        /// <summary>
        /// Initializes a new instance of the <see cref="ReceiveMessagesFactory" /> class.
        /// </summary>
        /// <param name="container">The container.</param>
        public ReceiveMessagesFactory(IContainerFactory container)
        {
            Guard.NotNull(() => container, container);
            _container = container;
        }
        /// <summary>
        /// Creates a new instance of <see cref="IReceiveMessages" />
        /// </summary>
        /// <returns></returns>
        public IReceiveMessages Create()
        {
            return _container.Create().GetInstance<IReceiveMessages>();
        }
    }
}
