using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KindomDataAPIServer.Common
{
    public static class JsonHelper
    {
        public static T ConvertFrom<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }

        public static object ConvertFrom(string json, Type type)
        {
            return JsonConvert.DeserializeObject(json, type);
        }

        public static string ToJson(object obj)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.NullValueHandling = NullValueHandling.Ignore;
            string json = JsonConvert.SerializeObject(obj, Formatting.Indented, settings);
            return json;
        }



        public static string ToJsonSmall(object obj, bool humanReadable = true)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            settings.Formatting = humanReadable ? Formatting.Indented : Formatting.None;
            settings.NullValueHandling = NullValueHandling.Ignore;
            string json = JsonConvert.SerializeObject(obj, settings);
            return json;
        }

    }
}
