using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Ajax.Utilities;
using Newtonsoft.Json;
using Vostok.Airlock.Logging;
using Vostok.Logging;

namespace Vostok.FrontReport.Dto
{
    public abstract class Report
    {
        [JsonProperty("frontreport-host")]
        public string Host { get; set; }

        public abstract string GetProject();

        public virtual LogEventData ToLogEventData()
        {
            var logEventData = new LogEventData
            {
                Level = LogLevel.Error,
                Timestamp = DateTimeOffset.UtcNow,
                Properties = new Dictionary<string, string>()
            };
            //logEventData.Properties["_type"] = GetType().Name;
            LoadStringPropertiesToDictionary(this, logEventData.Properties);
            return logEventData;
        }

        protected string GetServiceFromHostName(string host)
        {
            if (string.IsNullOrWhiteSpace(host))
                return null;
            return host.Replace("kontur.ru", "").Replace(".ru","").Replace("www.","");
        }

        protected static void LoadStringPropertiesToDictionary(object obj, IDictionary<string, string> dictionary)
        {
            obj.GetType().GetProperties().Where(prop => Attribute.IsDefined(prop, typeof(JsonPropertyAttribute)) 
                                                        && (prop.PropertyType==typeof(string) || prop.PropertyType == typeof(bool) || prop.PropertyType == typeof(int)) &&
                                                        !string.IsNullOrEmpty(prop.GetValue(obj)?.ToString()))
                .ForEach(prop =>
                {
                    var value = prop.GetValue(obj);
                    if (value == null)
                        return;
                    string propStrValue = null;
                    if (prop.PropertyType == typeof(string) || prop.PropertyType == typeof(bool) || prop.PropertyType == typeof(int) || prop.PropertyType == typeof(NameAndVersion))
                        propStrValue = value.ToString();
                    else if (prop.PropertyType == typeof(string[]))
                        propStrValue = string.Join(", ", (string[])value);
                    if (string.IsNullOrEmpty(propStrValue))
                        return;
                    var propertyName = (prop.GetCustomAttributes(typeof(JsonPropertyAttribute), true).First() as JsonPropertyAttribute)?.PropertyName ?? prop.Name;
                    dictionary.Add(propertyName, propStrValue);
                });
        }

    }
}