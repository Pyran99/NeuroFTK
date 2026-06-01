#nullable enable

using NeuroSdk.Json;
using Newtonsoft.Json;

namespace NeuroSdk.Actions
{
    public readonly struct WsAction(string name, string description, JsonSchema? schema)
    {
        // public WsAction(string name, string description, JsonSchema? schema)
        // {
        //     Name = name;
        //     _description = description;
        //     _schema = schema;
        // }


        [JsonProperty("name", Order = 0)]
        public readonly string Name = name;

        [JsonProperty("description", Order = 10)]
        private readonly string _description = description;

        [JsonProperty("schema", Order = 20)]
        private readonly JsonSchema? _schema = schema;
    }
}
