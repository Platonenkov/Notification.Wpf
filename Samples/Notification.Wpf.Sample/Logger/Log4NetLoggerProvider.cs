using System.Collections.Concurrent;
using System.Xml;
using Microsoft.Extensions.Logging;

namespace Notification.Wpf.Sample.Logger
{
    public class Log4NetLoggerProvider : ILoggerProvider
    {
        private readonly string _ConfigurationFile;
        private readonly ConcurrentDictionary<string, Log4NetLogger> _Logger = new ConcurrentDictionary<string, Log4NetLogger>();

        public Log4NetLoggerProvider(string ConfigurationFile) { _ConfigurationFile = ConfigurationFile; }
        #region Implementation of IDisposable

        public void Dispose() => _Logger.Clear();

        public ILogger CreateLogger(string categoryName)
        {
            return _Logger.GetOrAdd(
                categoryName, category =>
                {
                    var xml = new XmlDocument();
                    var file_name = _ConfigurationFile;
                    xml.Load(file_name);
                    return new Log4NetLogger(category, xml["log4net"]);
                });
        }

        #endregion
    }
}