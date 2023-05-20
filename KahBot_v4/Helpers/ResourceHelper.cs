using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

namespace KahBot_v4.Helpers
{
    public class ResourceHelper
    {
        private readonly ResourceManager _resourceManagerGeneralMessages;
        private readonly IConfigurationRoot _configuration;
        private readonly CultureInfo _culture;

        public ResourceHelper(Assembly assembly, IConfigurationRoot configuration)
        {
            _resourceManagerGeneralMessages = new ResourceManager("KahBot_v4.Resources.GeneralMessages", assembly);
            _configuration = configuration;
            string cultureValue = _configuration["Culture"] ?? "en-GB";
            _culture = new CultureInfo(cultureValue);
        }

        public string GetString(ResourceFiles resourceFiles, string key)
        {
            switch (resourceFiles)
            {
                case ResourceFiles.GeneralMessages:
                    return _resourceManagerGeneralMessages.GetString(key, _culture!) ?? string.Empty;
                default:
                    return string.Empty;
            }
            
        }
    }

}
