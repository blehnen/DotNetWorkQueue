using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DotNetWorkQueue.Transport.SqlServer.Tests
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
            TraceTags = new Dictionary<string, string>();
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

        /// <summary>
        /// Tags that will be added to the trace when sending a message
        /// </summary>
        /// <value>
        /// The trace tags.
        /// </value>
        public IDictionary<string, string> TraceTags { get; }
    }
}
