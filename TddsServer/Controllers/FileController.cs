using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.WebSockets;
using TddsServer.General;

namespace TddsServer.Controllers {
    public class FileController : ControllerBase {
        [HttpGet("/file/download")]
        public async Task DownloadFile() {
            if (HttpContext.WebSockets.IsWebSocketRequest) {
                using WebSocket webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

                // Connect as downloader
                TddsService.FileManager.ConnectAsDownloader(HttpContext, webSocket);
                // Retransmit
                await TddsService.FileManager.RetransmitToUploader(webSocket);
            } else {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
        }

        [HttpGet("/file/upload")]
        public async Task UploadFile() {
            if (HttpContext.WebSockets.IsWebSocketRequest) {
                using WebSocket webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

                // Connect as uploader
                TddsService.FileManager.ConnectAsUploader(HttpContext, webSocket);
                // Retransmit
                await TddsService.FileManager.RetransmitToDownloader(webSocket);
            } else {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
        }
    }
}
