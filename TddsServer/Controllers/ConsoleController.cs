using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.WebSockets;
using TddsServer.General;

namespace TddsServer.Controllers {
    public class ConsoleController : ControllerBase {
        [HttpGet("/console")]
        public async Task Get() {
            if (HttpContext.WebSockets.IsWebSocketRequest) {
                using WebSocket webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                ServiceManager.ConnectAsConsole(HttpContext, webSocket);
                await ServiceManager.RetransmitToTdds(webSocket);
            } else {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
        }
    }
}
