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
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IoC;

namespace DotNetWorkQueue.Tests.JobScheduler
{
    /// <summary>
    /// A transport that registers nothing.
    /// </summary>
    /// <remarks>
    /// Both <see cref="DotNetWorkQueue.JobScheduler.JobQueue"/> and
    /// <see cref="DotNetWorkQueue.JobScheduler.JobScheduler"/> take the transport as a generic
    /// type argument constrained to <c>ITransportInit, new()</c>, so a concrete class is
    /// required — a substitute cannot satisfy the <c>new()</c> constraint. This registers no
    /// implementations, which is sufficient for the paths these tests reach and keeps the
    /// test project free of any transport project reference.
    /// </remarks>
    internal class NoOpTransport : ITransportInit
    {
        public void RegisterImplementations(IContainer container, RegistrationTypes registrationType, QueueConnection queueConnection)
        {
        }

        public void SuppressWarningsIfNeeded(IContainer container, RegistrationTypes registrationType)
        {
        }

        public void SetDefaultsIfNeeded(IContainer container, RegistrationTypes registrationType, ConnectionTypes connectionType)
        {
        }

        public bool IsRelationalTransport => false;
    }

    /// <summary>
    /// A queue-creation type used only to satisfy the <c>TQueue : class, IJobQueueCreation</c>
    /// constraint on the two-generic <c>AddUpdateJob</c> overload.
    /// </summary>
    /// <remarks>
    /// The scheduler passes this type straight through to <see cref="IJobQueue"/>, which is
    /// substituted in these tests, so no member is ever invoked.
    /// </remarks>
    internal class NoOpJobQueueCreation : IJobQueueCreation
    {
        public QueueCreationResult CreateJobSchedulerQueue(Action<IContainer> registerService,
            QueueConnection queueConnection, Action<IContainer> setOptions = null, bool enableRoutes = false)
        {
            return new QueueCreationResult(QueueCreationStatus.Success);
        }

        public QueueRemoveResult RemoveQueue()
        {
            return new QueueRemoveResult(QueueRemoveStatus.Success);
        }

        public ICreationScope Scope => null;

        public bool IsDisposed => false;

        public void Dispose()
        {
        }
    }
}
