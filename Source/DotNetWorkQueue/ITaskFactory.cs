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

using System;
using System.Threading.Tasks;
using DotNetWorkQueue.TaskScheduling;
namespace DotNetWorkQueue
{
    /// <summary>
    /// Extends the standard task factory to allow for trying to add a new task, but failing to do so.
    /// </summary>
    public interface ITaskFactory
    {
        /// <summary>
        /// Tries to add a new task to the task factory
        /// </summary>
        /// <remarks>The factory may reject the request if it's too busy.</remarks>
        /// <param name="action">The action.</param>
        /// <param name="state">The state.</param>
        /// <param name="continueWith">The continue with action.</param>
        /// <param name="task">The task.</param>
        /// <returns>true if the task was added, false otherwise</returns>
        TryStartNewResult TryStartNew(Action<object> action, StateInformation state, Action<Task> continueWith, out Task task);
        /// <summary>
        /// Gets the task scheduler.
        /// </summary>
        /// <value>
        /// The scheduler.
        /// </value>
        ATaskScheduler Scheduler { get;  }
    }

    /// <summary>
    /// Indicates the result of a request to start a new task on <see cref="ITaskFactory"/>
    /// </summary>
    public enum TryStartNewResult
    {
        /// <summary>
        /// The result was not specified
        /// </summary>
        NotSpecified = 0,
        /// <summary>
        /// The task was added
        /// </summary>
        Added = 1,
        /// <summary>
        /// The task was added, but is queued for processing
        /// </summary>
        Queued = 2,
        /// <summary>
        /// The task was rejected; it was not started or queued.
        /// </summary>
        Rejected = 3
    }

    /// <summary>
    /// An extention method for determining if <see cref="TryStartNewResult"/> returned a success code
    /// </summary>
    public static class TryStartNewResultExtensions
    {
        /// <summary>
        /// Returns true if <see cref="TryStartNewResult"/> contains a success status code
        /// </summary>
        /// <param name="result">The result.</param>
        /// <returns></returns>
        public static bool Success(this TryStartNewResult result)
        {
            switch (result)
            {
                case TryStartNewResult.Added:
                case TryStartNewResult.Queued:
                    return true;
                default: //everything else means it was not added
                    return false;
            }
        }
    }
}
