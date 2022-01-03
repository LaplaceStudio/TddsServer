using Newtonsoft.Json;
using System.Net.WebSockets;
using System.Text;
using TddsServer.Objects;

namespace TddsServer.General {
    public class ServiceManager {

        private static string? TddsConnectionId = null;
        private static string? ConsoleConnectionId = null;
        public static WebSocket? TddsSocket = null;
        public static WebSocket? ConsoleSocket = null;

        private static readonly ILogger logger;

        public static async void ConnectAsTdds(HttpContext httpContext, WebSocket webSocket) {
            if (TddsSocket != null) {
                TddsSocket.Abort();

                //logger.LogInformation($"Abort TDDS websocket, Id:{TddsConnectionId}");
            }
            TddsSocket = webSocket;
            TddsConnectionId = httpContext.Connection.RemoteIpAddress == null
                ? httpContext.Connection.Id
                : $"{httpContext.Connection.RemoteIpAddress}:{httpContext.Connection.RemotePort}";
            TddsSvcMsg msg = new TddsSvcMsg(MessageType.TddsConnected, $"Login success. Id:{TddsConnectionId}.");

            await SendAsync(TddsSocket, msg.GetJson());
            if (ConsoleSocket != null)
                await SendAsync(ConsoleSocket, msg.GetJson());

            //logger.LogInformation($"Id:{TddsConnectionId} connected service as TDDS.");
        }

        public static async void DisconnectAsTdds() {
            TddsSvcMsg msg = new TddsSvcMsg(MessageType.TddsDisconnected, "TDDS logged out.");
            if (ConsoleSocket != null) {
                await SendAsync(ConsoleSocket, msg.GetJson());
            }
            TddsSocket?.Abort();
            //logger.LogInformation($"Id:{TddsConnectionId} disconnected service.");

            TddsSocket = null;
            TddsConnectionId = null;
        }

        public static async void ConnectAsConsole(HttpContext httpContext, WebSocket webSocket) {
            if (ConsoleSocket != null) {
                ConsoleSocket.Abort();

                //logger.LogInformation($"Abort Console websocket, Id:{TddsConnectionId}");
            }
            ConsoleSocket = webSocket;
            ConsoleConnectionId = httpContext.Connection.RemoteIpAddress == null
                ? httpContext.Connection.Id
                : $"{httpContext.Connection.RemoteIpAddress}:{httpContext.Connection.RemotePort}";
            TddsSvcMsg msg = new TddsSvcMsg(MessageType.ConsoleConnected, $"Login success. Id:{ConsoleConnectionId}.");
            if (TddsSocket != null)
                await SendAsync(TddsSocket, msg.GetJson());
            await SendAsync(ConsoleSocket, msg.GetJson());
        }

        public static async void DisconnectAsConsole() {
            TddsSvcMsg msg = new TddsSvcMsg(MessageType.ConsoleDisconnected, "Console logged out.");
            if (TddsSocket != null)
                await SendAsync(TddsSocket, msg.GetJson());
            ConsoleSocket?.Abort();
            //logger.LogInformation($"Id:{TddsConnectionId} disconnected service.");

            ConsoleSocket = null;
            ConsoleConnectionId = null;
        }

        public static async Task SendAsync(WebSocket webSocket,string message) {
            if (webSocket == null) return;
            ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[4096]);
            buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));
            await webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public static async Task RetransmitToTdds(WebSocket consoleSocket) {
            if (TddsSocket == null) {
                TddsSvcMsg msg = new TddsSvcMsg(MessageType.TddsDisconnected, "TDDS is not connected.");
                await SendAsync(consoleSocket, msg.GetJson());
            }
            var buffer = new byte[1024 * 4];
            WebSocketReceiveResult result = await consoleSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            while (!result.CloseStatus.HasValue) {
                if (TddsSocket == null) {
                    await SendAsync(consoleSocket, new TddsSvcMsg(MessageType.TddsDisconnected, "TDDS is not connected.").GetJson());
                } else { 
                    await TddsSocket.SendAsync(
                        new ArraySegment<byte>(buffer, 0, result.Count),
                        result.MessageType,
                        result.EndOfMessage,
                       CancellationToken.None);
                }
                result = await consoleSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }
            await consoleSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
            DisconnectAsConsole();
        }

        public static async Task RetransmitToConsole(WebSocket tddsSocket) {
            if(ConsoleSocket == null) {
                TddsSvcMsg msg = new TddsSvcMsg(MessageType.ConsoleDisconnected, "Console is not connected.");
                await SendAsync(tddsSocket, msg.GetJson());
            }
            var buffer = new byte[1024 * 4];
            WebSocketReceiveResult result = await tddsSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            while (!result.CloseStatus.HasValue) {
                if (ConsoleSocket == null) {
                    await SendAsync(tddsSocket, new TddsSvcMsg(MessageType.ConsoleDisconnected, "Console is not connected.").GetJson());
                } else {
                    await ConsoleSocket.SendAsync(
                        new ArraySegment<byte>(buffer, 0, result.Count),
                        result.MessageType,
                        result.EndOfMessage,
                        CancellationToken.None);
                }
                result = await tddsSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }
            await tddsSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
            DisconnectAsTdds();
        }

    }
}
