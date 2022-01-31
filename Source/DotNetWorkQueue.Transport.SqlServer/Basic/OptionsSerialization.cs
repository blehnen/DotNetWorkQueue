// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2022 Brian Lehnen
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
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SqlServer.Basic
{
    internal class OptionsSerialization : IOptionsSerialization
    {
        private readonly IInternalSerializer _serializer;
        private readonly Lazy<SqlServerMessageQueueTransportOptions> _options;

        public OptionsSerialization(ISqlServerMessageQueueTransportOptionsFactory options,
            IInternalSerializer serializer)
        {
            Guard.NotNull(() => options, options);
            Guard.NotNull(() => serializer, serializer);

            _serializer = serializer;
            _options = new Lazy<SqlServerMessageQueueTransportOptions>(options.Create);
        }
        public byte[] ConvertToBytes()
        {
            return _serializer.ConvertToBytes(_options.Value);
        }
    }
}
