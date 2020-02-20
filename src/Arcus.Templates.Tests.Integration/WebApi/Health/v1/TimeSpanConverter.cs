using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Arcus.Templates.Tests.Integration.WebApi.Health.v1 
{
    /// <summary>
    /// JSON converter to parse <see cref="TimeSpan"/> values.
    /// Fixing issue: https://github.com/JamesNK/Newtonsoft.Json/issues/2189.
    /// </summary>
    public class TimeSpanConverter : JsonConverter<TimeSpan>
    {
        /// <summary>Writes the JSON representation of the object.</summary>
        /// <param name="writer">The <see cref="T:Newtonsoft.Json.JsonWriter" /> to write to.</param>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, TimeSpan value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        /// <summary>Reads the JSON representation of the object.</summary>
        /// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader" /> to read from.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="existingValue">The existing value of object being read. If there is no existing value then <c>null</c> will be used.</param>
        /// <param name="hasExistingValue">The existing value has a value.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <returns>The object value.</returns>
        public override TimeSpan ReadJson(
            JsonReader reader,
            Type objectType,
            TimeSpan existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            JObject jObject = JObject.Load(reader);
            var days = jObject.GetValue("days").Value<int>();
            var hours = jObject.GetValue("hours").Value<int>();
            var min = jObject.GetValue("minutes").Value<int>();
            var sec = jObject.GetValue("seconds").Value<int>();
            var minSec = jObject.GetValue("milliseconds").Value<int>();

            return new TimeSpan(days, hours, min, sec, minSec);
        }
    }
}