using System;
using Gallio.Common.Diagnostics;
using Gallio.Runtime.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace Microsoft.VisualStudio.TestPlatform.Gallio
{
    public class TestFrameworkLogger : BaseLogger
    {
        private readonly IMessageLogger _messageLogger;

        public TestFrameworkLogger(IMessageLogger messageLogger)
        {
            _messageLogger = messageLogger;
        }

        protected override void LogImpl(LogSeverity severity, string message, ExceptionData exceptionData)
        {
            var warning = exceptionData == null
                   ? message
                   : string.Concat(message, "\n", exceptionData.ToString());

            switch (severity)
            {
                case LogSeverity.Info:
                    _messageLogger.SendMessage(TestMessageLevel.Informational, warning);
                    break;
                case LogSeverity.Warning:
                    _messageLogger.SendMessage(TestMessageLevel.Warning, warning);
                    break;
                case LogSeverity.Debug:
                    _messageLogger.SendMessage(TestMessageLevel.Informational, warning);
                    break;
                case LogSeverity.Important:
                    _messageLogger.SendMessage(TestMessageLevel.Informational, warning);
                    break;
                case LogSeverity.Error:
                    _messageLogger.SendMessage(TestMessageLevel.Error, warning);
                    break;
            }
        }
    }
}
