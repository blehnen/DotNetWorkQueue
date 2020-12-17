using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using DotNetWorkQueue.Logging;
using FluentAssertions;
using Xunit;

namespace DotNetWorkQueue.IntegrationTests.Shared
{
    public static class LoggerShared
    {
        public static ILogger Create(string queueName, string initText)
        {
            return Create(queueName, LoggingEventType.Error, initText);
        }

        public static ILogger Create(string queueName, LoggingEventType logLevel, string initText)
        {
            if(!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\"))
                Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\");

            return
                new TextFileLogProvider(
                    AppDomain.CurrentDomain.BaseDirectory + $"\\Logs\\{queueName}.txt", logLevel, initText);
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "part of test")]
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
                            $"errors should have occurred, however no errors where found for queue {queueName}");
                }
            }
            else
            {
                Assert.False(true, $"No error file was found; errors should have occurred for queue {queueName}");
            }
        }
        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "part of test")]
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
                        .BeEmpty("No errors should have occurred, however the following errors where found {0}", errors);
                }
            }
        }
    }
    public class TextFileLogProvider : ILogger
    {
        private readonly object _locker = new object();

        private readonly string _fileName;
        private readonly string _fileNameOther;
        private readonly LoggingEventType _level;

        // ReSharper disable once UnusedParameter.Local
        /// <summary>
        /// Initializes a new instance of the <see cref="TextFileLogProvider"/> class.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="logLevel">The log level.</param>
        /// <param name="initText">The initialize text.</param>
        public TextFileLogProvider(string fileName, LoggingEventType logLevel, string initText)
        {
            _fileName = fileName;
            _fileNameOther = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + "Other.txt";
            _level = logLevel;
        }

        /// <inheritdoc />
        public void Log(Func<LogEntry> entry)
        {
            Log(entry.Invoke());
        }

        /// <inheritdoc />
        public void Log(LogEntry entry)
        {
            if (entry.Severity == LoggingEventType.Trace && _level <= LoggingEventType.Trace)
                WriteMessage(entry.Severity, entry.Message, entry.Exception);
            else if (entry.Severity == LoggingEventType.Debug && _level <= LoggingEventType.Debug)
                WriteMessage(entry.Severity, entry.Message, entry.Exception);
            else if (entry.Severity == LoggingEventType.Information && _level <= LoggingEventType.Information)
                WriteMessage(entry.Severity, entry.Message, entry.Exception);
            else if (entry.Severity == LoggingEventType.Warning && _level <= LoggingEventType.Warning)
                WriteMessage(entry.Severity, entry.Message, entry.Exception);
            else if (entry.Severity == LoggingEventType.Error && _level <= LoggingEventType.Error)
                WriteMessage(entry.Severity, entry.Message, entry.Exception);
            else
                WriteMessage(entry.Severity, entry.Message, entry.Exception);
        }

        /// <summary>
        /// Writes the message.
        /// </summary>
        /// <param name="logLevel">The log level.</param>
        /// <param name="message">The message function.</param>
        /// <param name="exception">The exception.</param>
        private void WriteMessage(
            LoggingEventType logLevel,
            string message,
            Exception exception)
        {
            try
            {
                if (exception != null)
                {
                    message = message + "|" + exception;
                }

                if (logLevel >= _level)
                {
                    FileWriteLine($"{DateTime.UtcNow} | {logLevel} | {message}");
                }
                else
                {
                    WriteMessageConsole(logLevel, message, exception);
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
            lock (_locker)
            {
                File.AppendAllText(_fileName, message + Environment.NewLine + Environment.NewLine);
            }
        }

        /// <summary>
        /// Writes the message.
        /// </summary>
        /// <param name="logLevel">The log level.</param>
        /// <param name="message">The message function.</param>
        /// <param name="exception">The exception.</param>
        private static void WriteMessageConsole(
            LoggingEventType logLevel,
            string message,
            Exception exception)
        {
            if (exception != null)
            {
                message = message + "|" + exception;
            }
            Console.WriteLine("{0} | {1} | {2}", DateTime.UtcNow, logLevel, message);
        }

        // ReSharper disable once UnusedMember.Local
        private void FileWriteLineOther(string message)
        {
            lock (_locker)
            {
                File.AppendAllText(_fileNameOther, message + Environment.NewLine + Environment.NewLine);
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// A 'null' object that won't complain if dispose is called
        /// </summary>
        private class NullDisposable : IDisposable
        {
            internal static readonly IDisposable Instance = new NullDisposable();

            /// <inheritdoc />
            public void Dispose()
            { }
        }
    }
}
