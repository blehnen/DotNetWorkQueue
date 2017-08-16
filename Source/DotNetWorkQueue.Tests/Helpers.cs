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
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DotNetWorkQueue.Configuration;
namespace DotNetWorkQueue.Tests
{
    public static class Helpers
    {
        private static readonly string AllowedChars =
         "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz#@$^*()";

        /// <summary>
        /// Creates a random collection of strings
        /// </summary>
        /// <param name="minLength">The minimum length.</param>
        /// <param name="maxLength">The maximum length.</param>
        /// <param name="count">The count.</param>
        /// <param name="rng">The random class to use.</param>
        /// <returns></returns>
        public static IEnumerable<string> RandomStrings(
           int minLength,
           int maxLength,
           int count,
           Random rng)
        {
            var chars = new char[maxLength];
            var setLength = AllowedChars.Length;

            while (count-- > 0)
            {
                var length = rng.Next(minLength, maxLength + 1);

                for (var i = 0; i < length; ++i)
                {
                    chars[i] = AllowedChars[rng.Next(setLength)];
                }

                yield return new string(chars, 0, length);
            }
        }
    }

    public class RpcSettingsNoOp : BaseRpcConnection
    {
        public override IConnectionInformation GetConnection(ConnectionTypes connectionType)
        {
            return new BaseConnectionInformation(string.Empty, String.Empty);
        }
    }
    public class FakeAMessageData : IAdditionalMessageData
    {
        private readonly ConcurrentDictionary<string, object> _settings;
        private readonly Dictionary<string, object> _headers;

        public FakeAMessageData()
        {
            _headers = new Dictionary<string, object>();
            _settings = new ConcurrentDictionary<string, object>();
        }
        public ICorrelationId CorrelationId
        {
            get;
            set;
        }

        public string Route { get; set; }

        public List<IAdditionalMetaData> AdditionalMetaData
        {
            get;
            set;
        }

        public IReadOnlyDictionary<string, object> Headers => new ReadOnlyDictionary<string, object>(_headers);

        public THeader GetHeader<THeader>(IMessageContextData<THeader> property) where THeader : class
        {
            return null;
        }

        public void SetHeader<THeader>(IMessageContextData<THeader> property, THeader value) where THeader : class
        {
            if(!_headers.ContainsKey(property.Name))
                _headers.Add(property.Name, value);
        }


        /// <summary>
        /// Sets a setting.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public void SetSetting(string name, object value)
        {
            _settings[name] = value;
        }

        /// <summary>
        /// Tries to get a setting
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        /// true if the setting was found
        /// </returns>
        public bool TryGetSetting(string name, out object value)
        {
            return _settings.TryGetValue(name, out value);
        }
    }

    public class FakeMessage
    {

    }
}
