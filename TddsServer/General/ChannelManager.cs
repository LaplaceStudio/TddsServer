using System.Collections.Concurrent;
using System.Net.WebSockets;
using TddsServer.Objects;

namespace TddsServer.General {
    public class ChannelManager {
        /// <summary>
        /// 此服务支持的最大通道数。可依据实测网络环境更改此值。
        /// </summary>
        private const int FIXED_MAX_CHANNELS_COUNT = 8;
        /// <summary>
        /// 客户端可用最大通道数，上限为FIXED_MAX_CHANNELS_COUNT，
        /// </summary>
        private static int MAX_CHANNELS_COUNT = 4;

        private static ConcurrentDictionary<int, WebSocket> UploadChannels = new ConcurrentDictionary<int, WebSocket>();
        private static ConcurrentDictionary<int, WebSocket> DownloadChannels = new ConcurrentDictionary<int, WebSocket>();
        private static ConcurrentDictionary<int, ChannelImageFormat> ChannelImageFormats = new ConcurrentDictionary<int, ChannelImageFormat>();


        public static List<int> GetChannelIds() {
            return UploadChannels.Keys.ToList();
        }

        public static async Task<int> CreateUploadChannelAsync(WebSocket webSocket) {
            if (UploadChannels.Keys.Count >= MAX_CHANNELS_COUNT) {
                TddsSvcMsg msg = new TddsSvcMsg(MessageType.Error, "The maximum number of channels has been reached.");
                await ServiceManager.SendAsync(webSocket, msg.GetJson());
                Console.WriteLine(msg.Message);
                return -1;
            } else {
                List<int> chIds = UploadChannels.Keys.ToList();
                int id = Enumerable.Range(1, MAX_CHANNELS_COUNT).Except(chIds).First();
                UploadChannels[id] = webSocket;
                TddsSvcMsg msg2 = new TddsSvcMsg(MessageType.Success, "Create channel successfully.", id);
                await ServiceManager.SendAsync(webSocket, msg2.GetJson());
                Console.WriteLine(msg2.Message);
                return id;
            }
        }

        public static async Task RemoveUploadChannelAsync(int id) {
            TddsSvcMsg msg;
            if(UploadChannels.TryRemove(id, out WebSocket? socket)) {
                socket?.Dispose();
                msg = new TddsSvcMsg(MessageType.Success, $"Uploading channel {id} is removed successfully.");
            } else {
                msg = new TddsSvcMsg(MessageType.Error, $"Remove uploading channel {id} failed.");
            }
            if(ServiceManager.TddsSocket!=null)
                await ServiceManager.SendAsync(ServiceManager.TddsSocket, msg.GetJson());
            if (ServiceManager.ConsoleSocket != null)
                await ServiceManager.SendAsync(ServiceManager.ConsoleSocket, msg.GetJson());
        }

        public static async Task<bool> CreateDownloadChannel(int id, WebSocket webSocket) {
            // Return false if id dose not exist.
            if (!UploadChannels.Keys.Contains(id)) {
                Console.WriteLine($"Cannot create downloading channel {id} because {id} is not available.");
                return false;
            }
            // Return false if image format dose not exist.
            if (ChannelImageFormats.TryGetValue(id, out var imageFormat)) {
                if (imageFormat == null) {
                    Console.WriteLine($"Connot create downloading channel {id} because ImageFormat is null.");
                    return false;
                }
            } else {
                Console.WriteLine($"Connot create downloading channel {id} because ImageFormat is null.");
                return false;
            }
            // Close old download channel
            if (DownloadChannels.TryGetValue(id, out WebSocket? oldSocket)) {
                if (oldSocket != null) {
                    Console.WriteLine($"Close channel {id} for new downloading channel.");
                    try {
                        await oldSocket.CloseAsync(WebSocketCloseStatus.Empty, "Close for new downloading channel.", CancellationToken.None);
                    }catch(Exception exp) {
                        Console.WriteLine("Error occur when close old socket. " + exp.Message);
                    }
                }
            }
            // Add to channel dictinary.
            DownloadChannels[id] = webSocket;
            Console.WriteLine($"Created downloading channel with id {id}");
            return true;
        }

        public static async Task RemoveDownloadChannelAsync(int id) {
            TddsSvcMsg msg;
            if (UploadChannels.TryRemove(id, out WebSocket? socket)) {
                socket?.Dispose();
                msg = new TddsSvcMsg(MessageType.Success, $"Downloading channel {id} is removed successfully.");
            } else {
                msg = new TddsSvcMsg(MessageType.Error, $"Remove downloading channel {id} failed.");
            }
            if (ServiceManager.TddsSocket != null)
                await ServiceManager.SendAsync(ServiceManager.TddsSocket, msg.GetJson());
            if (ServiceManager.ConsoleSocket != null)
                await ServiceManager.SendAsync(ServiceManager.ConsoleSocket, msg.GetJson());
            Console.WriteLine(msg.Message);
        }

        public static async Task RetransmitToDownloadChannel(int id, WebSocket webSocket) {
            var buffer = new byte[10240];
            DateTime time0 = DateTime.Now;
            int count = 0;
            Console.WriteLine($"Retransmit channel {id}.");
            try {
                WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                while (!result.CloseStatus.HasValue) {
                    if (DownloadChannels.TryGetValue(id, out WebSocket? downloadSocket)) {
                        if (downloadSocket != null && downloadSocket.State==WebSocketState.Open)
                            await downloadSocket.SendAsync(
                                new ArraySegment<byte>(buffer, 0, result.Count),
                                result.MessageType,
                                result.EndOfMessage,
                                CancellationToken.None);

                    }
                    //if ((DateTime.Now - time0).TotalMilliseconds > 2500) {
                    //    Console.WriteLine($"Received data count:{result.Count}");
                    //    time0 = DateTime.Now;
                    //}

                    //if (result.EndOfMessage) {
                    //    Console.WriteLine($"Message end. Data count:{count}");
                    //    count = 0;
                    //}
                    //count += result.Count;
                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                }
                await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
                Console.WriteLine($"Channel {id} is closed.");
            }catch(Exception exp) {
                Console.WriteLine(exp.ToString());
            } finally {
                await RemoveUploadChannelAsync(id);
                Console.WriteLine($"Channel {id} is removed.");
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
                Console.WriteLine(exp.Message);
            } finally {
                await RemoveDownloadChannelAsync(id);
            }
        }

        public static async Task<TddsSvcMsg> SetChannelImageFormat(ChannelImageFormat channelImageFormat) {
            return await Task.Run(() => {
                if (UploadChannels.Keys.Contains(channelImageFormat.ChannelId)) {
                    ChannelImageFormats[channelImageFormat.ChannelId] = channelImageFormat;
                    return new TddsSvcMsg(MessageType.SetChannelImageFormat, $"Set format of channel {channelImageFormat.ChannelId} successfully.");
                } else {
                    return new TddsSvcMsg(MessageType.Error, $"There is no channel with Id:{channelImageFormat.ChannelId}");
                }
            });
        }
    }
}
