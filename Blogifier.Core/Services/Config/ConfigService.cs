using Microsoft.Extensions.Configuration;

namespace Blogifier.Core.Services
{
    public interface IConfigService
    {
        string GetSetting(string key);
    }

    public class ConfigService : IConfigService
    {
        private readonly IConfiguration config;

        public ConfigService(IConfiguration config)
        {
            this.config = config;
        }

        public string GetSetting(string key)
        {
            return this.config.GetSection("Blogifier").GetValue<string>(key);
        }
    }
}
