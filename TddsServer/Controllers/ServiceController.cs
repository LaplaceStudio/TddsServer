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
        public async Task<ActionResult<TddsSvcMsg>> Get([FromBody] TddsSvcMsg msg) {
            if (msg == null) return BadRequest();
            return await TddsService.Handle(msg);
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
            await Logger.Log(LogType.Info, "Connection created for TDDS service.");
            var lenBytes = new byte[2];
            try {
                WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(lenBytes), CancellationToken.None);
                while (!result.CloseStatus.HasValue) {
                    int len = BitConverter.ToUInt16(lenBytes.Reverse().ToArray(), 0);
                    if (len > 0) {
                        byte[] buffer = new byte[len];
                        await Logger.Log(LogType.Info, $"Attempt to receive data of length {len}...");
                        result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                        string content = Encoding.UTF8.GetString(buffer);
                        await Logger.Log(LogType.Info, $"Received content:{content}");
                        try {
                            TddsSvcMsg? msg = JsonConvert.DeserializeObject(content, typeof(TddsSvcMsg)) as TddsSvcMsg;
                            await Logger.Log(LogType.Info, "Attempt to handle this request...");
                            // Handle TddsSvcMsg
                            TddsSvcMsg resultMsg = await TddsService.Handle(msg);
                            await Logger.Log(LogType.Info, $"Request handled. Result:{resultMsg.GetJson()}");
                            // Return handle result
                            await TddsService.SendMsgAsync(webSocket, resultMsg);
                            await Logger.Log(LogType.Info, "Result sent to user.");
                        } catch (Exception exp) {
                            await TddsService.SendMsgAsync(webSocket, TddsSvcMsg.UnresolvedMsg());
                            await Logger.Log(LogType.Error, TddsSvcMsg.UnresolvedMsg().Message);
                            await Logger.Log(LogType.Error, exp.StackTrace);
                        }
                    }
                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(lenBytes), CancellationToken.None);
                }
                await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
                await Logger.Log(LogType.Info,"Connection is closed.");
            }catch(Exception outExp) {
                await Logger.Log(LogType.Info, "Connection is closed. " + outExp.Message);
            }
        }


    }
}
