﻿using System.Collections.Concurrent;
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

        public static TddsSvcMsg CreateUploadChannel(WebSocket webSocket, int channelId, int imageWidth, int imageHeight, int pixelFormt) {
            TddsSvcMsg msg= new TddsSvcMsg();
            if (UploadChannels.Keys.Count >= MAX_CHANNELS_COUNT) {
                msg = new TddsSvcMsg(MessageType.Error, "The maximum number of channels has been reached.");
                Console.WriteLine(msg.Message);
                return msg;
            }
            if(!Enum.IsDefined(typeof(PixelFormatType),pixelFormt) || pixelFormt == (int)PixelFormatType.UNKNOWN) {
                msg = new TddsSvcMsg(MessageType.Error, $"PixelFormat [{pixelFormt}] is not available.");
                return msg;
            }

            if (UploadChannels.TryGetValue(channelId, out WebSocket? oldChannel)) {
                Console.WriteLine($"Close old channel for create new channel with id {channelId}.");
                oldChannel?.Abort();
                Console.WriteLine($"Old channel with id {channelId} is closed.");
            }

            ChannelImageFormat format = new ChannelImageFormat() {
                ChannelId = channelId,
                ImageWidth = (uint)imageWidth,
                ImageHeight = (uint)imageHeight,
                PixelFormat = Enum.IsDefined(typeof(PixelFormatType), pixelFormt) ? ((PixelFormatType)pixelFormt) : PixelFormatType.UNKNOWN
            };
            UploadChannels[channelId] = webSocket;
            ChannelImageFormats[channelId] = format;

            msg = new TddsSvcMsg(MessageType.Success, $"Create channel with id:{channelId}, imageWidth:{imageWidth}, imageHeight:{imageHeight}, pixelFormat:{pixelFormt} successfully.", channelId);
            Console.WriteLine(msg.Message);
            return msg;
        }

        public static TddsSvcMsg RemoveUploadChannelAsync(int id) {
            TddsSvcMsg msg;
            if(UploadChannels.TryRemove(id, out WebSocket? socket)) {
                socket?.Dispose();
                ChannelImageFormats.TryRemove(id, out _);
                msg = new TddsSvcMsg(MessageType.Success, $"Uploading channel {id} is removed successfully.");
            } else {
                msg = new TddsSvcMsg(MessageType.Error, $"Remove uploading channel {id} failed. No channel with id {id}.");
            }
            Console.WriteLine(msg.Message);
            return msg;
        }

        public static bool CreateDownloadChannel(int id, WebSocket webSocket) {
            TddsSvcMsg msg;
            // Return false if id dose not exist.
            if (!UploadChannels.Keys.Contains(id)) {
                msg = new TddsSvcMsg(MessageType.Error, $"Cannot create downloading channel {id}, because no channel with id {id}.");
                Console.WriteLine(msg.Message);
                return false;
            }
            // Close old download channel
            if (DownloadChannels.TryGetValue(id, out WebSocket? oldSocket)) {
                if (oldSocket != null) {
                    Console.WriteLine($"Close channel {id} for new downloading channel.");
                    oldSocket.Dispose();
                    Console.WriteLine("Old downloading channel is closed.");
                }
            }
            // Add to channel dictinary.
            DownloadChannels[id] = webSocket;
            Console.WriteLine($"Created downloading channel with id {id}");
            return true;
        }

        public static TddsSvcMsg RemoveDownloadChannelAsync(int id) {
            TddsSvcMsg msg;
            if (UploadChannels.TryRemove(id, out WebSocket? socket)) {
                socket?.Dispose();
                msg = new TddsSvcMsg(MessageType.Success, $"Downloading channel {id} is removed successfully.");
            } else {
                msg = new TddsSvcMsg(MessageType.Error, $"Remove downloading channel {id} failed. No channel with id {id}.");
            }
            Console.WriteLine(msg.Message);
            return msg;
        }

        public static async Task RetransmitToDownloadChannel(int id, WebSocket webSocket) {
            var buffer = new byte[10240];
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
                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                }
                await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
                Console.WriteLine($"Channel {id} is closed.");
            }catch(Exception exp) {
                Console.WriteLine(exp.ToString());
            } finally {
                RemoveUploadChannelAsync(id);
                Console.WriteLine($"Channel {id} is closed.");
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
                RemoveDownloadChannelAsync(id);
            }
        }

        public static TddsSvcMsg GetChannelImageFormat(int channelId) {
            TddsSvcMsg msg;
            if (ChannelImageFormats.TryGetValue(channelId, out ChannelImageFormat? format)) {
                msg = new TddsSvcMsg(MessageType.Success, "Success", format);
            } else {
                msg= new TddsSvcMsg(MessageType.Error, $"No channel with ID:{channelId}.");
            }
            Console.WriteLine(msg.Message);
            return msg;
        }

        public static TddsSvcMsg GetAllChannelsImageFormats() {
            TddsSvcMsg msg;
            if (ChannelImageFormats == null || ChannelImageFormats.Count == 0) {
                msg = new TddsSvcMsg(MessageType.Error, "No any channels.");
            } else {
                msg = new TddsSvcMsg(MessageType.Success, $"Got image format of {ChannelImageFormats.Count} channels.", ChannelImageFormats.Values.ToList());
            }
            Console.WriteLine(msg.Message);
            return msg;
        }
    }
}
