using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.WebSockets;
using TddsServer.General;

namespace TddsServer.Controllers {
    public class UploadChannelController : ControllerBase {
        [HttpGet("/UploadChannel")]
        public async Task Get() {
            if (HttpContext.WebSockets.IsWebSocketRequest) {
                using WebSocket webSocket = await
                                   HttpContext.WebSockets.AcceptWebSocketAsync();

                int id = await ChannelManager.CreateUploadChannelAsync(webSocket);
                if (id > 0) {
                    await ChannelManager.RetransmitToDownloadChannel(id, webSocket);
                }
            } else {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
        }
    }
}
