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
namespace DotNetWorkQueue
{
    /// <summary>
    /// Indicates what mode we are processing messages in
    /// </summary>
    public enum MessageProcessingModes
    {
        /// <summary>
        /// Sync mode will be used. Threads that read messages will also run user to code to process them
        /// </summary>
        Sync = 0,
        /// <summary>
        /// ASync mode will be used. User code execution will be passed to another thread
        /// </summary>
        Async = 1
    }
}
