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
using System.Collections.Generic;
using System.Threading;
using OpenTelemetry.Trace;

namespace DotNetWorkQueue.Trace.Decorator
{
    /// <summary>
    /// Tracer for heart beat resetting
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.IResetHeartBeat" />
    public class ResetHeartBeatDecorator: IResetHeartBeat
    {
        private readonly Tracer _tracer;
        private readonly IResetHeartBeat _handler;
        private readonly IStandardHeaders _headers;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResetHeartBeatDecorator"/> class.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="tracer">The tracer.</param>
        /// <param name="headers">The headers.</param>
        public ResetHeartBeatDecorator(IResetHeartBeat handler,  Tracer tracer, IStandardHeaders headers)
        {
            _handler = handler;
            _tracer = tracer;
            _headers = headers;
        }

        /// <inheritdoc />
        public List<ResetHeartBeatOutput> Reset(CancellationToken cancelToken)
        {
            var results = _handler.Reset(cancelToken);
            foreach (var result in results)
            {
                if (result.Headers != null)
                {
                    var spanContext = result.Headers.Extract(_tracer, _headers);
                    using (var scope = _tracer.StartActiveSpan("ResetHeartBeat", parentContext: spanContext, startTime: result.ApproximateResetTimeStart))
                    {
                        scope.AddMessageIdTag(result.MessageId);
                        scope.End(result.ApproximateResetTimeEnd);
                    }
                }
                else
                {
                    using (var scope = _tracer.StartActiveSpan("ResetHeartBeat", startTime: result.ApproximateResetTimeStart))
                    {
                        scope.AddMessageIdTag(result.MessageId);
                        scope.End(result.ApproximateResetTimeEnd);
                    }
                }
            }

            return results;
        }
    }
}
