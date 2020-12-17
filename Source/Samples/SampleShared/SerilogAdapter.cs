using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetWorkQueue.Logging;
using Serilog.Events;

namespace SampleShared
{
    public class SerilogAdapter : ILogger
    {
        private readonly Serilog.ILogger _logger;

        public SerilogAdapter(Serilog.ILogger logger)
        {
            _logger = logger;
        }

        public void Log(LogEntry entry)
        {
            //see if we can log anything, else exit early
            if (!_logger.IsEnabled(LogEventLevel.Fatal))
                return;

            if (entry.Exception == null)
            {
                if (entry.Severity == LoggingEventType.Trace && _logger.IsEnabled(LogEventLevel.Verbose))
                    _logger.Verbose(entry.Message);
                else if (entry.Severity == LoggingEventType.Debug && _logger.IsEnabled(LogEventLevel.Debug))
                    _logger.Debug(entry.Message);
                else if (entry.Severity == LoggingEventType.Information && _logger.IsEnabled(LogEventLevel.Information))
                    _logger.Information(entry.Message);
                else if (entry.Severity == LoggingEventType.Warning && _logger.IsEnabled(LogEventLevel.Warning))
                    _logger.Warning(entry.Message);
                else if (entry.Severity == LoggingEventType.Error && _logger.IsEnabled(LogEventLevel.Error))
                    _logger.Error(entry.Message);
                else if (entry.Severity == LoggingEventType.Fatal && _logger.IsEnabled(LogEventLevel.Fatal))
                    _logger.Fatal(entry.Message);
            }
            else
            {
                if (entry.Severity == LoggingEventType.Trace && _logger.IsEnabled(LogEventLevel.Verbose))
                    _logger.Verbose(entry.Exception, entry.Message);
                else if (entry.Severity == LoggingEventType.Debug && _logger.IsEnabled(LogEventLevel.Debug))
                    _logger.Debug(entry.Exception, entry.Message);
                else if (entry.Severity == LoggingEventType.Information && _logger.IsEnabled(LogEventLevel.Information))
                    _logger.Information(entry.Exception, entry.Message);
                else if (entry.Severity == LoggingEventType.Warning && _logger.IsEnabled(LogEventLevel.Warning))
                    _logger.Warning(entry.Message, entry.Exception);
                else if (entry.Severity == LoggingEventType.Error && _logger.IsEnabled(LogEventLevel.Error))
                    _logger.Error(entry.Message, entry.Exception);
                else if (entry.Severity == LoggingEventType.Fatal && _logger.IsEnabled(LogEventLevel.Fatal))
                    _logger.Fatal(entry.Message, entry.Exception);
            }
        }

        public void Log(Func<LogEntry> entry)
        {
            //see if we can log anything, else exit early
            if (!_logger.IsEnabled(LogEventLevel.Fatal))
                return;

            Log(entry.Invoke());
        }
    }
}
