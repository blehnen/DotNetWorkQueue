// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2018 Brian Lehnen
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
using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Logging;

namespace DotNetWorkQueue.QueueStatus
{
    /// <summary>
    /// Returns queue status information, via listening on an HTTP port
    /// </summary>
    internal class QueueStatusHttp : IQueueStatus
    {
        private readonly ConcurrentBag<IQueueStatusProvider> _queueStatusProviders;

        private const string NotFoundResponse = @"<!DOCTYPE html><html><body>invalid request</body></html>";
        private HttpListener _httpListener;
        private CancellationTokenSource _cancelTokenSource;
        private string _prefixPath;

        private Task _processingTask;

        private readonly ILog _log;
        private readonly QueueStatusHttpConfiguration _configuration;
        private readonly IInternalSerializer _serializer;

        private int _disposeCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueueStatusHttp"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="additionalConfiguration">The additional configuration.</param>
        /// <param name="serializer">The serializer.</param>
        /// <param name="log">The log.</param>
        public QueueStatusHttp(
            QueueStatusHttpConfiguration configuration,
            IConfiguration additionalConfiguration,
            IInternalSerializer serializer,
            ILogFactory log)
        {
            _log = log.Create();
            _configuration = configuration;
            _serializer = serializer;
            Configuration = additionalConfiguration;
            _queueStatusProviders = new ConcurrentBag<IQueueStatusProvider>();
            Configuration.SetSetting("QueueStatusHttpConfiguration", _configuration);
        }

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        /// <value>
        /// The configuration.
        /// </value>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// Adds a status provider
        /// </summary>
        /// <param name="provider">The provider.</param>
        public void AddStatusProvider(IQueueStatusProvider provider)
        {
            _queueStatusProviders.Add(provider);
        }

        /// <summary>
        /// Parses the prefix path.
        /// </summary>
        /// <param name="listenerUriPrefix">The listener URI prefix.</param>
        /// <returns></returns>
        private string ParsePath(string listenerUriPrefix)
        {
            var match = Regex.Match(listenerUriPrefix, @"http://(?:[^/]*)(?:\:\d+)?/(.*)");
            return match.Success ? match.Groups[1].Value.ToLowerInvariant() : string.Empty;
        }

        /// <summary>
        /// Starts this instance.
        /// </summary>
        public void Start()
        {
            _cancelTokenSource = new CancellationTokenSource();
            _prefixPath = ParsePath(_configuration.ListenerAddress.ToString());
            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add(_configuration.ListenerAddress.ToString());

            _httpListener.Start();
            _processingTask = Task.Factory.StartNew(async () => await ProcessRequestAsync().ConfigureAwait(false), TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// Processes the requests.
        /// </summary>
        /// <returns></returns>
        private async Task ProcessRequestAsync()
        {
            while (!_cancelTokenSource.IsCancellationRequested)
            {
                try
                {
                    var context = await _httpListener.GetContextAsync().ConfigureAwait(false);
                    try
                    {
                        await ProcessRequestAsync(context).ConfigureAwait(false);
                        context.Response.Close();
                    }
                    catch (Exception ex)
                    {
                        context.Response.StatusCode = 500;
                        context.Response.StatusDescription = "Internal Server Error";
                        context.Response.Close();
                        _log.ErrorException("Error processing HTTP request", ex);
                    }
                }
                // ReSharper disable once UncatchableException
                catch (ObjectDisposedException ex)
                {
                    if (ex.ObjectName == _httpListener.GetType().FullName && _httpListener.IsListening == false)
                    {
                        return; // listener is closed/disposed
                    }
                    _log.ErrorException("Error processing HTTP request", ex);
                }
                catch (Exception ex)
                {
                    if (!(ex is HttpListenerException listenerException) || listenerException.ErrorCode != 995)// IO operation aborted
                    {
                        _log.ErrorException("Error processing HTTP request", ex);
                    }
                }
            }
        }

        /// <summary>
        /// Processes the request.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        private Task ProcessRequestAsync(HttpListenerContext context)
        {
            if (context.Request.HttpMethod.ToUpperInvariant() != "GET")
            {
                return WriteNotFoundAsync(context);
            }

            var urlPath = context.Request.RawUrl.Substring(_prefixPath.Length)
                .ToLowerInvariant();

            switch (urlPath)
            {
                case "/ping":
                    return WritePongAsync(context);
                case "/":
                case "/status":
                    return WriteStatusAsync(context);
                default:
                    var providers = _queueStatusProviders.ToList();
                    var outputs = providers.Select(provider => provider.HandlePath(urlPath)).Where(output => output != null).ToList();
                    if (outputs.Count > 0)
                    {
                        return WriteStringAsync(context, _serializer.ConvertToString(outputs), "application/json");
                    }
                    break;
            }
            return WriteNotFoundAsync(context);
        }

        /// <summary>
        /// Writes the status.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        private Task WriteStatusAsync(HttpListenerContext context)
        {
            var status = new QueueStatus(_queueStatusProviders.ToList());
            return WriteStringAsync(context, _serializer.ConvertToString(status), "application/json");
        }
        /// <summary>
        /// Writes the reply to a ping.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        private Task WritePongAsync(HttpListenerContext context)
        {
            return WriteStringAsync(context, "pong", "text/plain");
        }

        /// <summary>
        /// Writes the not found reply.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        private Task WriteNotFoundAsync(HttpListenerContext context)
        {
            return WriteStringAsync(context, NotFoundResponse, "text/html", 404, "NOT FOUND");
        }

        /// <summary>
        /// Writes the string.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="data">The data.</param>
        /// <param name="contentType">Type of the content.</param>
        /// <param name="httpStatus">The HTTP status.</param>
        /// <param name="httpStatusDescription">The HTTP status description.</param>
        /// <returns></returns>
        private async Task WriteStringAsync(HttpListenerContext context, string data, string contentType,
            int httpStatus = 200, string httpStatusDescription = "OK")
        {
            AddCorsHeaders(context.Response);
            AddNoCacheHeaders(context.Response);

            context.Response.ContentType = contentType;
            context.Response.StatusCode = httpStatus;
            context.Response.StatusDescription = httpStatusDescription;

            var acceptsGzip = AcceptsGzip(context.Request);
            if (!acceptsGzip)
            {
                using (var writer = new StreamWriter(context.Response.OutputStream, Encoding.UTF8, 4096, true))
                {
                    await writer.WriteAsync(data).ConfigureAwait(false);
                }
            }
            else
            {
                context.Response.AddHeader("Content-Encoding", "gzip");
                using (var gzip = new GZipStream(context.Response.OutputStream, CompressionMode.Compress, true))
                using (var writer = new StreamWriter(gzip, Encoding.UTF8, 4096, true))
                {
                    await writer.WriteAsync(data).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Returns true if the requester accepts gzip content.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        private bool AcceptsGzip(HttpListenerRequest request)
        {
            var encoding = request.Headers["Accept-Encoding"];
            return !string.IsNullOrEmpty(encoding) && encoding.Contains("gzip");
        }

        /// <summary>
        /// Adds the no cache headers.
        /// </summary>
        /// <param name="response">The response.</param>
        private void AddNoCacheHeaders(HttpListenerResponse response)
        {
            response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate");
            response.Headers.Add("Pragma", "no-cache");
            response.Headers.Add("Expires", "0");
        }

        /// <summary>
        /// Adds the cors headers.
        /// </summary>
        /// <param name="response">The response.</param>
        private void AddCorsHeaders(HttpListenerResponse response)
        {
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Access-Control-Allow-Headers", "Origin, X-Requested-With, Content-Type, Accept");
        }

        /// <summary>
        /// Stops this instance.
        /// </summary>
        private void Stop()
        {
            _cancelTokenSource?.Cancel();
            if (_processingTask != null && !_processingTask.IsCompleted)
            {
                _processingTask.Wait();
            }
            if (_httpListener != null && _httpListener.IsListening)
            {
                _httpListener.Stop();
                _httpListener.Prefixes.Clear();
            }
        }

        #region IDispose, IIsDisposed
        /// <summary>
        /// Throws an exception if this instance has been disposed.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <exception cref="System.ObjectDisposedException"></exception>
        protected void ThrowIfDisposed([CallerMemberName] string name = "")
        {
            if (Interlocked.CompareExchange(ref _disposeCount, 0, 0) != 0)
            {
                throw new ObjectDisposedException(name);
            }
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
        [SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "_cancelTokenSource", Justification = "not needed")]
        [SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "_httpListener", Justification = "not needed")]
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            if (Interlocked.Increment(ref _disposeCount) != 1) return;

            Stop();

            lock (_queueStatusProviders)
            {
                while (!_queueStatusProviders.IsEmpty)
                {
                    _queueStatusProviders.TryTake(out _);
                }
            }

            _httpListener?.Close();
            _cancelTokenSource?.Dispose();
        }

        /// <summary>
        /// Gets a value indicating whether this instance is disposed.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is disposed; otherwise, <c>false</c>.
        /// </value>
        public bool IsDisposed => Interlocked.CompareExchange(ref _disposeCount, 0, 0) != 0;

        #endregion
    }
}
