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

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DotNetWorkQueue.Transport.SQLite.Shared.Tests
{
    public class FakeMessage
    {

    }
    public class FakeAMessageData : IAdditionalMessageData
    {
        private readonly Dictionary<string, object> _headers;

        public FakeAMessageData()
        {
            _headers = new Dictionary<string, object>();
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
            if (!_headers.ContainsKey(property.Name))
                _headers.Add(property.Name, value);
        }


        public void SetSetting(string name, object value)
        {
            
        }

        public bool TryGetSetting(string name, out object value)
        {
            value = null;
            return false;
        }
    }
}
