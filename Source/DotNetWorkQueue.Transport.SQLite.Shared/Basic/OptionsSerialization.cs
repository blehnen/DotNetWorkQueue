using System;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Shared.Basic
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.Transport.RelationalDatabase.IOptionsSerialization" />
    public class OptionsSerialization : IOptionsSerialization
    {
        private readonly IInternalSerializer _serializer;
        private readonly Lazy<SqLiteMessageQueueTransportOptions> _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionsSerialization"/> class.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="serializer">The serializer.</param>
        public OptionsSerialization(ISqLiteMessageQueueTransportOptionsFactory options,
            IInternalSerializer serializer)
        {
            Guard.NotNull(() => options, options);
            Guard.NotNull(() => serializer, serializer);

            _serializer = serializer;
            _options = new Lazy<SqLiteMessageQueueTransportOptions>(options.Create);
        }
        /// <summary>
        /// Converts to bytes.
        /// </summary>
        /// <returns></returns>
        public byte[] ConvertToBytes()
        {
            return _serializer.ConvertToBytes(_options.Value);
        }
    }
}
