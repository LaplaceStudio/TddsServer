using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.WebSockets;
using TddsServer.General;

namespace TddsServer.Controllers {
    public class NoticeController : ControllerBase {
        [HttpGet("/notice/download")]
        public async Task DownloadNotice() {
            if (HttpContext.WebSockets.IsWebSocketRequest) {
                using WebSocket webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

                // Connect as downloader
                TddsService.NoticeManager.ConnectAsDownloader(HttpContext, webSocket);
                // Retransmit
                await TddsService.NoticeManager.RetransmitToUploader(webSocket);
            } else {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
        }

        [HttpGet("/notice/upload")]
        public async Task UploadNotice() {
            if (HttpContext.WebSockets.IsWebSocketRequest) {
                using WebSocket webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

                // Connect as uploader
                TddsService.NoticeManager.ConnectAsUploader(HttpContext, webSocket);
                // Retransmit
                await TddsService.NoticeManager.RetransmitToDownloader(webSocket);
            } else {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
        }
    }
}
