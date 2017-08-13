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
