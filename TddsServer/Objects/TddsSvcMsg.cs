using Newtonsoft.Json;

namespace TddsServer.Objects {

    public class TddsSvcMsg {

        [JsonProperty("Type")]
        public MessageType Type { get; set; }

        [JsonProperty("Message")]
        public string Message { get; set; }

        [JsonProperty("Data")]
        public object Data { get; set; }
        public string Time { get; set; }

        [JsonConstructor]
        public TddsSvcMsg() {
            Time = DateTime.Now.ToString();
        }

        public TddsSvcMsg(MessageType type, string message) {
            Type = type;
            Message = message;
            Time = DateTime.Now.ToString();
        }

        public TddsSvcMsg(MessageType type, string message, object data) : this(type, message) {
            Data = data;
        }

        public string GetJson() {
            return JsonConvert.SerializeObject(this);
        }

        public static TddsSvcMsg UnresolvedMsg() {
            return new TddsSvcMsg(MessageType.Error, "Unresolved message.");
        }

        public static TddsSvcMsg InvalidParamMsg(string paramName) {
            return new TddsSvcMsg(MessageType.Error, $"The data in the message is not a useful {paramName} object.");
        }
    }


}
