using System;
using System.Reflection;
using System.Xml;
using log4net;
using Microsoft.Extensions.Logging;

namespace Notification.Wpf.Sample.Logger
{
    public class Log4NetLogger : ILogger
    {
        private readonly ILog _log;
        public Log4NetLogger(string categoryName, XmlElement xml)
        {
            var repository = LogManager.CreateRepository(
                Assembly.GetEntryAssembly(),
                typeof(log4net.Repository.Hierarchy.Hierarchy));
            _log = LogManager.GetLogger(repository.Name, categoryName);


            log4net.Config.XmlConfigurator.Configure(repository,xml);
        }
        #region Implementation of ILogger

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if(!IsEnabled(logLevel))return;
            if(formatter is null) throw new ArgumentNullException(nameof(formatter));
            var msg = formatter(state, exception);
            if(string.IsNullOrWhiteSpace(msg) && exception is null)return;

            switch (logLevel)
            {
                default: throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
                case LogLevel.Trace:
                case LogLevel.Debug: 
                    _log.Debug(msg);
                    break;
                case LogLevel.Information:
                    _log.Info(msg);
                    break;

                case LogLevel.Warning:
                    _log.Warn(msg);
                    break;

                case LogLevel.Error:
                    _log.Error(msg ?? exception.ToString());
                    break;

                case LogLevel.Critical:
                    _log.Debug(msg ?? exception.ToString());
                    break;

                case LogLevel.None: break;
            }
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            switch (logLevel)
            {
                default: throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
                case LogLevel.Trace: 
                case LogLevel.Debug:
                    return _log.IsDebugEnabled;
                case LogLevel.Information: return _log.IsInfoEnabled;

                case LogLevel.Warning: return _log.IsWarnEnabled;

                case LogLevel.Error: return _log.IsErrorEnabled;

                case LogLevel.Critical: return _log.IsFatalEnabled;

                case LogLevel.None: return false;

            }
        }

        public IDisposable BeginScope<TState>(TState state) => null;

        #endregion
    }
}