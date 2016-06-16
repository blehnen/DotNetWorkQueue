// ---------------------------------------------------------------------
// Copyright (c) 2016 Brian Lehnen
// 
// All rights reserved.
// 
// MIT License
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// ---------------------------------------------------------------------
using System;
using System.Text;
using ConsoleShared;
using DotNetWorkQueue;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Interceptors;
using Metrics;

namespace ConsoleSharedCommands.Commands
{
    public abstract class SharedCommands: IConsoleCommand, IDisposable
    {
        protected readonly Lazy<QueueStatusContainer> QueueStatusContainer;
        protected DotNetWorkQueue.Metrics.Net.Metrics Metrics;
        protected IQueueStatus QueueStatus;

        protected bool Gzip;
        protected bool Des;
        protected TripleDesMessageInterceptorConfiguration DesConfiguration;

        protected SharedCommands()
        {
            QueueStatusContainer = new Lazy<QueueStatusContainer>(() => new QueueStatusContainer());
        }

        public abstract ConsoleExecuteResult Info { get; }
        public virtual ConsoleExecuteResult Example(string command)
        {
            switch (command)
            {
                case "EnableStatus":
                    return new ConsoleExecuteResult("EnableStatus http://localhost:10000/");
                case "EnableMetrics":
                    return new ConsoleExecuteResult("EnableMetrics http://localhost:10001/ false");
                case "EnableGzip":
                    return new ConsoleExecuteResult("EnableGzip");
                case "EnableDes":
                    return new ConsoleExecuteResult("EnableDes aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa aaaaaaaaaaa=");
            }
            return new ConsoleExecuteResult("Command not found");
        }
        public virtual ConsoleExecuteResult Help()
        {
            var help = new StringBuilder();
            help.AppendLine("");
            help.AppendLine("-The following should be enabled before performing queue actions; usage is optional-");
            help.AppendLine(ConsoleFormatting.FixedLength("EnableStatus uri", "Enables the status HTTP server"));
            help.AppendLine(ConsoleFormatting.FixedLength("EnableMetrics uri [performanceCounters]",
                "Enables queue metrics"));
            help.AppendLine(ConsoleFormatting.FixedLength("EnableGzip", "Enables the Gzip message interceptor"));
            help.AppendLine(ConsoleFormatting.FixedLength("EnableDes [key] [iv]",
                "Enables Triple DES message interceptor; key/iv must be base64 strings"));
            help.AppendLine("");
            return new ConsoleExecuteResult(help.ToString());
        }

        protected abstract ConsoleExecuteResult ValidateQueue(string queueName);

        public ConsoleExecuteResult EnableGzip()
        {
            Gzip = true;
            return new ConsoleExecuteResult("gzip compression has been enabled");
        }

        public ConsoleExecuteResult EnableDes(string key = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", string iv = "aaaaaaaaaaa=")
        {
            Des = true;
            DesConfiguration = new TripleDesMessageInterceptorConfiguration(Convert.FromBase64String(key), Convert.FromBase64String(iv));
            return new ConsoleExecuteResult("triple des encryption has been enabled");
        }
        public ConsoleExecuteResult EnableStatus(string location)
        {
            if (QueueStatus != null)
                return new ConsoleExecuteResult("status listener already enabled");

            if (!Uri.IsWellFormedUriString(location, UriKind.Absolute))
            {
                throw new ArgumentException($"{location} is not a valid Uri");
            }

            QueueStatus = QueueStatusContainer.Value.CreateStatus();
            QueueStatus.Options().ListenerAddress = new Uri(location);
            QueueStatus.Start();
            return new ConsoleExecuteResult($"status listener started at {location}", new ConsoleExecuteAction(ConsoleExecuteActions.StatusUri, location));
        }
        public ConsoleExecuteResult EnableMetrics(string location, bool performanceCounters = false)
        {
            if (Metrics != null)
                return new ConsoleExecuteResult("Metrics already enabled");

            if (!Uri.IsWellFormedUriString(location, UriKind.Absolute))
            {
                throw new ArgumentException($"{location} is not a valid Uri");
            }

            if (!location.EndsWith("/"))
                location = location + "/";

            Metrics = new DotNetWorkQueue.Metrics.Net.Metrics("ExampleApp");
            Metrics.Config.WithHttpEndpoint(location);
            if (performanceCounters)
            {
                Metrics.Config.WithAllCounters();
            }

            return new ConsoleExecuteResult($"Metrics enabled at {location}", new ConsoleExecuteAction(ConsoleExecuteActions.StartProcess, location));
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            Metrics?.Dispose();
            QueueStatus?.Dispose();
            if (QueueStatusContainer.IsValueCreated)
            {
                QueueStatusContainer.Value.Dispose();
            }
        }
    }

    public enum ConsumerQueueTypes
    {
        Poco,
        Method
    }
}
