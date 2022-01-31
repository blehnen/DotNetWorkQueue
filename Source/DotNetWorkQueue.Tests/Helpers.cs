﻿using System;
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
    public class FakeAMessageData : IAdditionalMessageData
    {
        private readonly ConcurrentDictionary<string, object> _settings;
        private readonly Dictionary<string, object> _headers;

        public FakeAMessageData()
        {
            _headers = new Dictionary<string, object>();
            _settings = new ConcurrentDictionary<string, object>();
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

        /// <inheritdoc />
        public void SetSetting(string name, object value)
        {
            _settings[name] = value;
        }
        /// <inheritdoc />
        public bool TryGetSetting(string name, out object value)
        {
            return _settings.TryGetValue(name, out value);
        }

        /// <summary>
        /// Tags that will be added to the trace when sending a message
        /// </summary>
        /// <value>
        /// The trace tags.
        /// </value>
        public IDictionary<string, string> TraceTags { get; }
    }

    public class FakeMessage
    {

    }
}
