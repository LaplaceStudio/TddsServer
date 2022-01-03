using Newtonsoft.Json;

namespace TddsServer.Objects {

    public enum MessageType {
        Error = -1,
        Success,
        TddsConnected,
        TddsDisconnected,
        ConsoleConnected,
        ConsoleDisconnected,

        #region TDDS and Console Message Type



        #endregion

        #region TDDS Service API Message Type
        ServiceOnline=100,
        TddsOnline,
        ConsoleOnline,
        GetChannelIds,
        SetChannelImageFormat,
        GetChannelImageFormat,


        #endregion
    }

    public class TddsSvcMsg {

        [JsonProperty("Type")]
        public MessageType Type { get; set; }

        [JsonProperty("Message")]
        public string Message { get; set; }

        [JsonProperty("Data")]
        public object Data { get; set; }

        [JsonConstructor]
        public TddsSvcMsg() { }

        public TddsSvcMsg(MessageType type, string message) {
            Type = type;
            Message = message;
        }

        public TddsSvcMsg(MessageType type, string message, object data) : this(type, message) {
            Data = data;
        }

        public string GetJson() {
            return JsonConvert.SerializeObject(this);
        }

        public static TddsSvcMsg InvalidParamMsg(string paramName) {
            return new TddsSvcMsg(MessageType.Error, $"The data in the message is not a useful {paramName} object.");
        }
    }


}
