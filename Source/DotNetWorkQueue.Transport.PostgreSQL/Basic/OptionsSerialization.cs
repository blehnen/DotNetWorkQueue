using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic
{
    internal class OptionsSerialization : IOptionsSerialization
    {
        private readonly IInternalSerializer _serializer;
        private readonly Lazy<PostgreSqlMessageQueueTransportOptions> _options;

        public OptionsSerialization(IPostgreSqlMessageQueueTransportOptionsFactory options,
            IInternalSerializer serializer)
        {
            Guard.NotNull(() => options, options);
            Guard.NotNull(() => serializer, serializer);

            _serializer = serializer;
            _options = new Lazy<PostgreSqlMessageQueueTransportOptions>(options.Create);
        }
        public byte[] ConvertToBytes() 
        {
            return _serializer.ConvertToBytes(_options.Value);
        }
    }
}
