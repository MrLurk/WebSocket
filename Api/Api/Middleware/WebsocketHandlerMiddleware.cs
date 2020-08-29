using Api.Models;
using Api.Store;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Api.Middleware {
    public class WebsocketHandlerMiddleware {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        public WebsocketHandlerMiddleware(
            RequestDelegate next,
            ILoggerFactory loggerFactory
            ) {
            _next = next;
            _logger = loggerFactory.
                CreateLogger<WebsocketHandlerMiddleware>();
        }

        public async Task Invoke(HttpContext context) {
            if (context.Request.Path == "/ws") {
                if (context.WebSockets.IsWebSocketRequest) {
                    WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    string clientId = Guid.NewGuid().ToString();
                    WebsocketClient client = new WebsocketClient() {
                        ClientId = clientId,
                        Websocket = webSocket
                    };
                    try {
                        await Handle(client);
                    } catch (Exception ex) {
                        await context.Response.WriteAsync("closed");
                    }

                } else {
                    context.Response.StatusCode = 404;
                }
            } else {
                await _next(context);
            }
        }

        private async Task Handle(WebsocketClient client) {
            WebsocketClientCollection.Add(client);

            WebSocketReceiveResult result = null;
            do {
                var buffer = new byte[1024 * 1]; // 1M数据
                result = await client.Websocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Text && !result.CloseStatus.HasValue) {
                    var msgString = Encoding.UTF8.GetString(buffer);
                    var message = JsonConvert.DeserializeObject<Message>(msgString);
                    message.SendClientId = client.ClientId;
                    MessageRoute(message);
                }
            } while (!result.CloseStatus.HasValue); // 未关闭请求一直执行

        }

        private void MessageRoute(Message message) {
            var client = WebsocketClientCollection.Get(message.SendClientId);
            switch (message.action) {
                case "join":
                    // 加入房间
                    client.RoomNo = message.msg;
                    // 向房间内的所有人发送 xx 加入房间
                    var inRoomClients = WebsocketClientCollection.GetRoomClients(client.RoomNo);
                    inRoomClients.ForEach(c => {
                        c.SendMessageAsync($"{message.nick} 加入房间 {client.RoomNo} 成功 .");
                    });
                    break;
                case "send_to_room":
                    // 发送消息
                    if (string.IsNullOrEmpty(client.RoomNo)) {
                        break;
                    }
                    // 读取房间内所有的用户
                    var clients = WebsocketClientCollection.GetRoomClients(client.RoomNo);
                    clients.ForEach(c => {
                        c.SendMessageAsync(message.nick + " : " + message.msg);
                    });
                    break;
                case "leave":
                    // 退出房间
                    var roomNo = client.RoomNo;
                    client.RoomNo = "";
                    client.SendMessageAsync($"{message.nick} 离开房间 {roomNo} 成功 .");
                    break;
                default:
                    break;
            }


        }
    }
}
