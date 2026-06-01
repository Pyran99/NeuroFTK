#nullable enable

using Newtonsoft.Json;

namespace NeuroSdk.Websocket
{
    /* Missing compiler required member 'System.Type.op_Equality'
    original -> public record WsMessage
    */
    public class WsMessage(string command, object? data, string game)
    {
        // public WsMessage(string command, object? data, string game)
        // {
        //     Command = command;
        //     _game = game;
        //     Data = data;
        // }


        [JsonProperty("command", Order = 0)]
        public readonly string Command = command;

        [JsonProperty("game", Order = 10)]
        private readonly string _game = game;

        [JsonProperty("data", Order = 20)]
        public readonly object? Data = data;
    }
}
