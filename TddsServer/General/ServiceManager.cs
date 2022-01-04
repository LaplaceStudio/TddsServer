using Newtonsoft.Json;
using System.Net.WebSockets;
using System.Text;
using TddsServer.Objects;

namespace TddsServer.General {
    public class ServiceManager {

        private static string? UploaderId = null;
        private static string? DownloaderId = null;
        public static WebSocket? UploaderSocket = null;
        public static WebSocket? DownloaderSocket = null;

        private static readonly ILogger logger;

        public async void ConnectAsUploader(HttpContext httpContext, WebSocket webSocket) {
            if (UploaderSocket != null) {
                Console.WriteLine("Uploader already exists. Aborting old uploader...");
                UploaderSocket.Abort();
                Console.WriteLine("Aborted old uploader.");
                //logger.LogInformation($"Abort TDDS websocket, Id:{TddsConnectionId}");
            }
            UploaderSocket = webSocket;
            UploaderId = httpContext.Connection.RemoteIpAddress == null
                ? httpContext.Connection.Id
                : $"{httpContext.Connection.RemoteIpAddress}:{httpContext.Connection.RemotePort}";
            TddsSvcMsg msg = new TddsSvcMsg(MessageType.UploaderOnline, $"Connect as uploader successfully. Id:{UploaderId}.");
            if (DownloaderSocket != null)
                await TddsService.SendMsgAsync(DownloaderSocket, msg);
            await TddsService.SendMsgAsync(UploaderSocket, msg);

            Console.WriteLine(msg.Message);
            
            //logger.LogInformation($"Id:{TddsConnectionId} connected service as TDDS.");
        }

        public async void DisconnectAsUploader() {
            TddsSvcMsg msg = new TddsSvcMsg(MessageType.DownloaderOffline, "Uploader logged out.");
            if (DownloaderSocket != null) {
                await TddsService.SendMsgAsync(DownloaderSocket, msg);
            }
            //logger.LogInformation($"Id:{TddsConnectionId} disconnected service.");

            UploaderSocket = null;
            UploaderId = null;
            Console.WriteLine(msg.Message);
        }

        public async void ConnectAsDownloader(HttpContext httpContext, WebSocket webSocket) {
            if (DownloaderSocket != null) {
                Console.WriteLine("Downloader already exists. Aborting old downloader.");
                DownloaderSocket.Abort();
                Console.WriteLine("Aborted old downloader.");
                //logger.LogInformation($"Abort Console websocket, Id:{TddsConnectionId}");
            }
            DownloaderSocket = webSocket;
            DownloaderId = httpContext.Connection.RemoteIpAddress == null
                ? httpContext.Connection.Id
                : $"{httpContext.Connection.RemoteIpAddress}:{httpContext.Connection.RemotePort}";
            TddsSvcMsg msg = new TddsSvcMsg(MessageType.DownloaderOnline, $"Connect as downloader successfully. Id:{DownloaderId}.");
            if (UploaderSocket != null)
                await TddsService.SendMsgAsync(UploaderSocket, msg);
            await TddsService.SendMsgAsync(DownloaderSocket, msg);

            Console.WriteLine(msg.Message);
        }

        public async void DisconnectAsDownloader() {
            TddsSvcMsg msg = new TddsSvcMsg(MessageType.DownloaderOffline, "Downloader logged out.");
            if (UploaderSocket != null)
                await SendAsync(UploaderSocket, msg.GetJson());
            DownloaderSocket?.Abort();
            //logger.LogInformation($"Id:{TddsConnectionId} disconnected service.");

            DownloaderSocket = null;
            DownloaderId = null;
            Console.WriteLine(msg.Message);
        }

        public async Task SendAsync(WebSocket webSocket,string message) {
            if (webSocket == null) return;
            ArraySegment<byte> buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));
            ArraySegment<byte> lenBytes = new ArraySegment<byte>(BitConverter.GetBytes(buffer.Count).Reverse().ToArray());
            // Send content length
            await webSocket.SendAsync(lenBytes, WebSocketMessageType.Binary, false, CancellationToken.None);
            // Send content
            await webSocket.SendAsync(buffer, WebSocketMessageType.Binary, true, CancellationToken.None);
        }

        public async Task RetransmitToUploader(WebSocket downloaderSocket) {
            var buffer = new byte[4096];
            WebSocketReceiveResult result = await downloaderSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            while (!result.CloseStatus.HasValue) {
                if (UploaderSocket == null) {
                    // Receive and not send if Uploader is null.
                    if (result.EndOfMessage) {
                        await TddsService.SendMsgAsync(downloaderSocket, new TddsSvcMsg(MessageType.DownloaderOffline, "Uploader is not connected."));
                    }
                } else {
                    // Received and send if Uploader is not null.
                    await UploaderSocket.SendAsync(
                        new ArraySegment<byte>(buffer, 0, result.Count),
                        result.MessageType,
                        result.EndOfMessage,
                       CancellationToken.None);
                }
                result = await downloaderSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }
            await downloaderSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
            DisconnectAsDownloader();
        }

        public async Task RetransmitToDownloader(WebSocket uploaderSocket) {
            var buffer = new byte[4096];
            WebSocketReceiveResult result = await uploaderSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            while (!result.CloseStatus.HasValue) {
                if (DownloaderSocket == null) {
                    // Receive and not send if Downloader is null.
                    if (result.EndOfMessage) {
                        await TddsService.SendMsgAsync(uploaderSocket, new TddsSvcMsg(MessageType.DownloaderOffline, "Uploader is not connected."));
                    }
                } else {
                    // Received and send if Downloader is not null.
                    await DownloaderSocket.SendAsync(
                        new ArraySegment<byte>(buffer, 0, result.Count),
                        result.MessageType,
                        result.EndOfMessage,
                        CancellationToken.None);
                }
                result = await uploaderSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }
            await uploaderSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
            DisconnectAsUploader();
        }

    }
}
