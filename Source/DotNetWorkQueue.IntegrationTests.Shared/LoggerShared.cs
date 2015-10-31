// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015 Brian Lehnen
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
using System.Globalization;
using System.IO;
using DotNetWorkQueue.Logging;
using FluentAssertions;
using Xunit;
namespace DotNetWorkQueue.IntegrationTests.Shared
{
    public static class LoggerShared
    {
        public static ILogProvider Create(string queueName)
        {
            return Create(queueName, LogLevel.Error);
        }

        public static ILogProvider Create(string queueName, LogLevel logLevel)
        {
            if(!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\"))
                Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\");

            return
                new TextFileLogProvider(
                    AppDomain.CurrentDomain.BaseDirectory + $"\\Logs\\{queueName}.txt", logLevel);
        }

        public static void ShouldHaveErrors(string queueName)
        {
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory +
                            $"\\Logs\\{queueName}.txt"))
            {
                using (var fileStream = new FileStream(AppDomain.CurrentDomain.BaseDirectory +
                                                       $"\\Logs\\{queueName}.txt", FileMode.Open,
                    FileAccess.Read, FileShare.ReadWrite))
                using (var textReader = new StreamReader(fileStream))
                {
                    var errors = textReader.ReadToEnd();
                    errors.Should()
                        .NotBeNullOrWhiteSpace(
                            $"errors should have occured, however no errors where found for queue {queueName}");
                }
            }
            else
            {
                Assert.False(true, $"No error file was found; errors should have occured for queue {queueName}");
            }
        }
        public static void CheckForErrors(string queueName)
        {
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory +
                            $"\\Logs\\{queueName}.txt"))
            {
                using (var fileStream = new FileStream(AppDomain.CurrentDomain.BaseDirectory +
                                                       $"\\Logs\\{queueName}.txt", FileMode.Open,
                    FileAccess.Read, FileShare.ReadWrite))
                using (var textReader = new StreamReader(fileStream))
                {
                    var errors = textReader.ReadToEnd();
                    errors.Should()
                        .BeEmpty("No errors should have occured, however the following errors where found {0}", errors);
                }
            }
        }
    }
    public class TextFileLogProvider : ILogProvider
    {
        private static readonly object Locker = new object();

        private readonly string _fileName;
        private readonly LogLevel _logLevel;

        public TextFileLogProvider(string fileName, LogLevel logLevel)
        {
            _fileName = fileName;
            _logLevel = logLevel;
        }
        /// <summary>
        /// Gets the specified named logger.
        /// </summary>
        /// <param name="name">Name of the logger.</param>
        /// <returns>
        /// The logger reference.
        /// </returns>
        public Logger GetLogger(string name)
        {
            return (logLevel, messageFunc, exception, formatParameters) =>
            {
                if (messageFunc == null)
                {
                    return true; // All log levels are enabled
                }
  
                WriteMessage(logLevel, name, messageFunc, formatParameters, exception);
                return true;
            };
        }

        /// <summary>
        /// Writes the message.
        /// </summary>
        /// <param name="logLevel">The log level.</param>
        /// <param name="name">The name.</param>
        /// <param name="messageFunc">The message function.</param>
        /// <param name="formatParameters">The format parameters.</param>
        /// <param name="exception">The exception.</param>
        private void WriteMessage(
            LogLevel logLevel,
            string name,
            Func<string> messageFunc,
            object[] formatParameters,
            Exception exception)
        {
            try
            {
                if (logLevel >= _logLevel)
                {
                    if (formatParameters == null || formatParameters.Length == 0)
                    {
                        var message = messageFunc();
                        if (exception != null)
                        {
                            message = message + "|" + exception;
                        }
                        FileWriteLine($"{DateTime.UtcNow} | {logLevel} | {name} | {message}");
                    }
                    else
                    {
                        var message = string.Format(CultureInfo.InvariantCulture, messageFunc(), formatParameters);
                        if (exception != null)
                        {
                            message = message + "|" + exception;
                        }
                        FileWriteLine($"{DateTime.UtcNow} | {logLevel} | {name} | {message}");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private void FileWriteLine(string message)
        {
            lock (Locker)
            {
                File.AppendAllText(_fileName, message + Environment.NewLine + Environment.NewLine);
            }
        }

        /// <summary>
        /// Opens a nested diagnostics context. Not supported in EntLib logging.
        /// </summary>
        /// <param name="message">The message to add to the diagnostics context.</param>
        /// <returns>
        /// A disposable that when disposed removes the message from the context.
        /// </returns>
        public IDisposable OpenNestedContext(string message)
        {
            return NullDisposable.Instance;
        }

        /// <summary>
        /// Opens a mapped diagnostics context. Not supported in EntLib logging.
        /// </summary>
        /// <param name="key">A key.</param>
        /// <param name="value">A value.</param>
        /// <returns>
        /// A disposable that when disposed removes the map from the context.
        /// </returns>
        public IDisposable OpenMappedContext(string key, string value)
        {
            return NullDisposable.Instance;
        }

        /// <summary>
        /// A 'null' object that won't complain if dispose is called
        /// </summary>
        private class NullDisposable : IDisposable
        {
            internal static readonly IDisposable Instance = new NullDisposable();

            /// <summary>
            /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
            /// </summary>
            public void Dispose()
            { }
        }
    }
}
