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
using System.Threading;
using DotNetWorkQueue.Validation;
using Microsoft.Extensions.Logging;

namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// One worker's claim on the message it is processing: how old it is, whether it can still be proved,
    /// and what a beat's result says about it.
    /// </summary>
    /// <remarks>
    /// The decisions only. It owns no timer, thread or lock and never cancels anything itself: a method that
    /// finds the claim lost returns true, and <see cref="HeartBeatWorker"/> cancels processing. Every age is
    /// read from the injected clock, so the decisions can be tested on chosen times (GitHub #419).
    ///
    /// Its state is read and written with <see cref="Interlocked"/>. The staleness check runs on ticks where
    /// the beat itself is skipped, so it is read without the worker's beat lock.
    /// </remarks>
    internal sealed class HeartBeatClaim
    {
        //The window the monitor resets against. A message whose stored heartbeat is older than this is
        //treated as abandoned and handed to another worker, so it is also the deadline this worker has
        //to keep meeting to keep its claim.
        private readonly TimeSpan _expiry;
        //The transport's clock, not the machine's. The monitor ages the stored heartbeat against this
        //same source - on Redis it is the Redis server's time - so comparing against the local clock
        //here would measure the claim against a different clock than the one that reclaims it.
        private readonly IGetTime _getTime;
        private readonly IMessageContext _context;
        private readonly IMessageClaim _messageClaim;
        private readonly ILogger _logger;

        //Ticks of the last beat that actually landed.
        private long _lastGoodBeatUtcTicks;

        //Set when a beat matched no record after the expiry, or when the claim lapsed with nothing to
        //prove it by. Never from lateness alone while a beat can still prove it.
        //0 = still ours, 1 = given up; the message may belong to another worker now.
        private int _lostConfirmed;

        //so the "we are running late" warning is logged once per lapse rather than on every tick
        private int _lateWarningLogged;

        /// <summary>
        /// Initializes a new instance of the <see cref="HeartBeatClaim"/> class.
        /// </summary>
        /// <param name="expiry">The window the monitor resets against; zero or less means it never resets.</param>
        /// <param name="getTime">The transport's clock.</param>
        /// <param name="context">The message being processed.</param>
        /// <param name="messageClaim">Carries the heartbeat the de-queue stamped, if the transport stamped one.</param>
        /// <param name="logger">The logger.</param>
        public HeartBeatClaim(TimeSpan expiry, IGetTime getTime, IMessageContext context,
            IMessageClaim messageClaim, ILogger logger)
        {
            Guard.NotNull(getTime);
            Guard.NotNull(context);
            Guard.NotNull(messageClaim);
            Guard.NotNull(logger);

            _expiry = expiry;
            _getTime = getTime;
            _context = context;
            _messageClaim = messageClaim;
            _logger = logger;
        }

        /// <summary>
        /// Whether this worker has given the claim up. Once true it stays true, and no further beats are sent.
        /// </summary>
        /// <remarks>
        /// Two things set it. A beat that matched no record after the expiry, when the monitor can have
        /// reset the message (<see cref="BeatMatchedNoRecord"/>). Or a claim past the expiry that no beat
        /// can prove, because none has landed and the de-queue stamped nothing (<see cref="TryGiveUpAnUnprovableClaim"/>).
        /// The second is a precaution: no beat was sent, so it does not show that another worker owns the
        /// message, only that this one can no longer show it does.
        /// </remarks>
        public bool IsLost => Interlocked.CompareExchange(ref _lostConfirmed, 0, 0) == 1;

        /// <summary>
        /// The current time on the transport's clock.
        /// </summary>
        public DateTime Now() => _getTime.GetCurrentUtcDate();

        /// <summary>
        /// Starts the claim: it is fresh as of now, and the heartbeat the de-queue stamped is adopted if
        /// there is one.
        /// </summary>
        public void Begin()
        {
            //staleness is measured from here until a beat lands
            Interlocked.Exchange(ref _lastGoodBeatUtcTicks, Now().Ticks);
            SeedStatusFromTheDeQueue();
        }

        /// <summary>
        /// Says once, and only once, that no beat has landed inside the expiry.
        /// </summary>
        /// <remarks>
        /// Being late is not the same as having lost the message, and this used to treat it as if it
        /// were: a single tick past the expiry stopped the beats for good, so a slow moment became a
        /// guaranteed reset and a second worker finishing the same message. The clock can say we are
        /// late; only a beat can say the claim is gone (GitHub #328).
        /// </remarks>
        public void WarnOnceIfLate()
        {
            if (!HasLapsed() || Interlocked.Exchange(ref _lateWarningLogged, 1) != 0)
                return;

            var lateBy = Now() - LastGoodBeat();
            _logger.LogWarning(
                "No heartbeat has landed for message {MessageId} in {Age}, past the {Expiry} the monitor resets against. Still beating: if the message has been given to another worker the next update will not match it, and processing stops then",
                _context.MessageId?.Id?.Value, lateBy, _expiry);
        }

        /// <summary>
        /// Gives the claim up rather than sending an update that could renew somebody else's, and says
        /// whether it did. When it returns true the caller must cancel processing.
        /// </summary>
        /// <remarks>
        /// A beat proves the claim is still ours by naming the heartbeat this worker last wrote, and
        /// until the first one lands there is nothing to name. An update that carries no such value
        /// matches whatever the record holds, including a claim the monitor has already handed to
        /// somebody else - it would then renew that worker's claim and, because the value it writes is
        /// not one that worker knows about, break the ownership check on the beats that legitimately own
        /// the message. Sending it is only safe while the claim is fresh, which is exactly while the
        /// monitor cannot have reset it; past the expiry with nothing to prove ownership with, the claim
        /// has to be treated as gone (GitHub #328).
        ///
        /// Callers must hold the beat lock. Outside it, a first beat that is merely slow looks identical
        /// to one that never happened, and cancelling on that would turn a slow transport into a lost
        /// message - the thing this whole change exists to stop. Under the lock, any beat that was in
        /// flight has finished and published its result.
        /// </remarks>
        public bool TryGiveUpAnUnprovableClaim()
        {
            if (LastWrittenHeartBeat().HasValue || !HasLapsed())
                return false;

            Interlocked.Exchange(ref _lostConfirmed, 1);
            _logger.LogError(
                "No heartbeat has landed for message {MessageId} since it was picked up, and the claim is now past the {Expiry} the monitor resets against. There is nothing left to show the message is still this worker's, so processing is being cancelled rather than renewing a claim that may now belong to another worker",
                _context.MessageId?.Id?.Value, _expiry);
            return true;
        }

        /// <summary>
        /// Records a beat that landed, and publishes its status for the next beat and the rollback to
        /// prove ownership with.
        /// </summary>
        /// <param name="status">What the transport returned; its <see cref="IHeartBeatStatus.LastHeartBeatTime"/> is set.</param>
        /// <param name="sentAt">When the update was sent, from <see cref="Now"/>.</param>
        /// <remarks>
        /// Freshness is measured from when the update was sent, not from when the reply came back. A slow
        /// reply would otherwise start a new local window while the record the monitor ages was already
        /// older than that - this errs on the side of believing the claim is staler than it is, which is
        /// the safe direction. The stored timestamp is not used: it can come from a different clock.
        /// </remarks>
        public void BeatLanded(IHeartBeatStatus status, DateTime sentAt)
        {
            Interlocked.Exchange(ref _lastGoodBeatUtcTicks, sentAt.Ticks);
            Interlocked.Exchange(ref _lateWarningLogged, 0);
            _context.WorkerNotification.HeartBeat.Status = status;
            if (_logger.IsEnabled(LogLevel.Trace))
                _logger.LogTrace("Set heartbeat for message {MessageId}", status.MessageId.Id.Value);
        }

        /// <summary>
        /// Decides what a beat that updated nothing means. When it returns true the claim is lost and the
        /// caller must cancel processing.
        /// </summary>
        /// <remarks>
        /// Every transport reports this the same way - the relational handlers return null when rows
        /// affected is not one, Redis returns zero once the message has left the working set, LiteDb
        /// leaves the date unset when the record is not found - but it does not by itself mean the
        /// message was taken away. It is also what a beat sees when the message has just been finished
        /// and its row removed while that beat was in flight, which is ordinary and happens on every
        /// queue.
        ///
        /// What separates the two is the age of our own claim. The monitor only resets a message whose
        /// stored heartbeat is older than the expiry, so a message that was genuinely taken from us was
        /// late first. A row that vanishes while our claim is fresh is this worker finishing its own
        /// message.
        /// </remarks>
        public bool BeatMatchedNoRecord(IHeartBeatStatus status)
        {
            if (HasLapsed())
            {
                Interlocked.Exchange(ref _lostConfirmed, 1);
                _logger.LogError(
                    "The heartbeat for message {MessageId} matched no record and this worker's claim was already past the {Expiry} the monitor resets against, so the message has been given to another worker. Processing is being cancelled",
                    status.MessageId?.Id?.Value, _expiry);
                return true;
            }

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(
                    "The heartbeat for message {MessageId} matched no record while the claim was still fresh, which is the message being completed while this beat was in flight",
                    status.MessageId?.Id?.Value);
            }
            return false;
        }

        /// <summary>
        /// Whether this worker's claim has aged past the expiry, so the monitor may already have handed the
        /// message to another worker.
        /// </summary>
        /// <remarks>
        /// The monitor resets any message whose stored heartbeat is older than the expiry. Until this was
        /// checked the original worker carried on and committed, so a lapsed heartbeat produced two workers
        /// finishing the same message rather than the redelivery it is supposed to produce.
        /// </remarks>
        public bool HasLapsed()
        {
            if (_expiry <= TimeSpan.Zero)
                return false; //no expiry configured, so nothing resets the message and the claim cannot lapse

            return Now() - LastGoodBeat() >= _expiry;
        }

        private DateTime LastGoodBeat() =>
            new DateTime(Interlocked.Read(ref _lastGoodBeatUtcTicks), DateTimeKind.Utc);

        /// <summary>
        /// Adopts the heartbeat the de-queue stamped, so the claim can be proved before this worker has
        /// written one of its own.
        /// </summary>
        /// <remarks>
        /// Both places that prove ownership read <see cref="IWorkerHeartBeatNotification.Status"/> - the
        /// beat names the value it expects to replace, and the rollback names the value it expects to
        /// reset - so publishing the de-queue's stamp here fixes both without either of them changing.
        ///
        /// Until this existed the status was null until the first beat landed, which left the beat
        /// giving up a claim it could not name and the rollback resetting a row without checking whose
        /// it was (GitHub #336).
        ///
        /// A transport that stamps nothing leaves this absent, and the older behaviour stands: that is a
        /// de-queue which deletes the record rather than marking it, or a queue with heartbeats off.
        /// </remarks>
        private void SeedStatusFromTheDeQueue()
        {
            if (_context.MessageId == null || !_context.MessageId.HasValue)
                return;

            var claimedAt = _context.Get(_messageClaim.ClaimedAt);
            if (claimedAt == null)
                return;

            _context.WorkerNotification.HeartBeat.Status =
                new HeartBeatStatus(_context.MessageId, claimedAt.Value);
        }

        /// <summary>
        /// The heartbeat this worker last wrote, or null when none has landed yet.
        /// </summary>
        /// <remarks>
        /// The transports read the same value when they build the ownership test into the update, so
        /// reading it from the same place keeps the decision here and the comparison there in step.
        /// </remarks>
        private DateTime? LastWrittenHeartBeat() =>
            _context.WorkerNotification?.HeartBeat?.Status?.LastHeartBeatTime;
    }
}
