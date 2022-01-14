using System.Collections.Concurrent;
using System.Net.WebSockets;
using TddsServer.Objects;

namespace TddsServer.General {
    public class ChannelManager {
        /// <summary>
        /// 此服务支持的最大通道数。可依据实测网络环境更改此值。
        /// </summary>
        private const int FIXED_MAX_CHANNELS_COUNT = 32;
        /// <summary>
        /// 客户端可用最大通道数，上限为FIXED_MAX_CHANNELS_COUNT，
        /// </summary>
        private static int MAX_CHANNELS_COUNT = 32;

        private static ConcurrentDictionary<int, WebSocket> UploadChannels = new ConcurrentDictionary<int, WebSocket>();
        private static ConcurrentDictionary<int, WebSocket> DownloadChannels = new ConcurrentDictionary<int, WebSocket>();
        private static ConcurrentDictionary<int, CameraConfig> ChannelImageFormats = new ConcurrentDictionary<int, CameraConfig>();


        public static List<int> GetChannelIds() {
            return UploadChannels.Keys.ToList();
        }

        public static async Task<TddsSvcMsg> CreateUploadChannel(WebSocket webSocket, int channelId, int imageWidth, int imageHeight, int pixelFormt) {
            TddsSvcMsg msg;
            if (UploadChannels.Keys.Count >= MAX_CHANNELS_COUNT) {
                msg = new TddsSvcMsg(MessageType.Error, "The maximum number of channels has been reached.");
                await Logger.Log(LogType.Info,msg.Message);
                return msg;
            }
            if(!Enum.IsDefined(typeof(PixelFormatType),pixelFormt) || pixelFormt == (int)PixelFormatType.UNKNOWN) {
                msg = new TddsSvcMsg(MessageType.Error, $"PixelFormat [{pixelFormt}] is not available.");
                return msg;
            }

            if (UploadChannels.TryGetValue(channelId, out WebSocket? oldChannel)) {
                await Logger.Log(LogType.Info,$"Close old channel for create new channel with id {channelId}.");
                oldChannel?.Abort();
                await Logger.Log(LogType.Info,$"Old channel with id {channelId} is closed.");
            }

            CameraConfig format = new CameraConfig() {
                ChannelId = channelId.ToString(),
                ImageWidth = imageWidth,
                ImageHeight = imageHeight,
                PixelFormat = Enum.IsDefined(typeof(PixelFormatType), pixelFormt) ? ((PixelFormatType)pixelFormt) : PixelFormatType.UNKNOWN
            };
            UploadChannels[channelId] = webSocket;
            ChannelImageFormats[channelId] = format;

            msg = new TddsSvcMsg(MessageType.Success, $"Create channel with id:{channelId}, imageWidth:{imageWidth}, imageHeight:{imageHeight}, pixelFormat:{pixelFormt} successfully.", channelId);
            await Logger.Log(LogType.Info,msg.Message);
            return msg;
        }

        public static async Task<TddsSvcMsg> RemoveUploadChannelAsync(int id) {
            TddsSvcMsg msg;
            if(UploadChannels.TryRemove(id, out WebSocket? socket)) {
                socket?.Dispose();
                ChannelImageFormats.TryRemove(id, out _);
                msg = new TddsSvcMsg(MessageType.Success, $"Uploading channel {id} is removed successfully.");
            } else {
                msg = new TddsSvcMsg(MessageType.Error, $"Remove uploading channel {id} failed. No channel with id {id}.");
            }
            await Logger.Log(LogType.Info,msg.Message);
            return msg;
        }

        public static async Task<bool> CreateDownloadChannel(int id, WebSocket webSocket) {
            TddsSvcMsg msg;
            // Return false if id dose not exist.
            if (!UploadChannels.ContainsKey(id)) {
                msg = new TddsSvcMsg(MessageType.Error, $"Cannot create downloading channel {id}, because no channel with id {id}.");
                await Logger.Log(LogType.Info,msg.Message);
                return false;
            }
            // Close old download channel
            if (DownloadChannels.TryGetValue(id, out WebSocket? oldSocket)) {
                if (oldSocket != null) {
                    await Logger.Log(LogType.Info,$"Close channel {id} for new downloading channel.");
                    oldSocket.Dispose();
                    await Logger.Log(LogType.Info,"Old downloading channel is closed.");
                }
            }
            // Add to channel dictinary.
            DownloadChannels[id] = webSocket;
            await Logger.Log(LogType.Info,$"Created downloading channel with id {id}");
            return true;
        }

        public static async Task<TddsSvcMsg> RemoveDownloadChannelAsync(int id) {
            TddsSvcMsg msg;
            if (DownloadChannels.TryRemove(id, out WebSocket? socket)) {
                socket?.Dispose();
                msg = new TddsSvcMsg(MessageType.Success, $"Downloading channel {id} is removed successfully.");
            } else {
                msg = new TddsSvcMsg(MessageType.Error, $"Remove downloading channel {id} failed. No channel with id {id}.");
            }
            await Logger.Log(LogType.Info,msg.Message);
            return msg;
        }

        public static async Task RetransmitToDownloadChannel(int id, WebSocket webSocket) {
            var buffer = new byte[10240];
            await Logger.Log(LogType.Info,$"Retransmit channel {id}.");
            try {
                WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                while (!result.CloseStatus.HasValue) {
                    if (DownloadChannels.TryGetValue(id, out WebSocket? downloadSocket)) {
                        if (downloadSocket != null && downloadSocket.State == WebSocketState.Open) {
                            try {
                            await downloadSocket.SendAsync(
                                new ArraySegment<byte>(buffer, 0, result.Count),
                                result.MessageType,
                                result.EndOfMessage,
                                CancellationToken.None);

                            }catch(Exception exp) {
                               await Logger.Log(LogType.Error, exp.Message);
                            }
                        }
                    }
                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                }
                await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
                await Logger.Log(LogType.Info,$"Channel {id} is closed.");
            }catch(Exception exp) {
                await Logger.Log(LogType.Error,exp.StackTrace);
            } finally {
                await RemoveUploadChannelAsync(id);
                await Logger.Log(LogType.Info,$"Channel {id} is closed.");
            }
        }

        public static async Task HoldingDownloadChannel(int id, WebSocket webSocket) {
            var buffer = new byte[1024];
            try {
                WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                while (!result.CloseStatus.HasValue) {
                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                }
                await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
            } catch (Exception exp) {
                await Logger.Log(LogType.Info,exp.Message);
            } finally {
               await RemoveDownloadChannelAsync(id);
            }
        }

        public static async Task<TddsSvcMsg> GetChannelImageFormat(int channelId) {
            TddsSvcMsg msg;
            if (ChannelImageFormats.TryGetValue(channelId, out CameraConfig? format)) {
                msg = new TddsSvcMsg(MessageType.Success, "Success", format);
            } else {
                msg= new TddsSvcMsg(MessageType.Error, $"No channel with ID:{channelId}.");
            }
            await Logger.Log(LogType.Info,msg.Message);
            return msg;
        }

        public static async Task<TddsSvcMsg> GetAllChannelsImageFormats() {
            TddsSvcMsg msg;
            if (ChannelImageFormats == null || ChannelImageFormats.IsEmpty) {
                msg = new TddsSvcMsg(MessageType.Error, "No any channels.");
            } else {
                msg = new TddsSvcMsg(MessageType.Success, $"Got image format of {ChannelImageFormats.Count} channels.", ChannelImageFormats.Values.ToList());
            }
            await Logger.Log(LogType.Info,msg.Message);
            return msg;
        }
    }
}
