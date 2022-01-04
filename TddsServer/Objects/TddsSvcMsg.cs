using Newtonsoft.Json;

namespace TddsServer.Objects {

    public enum MessageType {
        Error = -1,
        Success=0,
        UploaderOnline=1,
        UploaderOffline=2,
        DownloaderOnline=3,
        DownloaderOffline=4,

        #region TDDS and Console Message Type
        StartTDDS=10,
        StopTDDS=11,
        OpenChannel=12,
        CloseChannel=13,

        #endregion

        #region TDDS Service API Message Type
        ServiceOnline=100,
        TddsOnline=101,
        ConsoleOnline=102,
        GetChannelIds=103,
        GetChannelImageFormat=104,
        GetAllChannelsImageFormat=105,


        #endregion
    }

    public class TddsSvcMsg {

        [JsonProperty("Type")]
        public MessageType Type { get; set; }

        [JsonProperty("Message")]
        public string Message { get; set; }

        [JsonProperty("Data")]
        public object Data { get; set; }
        public DateTime DateTime { get; set; }

        [JsonConstructor]
        public TddsSvcMsg() {
            DateTime = DateTime.Now;
        }

        public TddsSvcMsg(MessageType type, string message) {
            Type = type;
            Message = message;
            DateTime = DateTime.Now;
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
