namespace Blogifier.Core.Common
{
    using Microsoft.Extensions.Logging;

    public class AppLogger
    {
        private readonly ILogger<object> logger;

        public AppLogger(ILogger<object> logger)
        {
            this.logger = logger;
        }

        public void LogInformation(string msg)
        {
            if (this.logger != null && ApplicationSettings.EnableLogging)
            {
                this.logger.LogInformation("[BLOGIFIER] " + msg);
            }
        }

        public void LogWarning(string msg)
        {
            if (this.logger != null && ApplicationSettings.EnableLogging)
            {
                this.logger.LogWarning("[BLOGIFIER] " + msg);
            }
        }

        public void LogError(string msg)
        {
            if (this.logger != null && ApplicationSettings.EnableLogging)
            {
                this.logger.LogError("[BLOGIFIER] " + msg);
            }
        }
    }
}