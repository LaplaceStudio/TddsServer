using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using TddsServer.General;

namespace TddsServer.Controllers {
    public class TddsController : ControllerBase {

        [HttpGet("/tdds")]
        public async Task Get() {
            if (HttpContext.WebSockets.IsWebSocketRequest) {
                using WebSocket webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                ServiceManager.ConnectAsTdds(HttpContext, webSocket);
                await ServiceManager.RetransmitToConsole(webSocket);
            } else {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
        }
    }
}
