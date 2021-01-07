using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Function
{
    public class ELAPSFunctionHandler
    {
        Stopwatch timer;
        MongoClient mongo;
        public ELAPSFunction Function { get; set; }
        public bool EnableLogging { get; set; }

        public ELAPSFunctionHandler()
        {
            Function = new ELAPSFunction { Parameters = new Dictionary<string, string>(), Children = new List<ELAPSFunction>() };
            EnableLogging = true;
        }

        public ELAPSFunctionHandler(string mongoEndpoint) : this()
        {
            mongo = new MongoClient($"mongodb://{mongoEndpoint}");
            EnableLogging = true;
        }

        public void StartTimer()
        {
            timer = Stopwatch.StartNew();
        }

        public TimeSpan StopTimer()
        {
            timer.Stop();
            return timer.Elapsed;
        }

        public async Task LogStartAsync()
        {
            _ = logMessage($"Starting function with key [{Function.Key}]");
        }
        public async Task LogStopAsync(TimeSpan t)
        {
            _ = logMessage($"Finished starting function with key [{Function.Key}] in {t.Milliseconds/1000d} seconds.");
        }

        public bool ReadFunctionCallDoc(string key)
        {

            if (mongo == null)
            {
                _ = logError("Mongo object is null");
                return false;
            }

            _ = logMessage($"Retrieving function call for key {key}");
            try
            {
                var database = mongo.GetDatabase("elaps");
                var collection = database.GetCollection<BsonDocument>("functioncalls");
                var filter = Builders<BsonDocument>.Filter.Eq("_key", key);
                var task = collection.Find(filter).FirstOrDefault();
                var jsonWriterSettings = new JsonWriterSettings { OutputMode = JsonOutputMode.Strict };

                var result = JObject.Parse(task.ToJson<BsonDocument>(jsonWriterSettings));
                return parseFunctionDoc(result);
            }
            catch (Exception ex)
            {
                _ = logError($"Error retrieving function doc: {ex.Message}");
                return false;
            }
        }

        public async Task CallChildren()
        {
            await CallChildren(this.Function);
        }

        public async Task CallChildren(ELAPSFunction functionCopy)
        {
            if (functionCopy.Children.Any())
            {
                // Set up each child function
                foreach (var child in functionCopy.Children)
                {
                    //Write inherited parameters down to child
                    foreach (var p in functionCopy.Parameters)
                    {
                        if (child.Parameters.ContainsKey(p.Key))
                        {
                            child.Parameters[p.Key] = p.Value;
                        }
                        else
                        {
                            child.Parameters.Add(p.Key, p.Value);
                        }
                    }

                    //Write function call doc
                    await writeFunctionCallDocAsync(child);
                    _ = callFunction(child);
                }
            }
        }

        private async Task writeFunctionCallDocAsync(ELAPSFunction child)
        {
            if (mongo == null)
            {
                _ = logError("Mongo object is null");
                return;
            }

            var database = mongo.GetDatabase("elaps");
            child.Key = Guid.NewGuid().ToString();
            var document = BsonDocument.Parse(child.ToJson().ToString());
            var collection = database.GetCollection<BsonDocument>("functioncalls");
            await collection.InsertOneAsync(document);
            //LOG function call created
        }

        private bool parseFunctionDoc(JObject doc)
        {
            if (doc.Type == JTokenType.Null)
                return false;

            try
            {
                Function.Key = doc["_key"].Value<string>();
                Function.Name = doc["name"].Value<string>();
                Function.Workflow = doc["workflow"].Value<string>();
                Function.Parameters = doc["params"].ToObject<Dictionary<string, string>>();
                Function.Children = doc["functions"].Children().Select(x => x.ToObject<ELAPSFunction>()).ToList();
                return true;
            }
            catch (Exception ex)
            {
                _ = logError($"Error parsing function doc: {ex.Message}");
                return false;
            }
        }

        private async Task callFunction(ELAPSFunction function)
        {            
            using (var client = new HttpClient())
            {
                // Use http://gateway:8080/... for docker swarm
                var uri = new Uri($"http://gateway.openfaas:8080/function/{function.Name}");
                HttpResponseMessage response = await client.PostAsync(uri, new StringContent(function.Key, Encoding.UTF8, "text/plain"));
                var result = await response.Content.ReadAsStringAsync();
                await logMessage($"Result of call to {uri.ToString()}: {result}");
            }
        }

        public async Task logMessage(string message, string type="info")
        {
            Console.WriteLine($"[{type}] {message}");
            if (!EnableLogging)
                return;

            if (mongo == null)
                return;

            var database = mongo.GetDatabase("elaps");
            BsonDocument document = new BsonDocument();
            document.Add("type", type);
            document.Add("timestamp", DateTime.Now.ToString());
            document.Add("source", Function.Name);
            document.Add("message", message);
            var collection = database.GetCollection<BsonDocument>("functionlogs");
            await collection.InsertOneAsync(document);
        }

        public async Task logError(string message)
        {
            await logMessage(message, "error");
        }
    }
}
