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

using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Factory
{
    /// <summary>
    /// Creates new instances of <see cref="IMessageProcessing"/>
    /// </summary>
    public class MessageProcessingFactory : IMessageProcessingFactory
    {
        private readonly IContainerFactory _container;
        private readonly MessageProcessingMode _mode;

        /// <summary>
        /// Initializes a new instance of the <see cref="IMessageProcessing" /> class.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="mode">The mode.</param>
        public MessageProcessingFactory(IContainerFactory container, MessageProcessingMode mode)
        {
            Guard.NotNull(() => container, container);
            Guard.NotNull(() => mode, mode);
            _container = container;
            _mode = mode;
        }

        /// <summary>
        /// Creates new instances of <see cref="MessageProcessing" />
        /// </summary>
        /// <returns></returns>
        public IMessageProcessing Create()
        {
            if(_mode.Mode == MessageProcessingModes.Async)
                return _container.Create().GetInstance<MessageProcessingAsync>();

            return _container.Create().GetInstance<MessageProcessing>();
        }
    }
}
