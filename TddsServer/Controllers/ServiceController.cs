using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TddsServer.General;
using TddsServer.Objects;

namespace TddsServer.Controllers {
    public class ServiceController : ControllerBase {

        /// <summary>
        /// HTTP API
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        [HttpPost("/service")]
        public ActionResult<TddsSvcMsg> Get([FromBody] TddsSvcMsg msg) {
            if (msg == null) return BadRequest();
            return TddsService.Handle(msg);
        }

        /// <summary>
        /// WebSocket API
        /// </summary>
        /// <returns></returns>
        [HttpGet("/service")]
        public async Task Get() {
            if (HttpContext.WebSockets.IsWebSocketRequest) {
                using WebSocket webSocket = await
                                   HttpContext.WebSockets.AcceptWebSocketAsync();

                await Holding(HttpContext, webSocket);
            } else {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
        }

        private async Task Holding(HttpContext httpContext, WebSocket webSocket) {
            var lenBytes = new byte[2];
            WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(lenBytes), CancellationToken.None);
            while (!result.CloseStatus.HasValue) {
                int len = BitConverter.ToUInt16(lenBytes.Reverse().ToArray(), 0);
                byte[] buffer = new byte[len];
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                string content = Encoding.UTF8.GetString(buffer);
                TddsSvcMsg? msg = JsonConvert.DeserializeObject(content,typeof(TddsSvcMsg)) as TddsSvcMsg;
                if (msg != null) {
                   await TddsService.SendMsgAsync(webSocket, TddsSvcMsg.UnresolvedMsg());
                } else {
                    // Handle TddsSvcMsg
                    TddsSvcMsg resultMsg = TddsService.Handle(msg);
                    // Return handle result
                    await TddsService.SendMsgAsync(webSocket, resultMsg);
                }
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(lenBytes), CancellationToken.None);
            }
            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }


    }
}
