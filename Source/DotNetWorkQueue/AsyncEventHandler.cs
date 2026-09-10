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
using System.Threading.Tasks;

namespace DotNetWorkQueue
{
    /// <summary>
    /// An event handler that can be awaited.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event data.</param>
    /// <returns>A task that completes when the handler has finished.</returns>
    /// <remarks>
    /// <see cref="EventHandler"/> returns void, so a subscriber doing I/O has to block the thread that
    /// raised the event. That is what the commit and rollback events did on the asynchronous consumer,
    /// where the raise happens in a continuation on a thread-pool thread.
    ///
    /// Handlers are awaited one after another, in subscription order, which is how the synchronous
    /// events already behaved - the next handler does not start until the previous one has finished.
    /// </remarks>
    public delegate Task AsyncEventHandler(object sender, EventArgs e);
}
