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
        public static ILogProvider Create(string queueName, string initText)
        {
            return Create(queueName, LogLevel.Error, initText);
        }

        public static ILogProvider Create(string queueName, LogLevel logLevel, string initText)
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
    public class TextFileLogProvider : ILogProvider
    {
        //private const bool _logEverything = false;
        private readonly object _locker = new object();

        private readonly string _fileName;
        private readonly string _fileNameOther;
        private readonly LogLevel _logLevel;

        // ReSharper disable once UnusedParameter.Local
        /// <summary>
        /// Initializes a new instance of the <see cref="TextFileLogProvider"/> class.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="logLevel">The log level.</param>
        /// <param name="initText">The initialize text.</param>
        public TextFileLogProvider(string fileName, LogLevel logLevel, string initText)
        {
            _fileName = fileName;
            _fileNameOther = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + "Other.txt";
            _logLevel = logLevel;

            //if (_logEverything)
            //{
            //    FileWriteLineOther($"{DateTime.UtcNow} | {LogLevel.Trace} | Init | {initText}");
            //}
        }
        /// <inheritdoc />
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

                if (formatParameters == null || formatParameters.Length == 0)
                {
                    var message = messageFunc();
                    if (exception != null)
                    {
                        message = message + "|" + exception;
                    }
                    if (logLevel >= _logLevel)
                    {
                        FileWriteLine($"{DateTime.UtcNow} | {logLevel} | {name} | {message}");
                    }
                    else
                    {
                        WriteMessageConsole(logLevel, name, messageFunc, formatParameters, exception);
                    }
                    //else if(_logEverything)
                    //{
                    //    FileWriteLineOther($"{DateTime.UtcNow} | {logLevel} | {name} | {message}");
                    //}
                }
                else
                {
                    var message = string.Format(CultureInfo.InvariantCulture, messageFunc(), formatParameters);
                    if (exception != null)
                    {
                        message = message + "|" + exception;
                    }
                    if (logLevel >= _logLevel)
                    {
                        FileWriteLine($"{DateTime.UtcNow} | {logLevel} | {name} | {message}");
                    }
                    else
                    {
                        WriteMessageConsole(logLevel, name, messageFunc, formatParameters, exception);
                    }
                    //else if (_logEverything)
                    //{
                    //    FileWriteLineOther($"{DateTime.UtcNow} | {logLevel} | {name} | {message}");
                    //}
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
        /// <param name="name">The name.</param>
        /// <param name="messageFunc">The message function.</param>
        /// <param name="formatParameters">The format parameters.</param>
        /// <param name="exception">The exception.</param>
        private static void WriteMessageConsole(
            LogLevel logLevel,
            string name,
            Func<string> messageFunc,
            object[] formatParameters,
            Exception exception)
        {
            if (formatParameters == null || formatParameters.Length == 0)
            {
                var message = messageFunc();
                if (exception != null)
                {
                    message = message + "|" + exception;
                }
                Console.WriteLine("{0} | {1} | {2} | {3}", DateTime.UtcNow, logLevel, name, message);
            }
            else
            {
                var message = string.Format(CultureInfo.InvariantCulture, messageFunc(), formatParameters);
                if (exception != null)
                {
                    message = message + "|" + exception;
                }
                Console.WriteLine("{0} | {1} | {2} | {3}", DateTime.UtcNow, logLevel, name, message);
            }
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
        public IDisposable OpenNestedContext(string message)
        {
            return NullDisposable.Instance;
        }
        /// <inheritdoc />
        public IDisposable OpenMappedContext(string key, string value)
        {
            return NullDisposable.Instance;
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
