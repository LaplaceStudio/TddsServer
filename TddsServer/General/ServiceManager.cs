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

        //private static readonly ILogger logger = new Log4NetLogger(new Log4NetProviderOptions());

        public bool IsUploaderOnline => UploaderSocket != null && UploaderSocket.State == WebSocketState.Open;
        public bool IsDownloaderOnline => DownloaderSocket != null && DownloaderSocket.State == WebSocketState.Open;

        public async void ConnectAsUploader(HttpContext httpContext, WebSocket webSocket) {
            if (UploaderSocket != null) {
                await Logger.Log(LogType.Info,"Uploader already exists. Aborting old uploader...");
                UploaderSocket.Abort();
                await Logger.Log(LogType.Info,"Aborted old uploader.");
                await Logger.Log(LogType.Info, "Old uploader socket is aborted.");
            }
            UploaderSocket = webSocket;
            UploaderId = httpContext.Connection.RemoteIpAddress == null
                ? httpContext.Connection.Id
                : $"{httpContext.Connection.RemoteIpAddress}:{httpContext.Connection.RemotePort}";
            TddsSvcMsg msg = new TddsSvcMsg(MessageType.UploaderOnline, $"Connect as uploader successfully. Id:{UploaderId}.");
            if (DownloaderSocket != null)
                await TddsService.SendMsgAsync(DownloaderSocket, msg);
            await TddsService.SendMsgAsync(UploaderSocket, msg);

            await Logger.Log(LogType.Info,msg.Message);

            await Logger.Log(LogType.Info, $"Id:{UploaderId} connected service as uploader.");
        }

        public async void DisconnectAsUploader() {
            TddsSvcMsg msg = new TddsSvcMsg(MessageType.DownloaderOffline, "Uploader logged out.");
            if (DownloaderSocket != null) {
                await TddsService.SendMsgAsync(DownloaderSocket, msg);
            }
            //logger.LogInformation($"Id:{TddsConnectionId} disconnected service.");

            UploaderSocket = null;
            UploaderId = null;
            await Logger.Log(LogType.Info,msg.Message);
        }

        public async void ConnectAsDownloader(HttpContext httpContext, WebSocket webSocket) {
            if (DownloaderSocket != null) {
                await Logger.Log(LogType.Info,"Downloader already exists. Aborting old downloader.");
                DownloaderSocket.Abort();
                await Logger.Log(LogType.Info,"Aborted old downloader.");
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

            await Logger.Log(LogType.Info,msg.Message);
        }

        public async void DisconnectAsDownloader() {
            TddsSvcMsg msg = new TddsSvcMsg(MessageType.DownloaderOffline, "Downloader logged out.");
            if (UploaderSocket != null)
                await TddsService.SendMsgAsync(UploaderSocket, msg);
            DownloaderSocket?.Abort();
            //logger.LogInformation($"Id:{TddsConnectionId} disconnected service.");

            DownloaderSocket = null;
            DownloaderId = null;
            await Logger.Log(LogType.Info,msg.Message);
        }

        public async Task RetransmitToUploader(WebSocket downloaderSocket) {
            var buffer = new byte[4096];
            try {
                WebSocketReceiveResult result = await downloaderSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                while (!result.CloseStatus.HasValue) {
                    if (UploaderSocket == null) {
                        // Receive and not send if Uploader is null.
                        try {
                            if (result.EndOfMessage) {
                                await TddsService.SendMsgAsync(downloaderSocket, new TddsSvcMsg(MessageType.DownloaderOffline, "Uploader is not connected."));
                            }
                        }catch(Exception exp) {
                            await Logger.Log(LogType.Error, exp.Message);
                        }
                    } else {
                        // Received and send if Uploader is not null.
                        try {
                            await UploaderSocket.SendAsync(
                                new ArraySegment<byte>(buffer, 0, result.Count),
                                result.MessageType,
                                result.EndOfMessage,
                               CancellationToken.None);
                        }catch(Exception exp) {
                            await Logger.Log(LogType.Error,exp.Message);
                        }
                    }
                    result = await downloaderSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                }
                await downloaderSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
                DisconnectAsDownloader();
            }catch(Exception exp) {
                await Logger.Log(LogType.Error,"Connection is closed. " + exp.StackTrace);
            }
        }

        public async Task RetransmitToDownloader(WebSocket uploaderSocket) {
            var buffer = new byte[4096];
            try {
                WebSocketReceiveResult result = await uploaderSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                while (!result.CloseStatus.HasValue) {
                    if (DownloaderSocket == null) {
                        // Receive and not send if Downloader is null.
                        try {
                            if (result.EndOfMessage) {
                                await TddsService.SendMsgAsync(uploaderSocket, new TddsSvcMsg(MessageType.DownloaderOffline, "Downlaoder is not connected."));
                            }
                        } catch (Exception exp) { 
                            await Logger.Log(LogType.Error, exp.Message);
                        }
                    } else {
                        // Received and send if Downloader is not null.
                        try {
                            await DownloaderSocket.SendAsync(
                                new ArraySegment<byte>(buffer, 0, result.Count),
                                result.MessageType,
                                result.EndOfMessage,
                                CancellationToken.None);
                        } catch (Exception exp) { 
                            await Logger.Log(LogType.Error,exp.Message);
                        }
                    }
                    result = await uploaderSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                }
                await uploaderSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
                DisconnectAsUploader();
            }catch(Exception exp) {
                await Logger.Log(LogType.Info,"Connection is closed. "+ exp.StackTrace);
            }
        }

    }
}
