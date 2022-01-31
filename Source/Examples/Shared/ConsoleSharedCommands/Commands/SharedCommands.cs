// ---------------------------------------------------------------------
// Copyright © 2015-2020 Brian Lehnen
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
using System.Linq;
using System.Text;
using ConsoleShared;
using DotNetWorkQueue;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Interceptors;
using App.Metrics;

namespace ConsoleSharedCommands.Commands
{
    public abstract class SharedCommands : IConsoleCommand, IDisposable
    {
        protected DotNetWorkQueue.AppMetrics.Metrics Metrics;
        private App.Metrics.IMetricsRoot _metricsRoot;

        protected bool Gzip;
        protected bool Des;
        protected TripleDesMessageInterceptorConfiguration DesConfiguration;

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
            help.AppendLine(ConsoleFormatting.FixedLength("EnableMetrics",
                "Enables queue metrics"));
            help.AppendLine(ConsoleFormatting.FixedLength("ViewMetrics", "Displays any captured metrics"));
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

        public ConsoleExecuteResult ViewMetrics()
        {
            if (Metrics != null)
            {
                var tasks = _metricsRoot.ReportRunner.RunAllAsync();
                System.Threading.Tasks.Task.WaitAll(tasks.ToArray());
            }
            return new ConsoleExecuteResult("Metric reports have been run");
        }
        public ConsoleExecuteResult EnableMetrics()
        {
            if (Metrics != null)
                return new ConsoleExecuteResult("Metrics already enabled");

            _metricsRoot = new App.Metrics.MetricsBuilder()
                .Configuration.Configure(
                    options =>
                    {
                        options.DefaultContextLabel = "ExampleApp";
                        options.Enabled = true;
                        options.ReportingEnabled = true;
                    })
                .Report.ToConsole(
                    options =>
                    {
                        options.FlushInterval = TimeSpan.FromSeconds(5);
                    })
                .Build();

            Metrics = new DotNetWorkQueue.AppMetrics.Metrics(_metricsRoot);
            return new ConsoleExecuteResult($"Metrics enabled");
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
        }
    }

    public enum ConsumerQueueTypes
    {
        Poco,
        Method
    }
}
