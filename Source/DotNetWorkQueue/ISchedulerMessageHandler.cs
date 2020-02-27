// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2020 Brian Lehnen
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
    /// Handles scheduling a message for task schedulers
    /// </summary>
    public interface ISchedulerMessageHandler
    {
        /// <summary>
        /// Handles scheduling a message on the task scheduler
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="workGroup">The work group.</param>
        /// <param name="message">The message.</param>
        /// <param name="notifications">The notifications.</param>
        /// <param name="functionToRun">The function to run.</param>
        /// <param name="taskFactory">The task factory.</param>
        /// <returns></returns>
        Task HandleAsync<T>(IWorkGroup workGroup, IReceivedMessage<T> message, IWorkerNotification notifications,
            Action<IReceivedMessage<T>, IWorkerNotification> functionToRun, ITaskFactory taskFactory)
            where T : class;
    }
}
