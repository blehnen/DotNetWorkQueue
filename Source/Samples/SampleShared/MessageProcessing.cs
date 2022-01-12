using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetWorkQueue;
using DotNetWorkQueue.Logging;
using Microsoft.Extensions.Logging;

namespace SampleShared
{
    public static class MessageProcessing
    {
        private static readonly ConcurrentDictionary<string, int> RetryErrorCount = new ConcurrentDictionary<string, int>();

        public static void HandleMessages(IReceivedMessage<SimpleMessage> arg1, IWorkerNotification arg2)
        {
            arg2.Log.LogInformation($"Processing message {arg1.MessageId.Id.Value.ToString()} - Processing time is {arg1.Body.ProcessingTime}");

            if (arg1.Body.Error == ErrorTypes.Error)
            {
                //simulate some processing
                System.Threading.Thread.Sleep(100);

                var i = 9 - 9;
                var result = 100 / i;
                arg2.Log.LogInformation(result.ToString());
            }
            else if (arg1.Body.Error == ErrorTypes.RetryableErrorFail)
            {
                foreach (var error in arg1.PreviousErrors)
                {
                    arg2.Log.LogInformation($"previous error {error.Key}, count {error.Value}");
                }

                //simulate some processing
                System.Threading.Thread.Sleep(100);
                throw new InvalidDataException("the data is invalid. We will retry a few times and then give up because this error will happen over and over");
            }
            else if (arg1.Body.Error == ErrorTypes.RetryableError)
            {
                //simulate some processing
                System.Threading.Thread.Sleep(100);

                if (!RetryErrorCount.ContainsKey(arg1.MessageId.Id.Value.ToString()))
                {
                    RetryErrorCount.TryAdd(arg1.MessageId.Id.Value.ToString(), 1);
                    throw new InvalidDataException("the data is invalid");
                }
                else if (RetryErrorCount[arg1.MessageId.Id.Value.ToString()] > 2)
                {
                    //complete
                    foreach (var error in arg1.PreviousErrors)
                    {
                        arg2.Log.LogInformation($"previous error {error.Key}, count {error.Value}");
                    }
                }
                else
                {
                    RetryErrorCount[arg1.MessageId.Id.Value.ToString()] = RetryErrorCount[arg1.MessageId.Id.Value.ToString()] + 1;
                    foreach (var error in arg1.PreviousErrors)
                    {
                        arg2.Log.LogInformation($"previous error {error.Key}, count {error.Value}");
                    }
                    throw new InvalidDataException("the data is invalid");
                }
            }

            //allow canceling if the transport supports rolling back
            if (arg2.TransportSupportsRollback)
            {
                //NOTE - there are two tokens
                //Stop - the queue is asking you stop soon if possible. Eventually, the cancel token will be fired
                //Cancel - the queue is requesting that you stop ASAP
                //you can check either token or both
                //to check both, create a composite toke
                //https://docs.microsoft.com/en-us/dotnet/standard/threading/how-to-listen-for-multiple-cancellation-requests

                var canceled =
                    arg2.WorkerStopping.StopWorkToken.WaitHandle.WaitOne(
                        TimeSpan.FromMilliseconds(arg1.Body.ProcessingTime));

                if (canceled) throw new OperationCanceledException("We have been asked to stop working"); //force a requeue
            }
            else
                System.Threading.Thread.Sleep(arg1.Body.ProcessingTime);

            arg2.Log.LogInformation($"Message {arg1.MessageId.Id.Value.ToString()} complete");
        }
    }
}
