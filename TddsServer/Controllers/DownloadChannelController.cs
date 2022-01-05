using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.WebSockets;
using TddsServer.General;

namespace TddsServer.Controllers {
    public class DownloadChannelController : ControllerBase {
        [HttpGet("/DownloadChannel")]
        public async Task Get(int id) {
            if (HttpContext.WebSockets.IsWebSocketRequest) {
                using WebSocket webSocket = await
                                   HttpContext.WebSockets.AcceptWebSocketAsync();
                bool success = await ChannelManager.CreateDownloadChannel(id, webSocket);
                if (success)
                    await ChannelManager.HoldingDownloadChannel(id, webSocket);
                else
                    HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            } else {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
        }
    }
}
