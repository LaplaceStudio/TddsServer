using System.Net.WebSockets;
using System.Text;
using TddsServer.Objects;

namespace TddsServer.General {
    public static class TddsService {

        public static ServiceManager ConsoleManager = new ServiceManager();
        public static ServiceManager NoticeManager = new ServiceManager();

        public static async void ConnectAsUploader(ServiceManager serviceManager,HttpContext httpContext,WebSocket webSocket) {
            
        }

        public static TddsSvcMsg Handle(TddsSvcMsg msg) {
            if (msg == null) {
                return TddsSvcMsg.UnresolvedMsg();
            }
            switch (msg.Type) {
                case MessageType.ServiceOnline:
                    return new TddsSvcMsg(MessageType.Success, "Service is online.");
                case MessageType.GetChannelIds:
                    var ids = ChannelManager.GetChannelIds();
                    return new TddsSvcMsg(msg.Type, $"Got {ids.Count} uploading channels.", ids);
                case MessageType.GetChannelImageFormat:
                    if (msg.Data != null && int.TryParse(msg.Data.ToString(), out int channelId)) {
                        return ChannelManager.GetChannelImageFormat(channelId);
                    } else {
                        return TddsSvcMsg.InvalidParamMsg("channelId");
                    }
                default:
                    return new TddsSvcMsg(MessageType.Error, "Unknow message type.");
            }
        }

        public static async Task SendMsgAsync(WebSocket webSocket, TddsSvcMsg msg) {
            if (webSocket == null) return;
            ArraySegment<byte> buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(msg.GetJson()));
            ArraySegment<byte> lenBytes = new ArraySegment<byte>(BitConverter.GetBytes(buffer.Count).Reverse().ToArray());
            // Send content length
            await webSocket.SendAsync(lenBytes, WebSocketMessageType.Binary, false, CancellationToken.None);
            // Send content
            await webSocket.SendAsync(buffer, WebSocketMessageType.Binary, true, CancellationToken.None);
        }
    }
}
