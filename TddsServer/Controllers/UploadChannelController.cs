using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using TddsServer.General;
using TddsServer.Objects;

namespace TddsServer.Controllers {
    public class UploadChannelController : ControllerBase {
        [HttpGet("/UploadChannel")]
        public async Task Get(int channelId,int imageWidth,int imageHeight,int pixelFormat) {
            if (HttpContext.WebSockets.IsWebSocketRequest) {
                using WebSocket webSocket = await
                                   HttpContext.WebSockets.AcceptWebSocketAsync();

                TddsSvcMsg msg = ChannelManager.CreateUploadChannel(webSocket, channelId, imageWidth, imageHeight, pixelFormat);
                if (msg.Type == MessageType.Success) {
                    await ChannelManager.RetransmitToDownloadChannel(channelId, webSocket);
                } else {
                    HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                }
            } else {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
        }
    }
}
