using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetWorkQueue;
using DotNetWorkQueue.Messages;

namespace SampleShared
{
    public static class Messages
    {
        public static SimpleMessage CreateSimpleMessage(int messagePayloadLength, int processingTime)
        {
            var message = new SimpleMessage
            {
                Message = RandomString.Create(messagePayloadLength),
                ProcessingTime = processingTime
            };
            return message;
        }

        public static SimpleMessage CreateSimpleExpiredMessage()
        {
            var message = new SimpleMessage
            {
                Message = RandomString.Create(100),
                ProcessingTime = 100
            };
            return message;
        }
        public static IEnumerable<SimpleMessage> CreateSimpleMessageError(int messagePayloadLength, int processingTime)
        {
            var message = new SimpleMessage
            {
                Message = RandomString.Create(messagePayloadLength),
                ProcessingTime = processingTime,
                Error = ErrorTypes.Error
            };
            yield return message;
        }

        public static IEnumerable<SimpleMessage> CreateSimpleMessageRetryError(int messagePayloadLength, bool failAtEnd)
        {
            var message = new SimpleMessage
            {
                Message = RandomString.Create(messagePayloadLength),
                ProcessingTime = 100,
                Error = failAtEnd ? ErrorTypes.RetryableErrorFail : ErrorTypes.RetryableError,
            };
            yield return message;
        }

        public static IEnumerable<SimpleMessage> CreateSimpleMessage(int count, int sleepTime, int size)
        {
            for (var i = 0; i < count; i++)
            {
                yield return Messages.CreateSimpleMessage(size, sleepTime);
            }
        }

        public static IEnumerable<SimpleMessage> CreateSimpleMessageRandom(int count)
        {
            var random = new Random();
            for (var i = 0; i < count; i++)
            {
                yield return Messages.CreateSimpleMessage(random.Next(1, 1000000), random.Next(0, 3000));
            }
        }

        public static List<SimpleMessage> CreateSimpleMessageRandomList(int count)
        {
            var random = new Random();
            var data = new List<SimpleMessage>(count);
            for (var i = 0; i < count; i++)
            {
                data.Add(Messages.CreateSimpleMessage(random.Next(1, 10000), random.Next(0, 3000)));
            }
            return data;
        }
    }
}
