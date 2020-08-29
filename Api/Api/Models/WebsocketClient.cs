using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Api.Models {
    public class WebsocketClient {

        public WebSocket Websocket {get;set; }

        public string ClientId { get;set;}

        public string RoomNo { get; set; }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public Task SendMessageAsync(string message) {
            var msg = Encoding.UTF8.GetBytes(message);
            return Websocket.SendAsync(new ArraySegment<byte>(msg,0,msg.Length),WebSocketMessageType.Text,true,CancellationToken.None);
        }
    }
}
