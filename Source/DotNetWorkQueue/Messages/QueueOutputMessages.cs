// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015 Brian Lehnen
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

using System.Collections;
using System.Collections.Generic;
using System.Linq;
namespace DotNetWorkQueue.Messages
{
    /// <summary>
    /// The results of a producer batch send
    /// </summary>
    public class QueueOutputMessages : IQueueOutputMessages
    {
        private readonly List<IQueueOutputMessage> _list;
        /// <summary>
        /// Initializes a new instance of the <see cref="QueueOutputMessages"/> class.
        /// </summary>
        /// <param name="messages">The messages.</param>
        public QueueOutputMessages(List<IQueueOutputMessage> messages)
        {
            _list = messages;
        }

        /// <summary>
        /// Gets a value indicating whether this instance contains a message result with an exception or no message ID
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance has errors; otherwise, <c>false</c>.
        /// </value>
        public bool HasErrors
        {
            get { return _list.Any(x => x.HasError); }
        }
        /// <summary>
        /// Gets or sets the <see cref="IQueueOutputMessage"/> at the specified index.
        /// </summary>
        /// <value>
        /// The <see cref="IQueueOutputMessage"/>.
        /// </value>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        public IQueueOutputMessage this[int index] => _list[index];

        /// <summary>
        /// Gets the count.
        /// </summary>
        /// <value>
        /// The count.
        /// </value>
        public int Count => _list.Count;

        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<IQueueOutputMessage> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }
    }
}
