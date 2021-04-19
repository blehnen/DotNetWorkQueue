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
namespace DotNetWorkQueue.Metrics.Decorator
{
    internal class SendHeartBeatDecorator: ISendHeartBeat
    {
        private readonly ISendHeartBeat _handler;
        private readonly ITimer _timer;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendHeartBeatDecorator" /> class.
        /// </summary>
        /// <param name="metrics">The metrics factory.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="connectionInformation">The connection information.</param>
        public SendHeartBeatDecorator(IMetrics metrics,
            ISendHeartBeat handler,
            IConnectionInformation connectionInformation)
        {
            var name = "SendHeartBeat";
            _timer = metrics.Timer($"{connectionInformation.QueueName}.{name}.SendTimer", Units.Calls);
            _handler = handler;
        }

        /// <summary>
        /// Updates the heart beat for a record.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public IHeartBeatStatus Send(IMessageContext context)
        {
            using (_timer.NewContext())
            {
                return _handler.Send(context);
            }
        }
    }
}
