using System.Net.WebSockets;

namespace TddsServer.General {
    public class Utils {

        public static bool SaveFile(string path,string text) {
            try {
                File.WriteAllText(path, text);
                return true;
            }catch(Exception) {
                return false;
            }
        }

        public static async Task Echo(HttpContext httpContext, WebSocket webSocket) {
            var buffer = new byte[1024 * 4];
            WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            while (!result.CloseStatus.HasValue) {
                await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None);

                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }
            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }
        //public static async Task Handle(HttpContext httpContext, WebSocket webSocket) {
        //    byte[] bytes = new byte[1024];
        //    List<byte> buffer = new List<byte>();
        //    WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(bytes), CancellationToken.None);
        //    while (!result.CloseStatus.HasValue) {
        //        buffer.AddRange(bytes.Take(result.Count));
        //        if (result.EndOfMessage) {
        //            await ServiceManager.HandleMessage(webSocket, buffer.ToArray());
        //            buffer.Clear();
        //        }
        //        result = await webSocket.ReceiveAsync(new ArraySegment<byte>(bytes), CancellationToken.None);
        //    }
        //    await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        //}
    }

}
