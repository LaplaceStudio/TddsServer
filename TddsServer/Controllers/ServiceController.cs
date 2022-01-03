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

        [HttpGet("/service")]
        public async Task Get() {
            if (HttpContext.WebSockets.IsWebSocketRequest) {
                using WebSocket webSocket = await
                                   HttpContext.WebSockets.AcceptWebSocketAsync();

                await Utils.Echo(HttpContext, webSocket);

            } else {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
        }



        [HttpPost("/service")]
        public async Task<ActionResult<TddsSvcMsg>> Get([FromBody] TddsSvcMsg msg) {
            if (msg == null) return BadRequest();
            switch (msg.Type) {
                case MessageType.ServiceOnline:
                    return new TddsSvcMsg(MessageType.Success, "Service is online.");
                case MessageType.GetChannelIds:
                    var ids = ChannelManager.GetChannelIds();
                    return new TddsSvcMsg(msg.Type, $"Got {ids.Count} uploading channels.", ids);
                case MessageType.SetChannelImageFormat:
                    if(msg.Data !=null && JsonConvert.DeserializeObject(msg.Data.ToString(),typeof(ChannelImageFormat)) is ChannelImageFormat cif){
                        return await ChannelManager.SetChannelImageFormat(cif);
                    }else {
                        return TddsSvcMsg.InvalidParamMsg(nameof(ChannelImageFormat));
                    }
                default:
                    return new TddsSvcMsg(MessageType.Error, "Unknow message type.");
            }
        }
    }
}
