using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Newtonsoft.Json;

namespace Function
{
    public class ELAPSFunction
    {
        [BsonElement("_key")]
        [JsonProperty("_key")]
        public string Key { get; set; }
        
        [BsonElement("name")]
        [JsonProperty("name")]
        public string Name { get; set; }
        
        [BsonElement("workflow")]
        [JsonProperty("workflow")]
        public string Workflow { get; set; }
        
        [BsonElement("params")]
        [JsonProperty("params")]
        public Dictionary<string, string> Parameters { get; set; }
        
        [BsonElement("functions")]
        [JsonProperty("functions")]
        public List<ELAPSFunction> Children { get; set; }

        public JObject ToJObject()
        {
            return JObject.FromObject(this);
        }
    }
}
