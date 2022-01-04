using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using TddsServer.Objects;

namespace TddsServer.General {
    public class DemoManager {

        private static WebSocket? TddsSocket = null;

        private static ConcurrentDictionary<string, WebSocket> ClientSocket = new ConcurrentDictionary<string, WebSocket>();

        #region TDDS Login
        public static async Task<bool> TddsLogin(WebSocket webSocket) {
            TddsSvcMsg msg;
            if (TddsSocket != null) {
                msg = new TddsSvcMsg(MessageType.UploaderOnline, "Login failed, TDDS is already connected.");
                await SendToSocket(webSocket, msg.GetJson());
                return false;
            } else {
                TddsSocket = webSocket;
                msg = new TddsSvcMsg(MessageType.UploaderOnline, "TDDS login succeeded.");
                await SendToSocket(TddsSocket, msg.GetJson());
                await TddsToClient(msg.GetJson());
                return true;
            }
        }
        #endregion

        #region TDDS Logout
        public static async void TddsLogout() {
            TddsSocket = null;
            TddsSvcMsg msg = new TddsSvcMsg(MessageType.DownloaderOffline, "TDDS disconnected.");
            await TddsToClient(msg.GetJson());
        }
        #endregion

        /// <summary>
        /// Add a new client.
        /// </summary>
        /// <param name="id">Connection Id of HttpContext.</param>
        /// <param name="webSocket">WebSocket of HttpContext.</param>
        /// <returns>True for addding successfully and false for already online.</returns>
        public static async Task<bool> AddClient(string id, WebSocket webSocket) {
            bool success = ClientSocket.TryAdd(id, webSocket);
            if (success) {
                // A new logged in message.
                TddsSvcMsg msg = new TddsSvcMsg(MessageType.DownloaderOnline, $"Id:{id} connected.");

                // Send logged in message to TDDS.
                if (TddsSocket != null) {
                    await SendToSocket(TddsSocket, msg.GetJson());
                }

                // Send logged in message to all clients.
                await TddsToClient(msg.GetJson());
            }
            return success;
        }

        /// <summary>
        /// Remove Client
        /// </summary>
        /// <param name="clientId"></param>
        public static async void RemoveClient(string clientId) {
            ClientSocket.TryRemove(clientId, out WebSocket? ws);
            TddsSvcMsg msg = new TddsSvcMsg(MessageType.DownloaderOffline, $"Id:{clientId} disconnected.");

            // Send logged out message to TDDS
            if (TddsSocket != null) {
                await SendToSocket(TddsSocket, msg.GetJson());
            }

            // Send logged out message to all clients.
            await TddsToClient(msg.GetJson());
        }

        /// <summary>
        /// Send a text message from TDDS to all client.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static async Task TddsToClient(string message) {
            ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[4096]);
            buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));
            foreach (var socket in ClientSocket.Values) {
                await socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        public static async Task RetransmitToTdds(string id, WebSocket clientSocket) {
            var buffer = new byte[1024 * 4];
            WebSocketReceiveResult result = await clientSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            while (!result.CloseStatus.HasValue) {
                if (TddsSocket != null) {
                    await TddsSocket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None);
                }
                result = await clientSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }
            await clientSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);

            RemoveClient(id);
        }

        /// <summary>
        /// Send result of tdds socket to client socket.
        /// </summary>
        /// <param name="webSocket">TDDS Socket.</param>
        /// <returns></returns>
        public static async Task RetransmitToClient(WebSocket webSocket) {
            var buffer = new byte[1024 * 4];
            WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            while (!result.CloseStatus.HasValue) {
                foreach (var socket in ClientSocket.Values) {
                    await socket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None);
                }
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }
            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
            // Send logout message after connection closed.
            TddsLogout();
        }

        public static async Task SendToSocket(WebSocket webSocket, string message) {
            ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[4096]);
            buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));
            await webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }

    
}
