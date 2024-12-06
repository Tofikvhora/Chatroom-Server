using System.Net;
using System.Net.WebSockets;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var connection = new List<WebSocket>();


app.UseWebSockets();


app.MapGet("/ws",async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var UserName = context.Request.Query["name"];
        using var ws = await context.WebSockets.AcceptWebSocketAsync();
        connection.Add(ws);
        await BoradCast($"{UserName} Joined the room");
        await BoradCast($"{connection.Count} User Connected");
        await ReciveMessage(ws,
            async (result,buffer) =>
            {
                if(result.MessageType == WebSocketMessageType.Text)
                {
                    string message = Encoding.UTF8.GetString(buffer,0,buffer.Length);
                    await BoradCast(UserName + ": " + message);
                }else if(result.MessageType == WebSocketMessageType.Close || ws.State == WebSocketState.Open)
                {
                    connection.Remove(ws);
                    await BoradCast($"{UserName} left the room");
                    await BoradCast($"{connection.Count} users Connected");
                    await ws.CloseAsync(result.CloseStatus.Value,result.CloseStatusDescription,CancellationToken.None);
                }
                {

                } 
            }
            );

    }
    else
    {
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
    }
});

async Task ReciveMessage(WebSocket socket, Action<WebSocketReceiveResult, byte[]> handleMessage)
{

    var buffer = new byte[1024 / 10];
    while(socket.State == WebSocketState.Open)
    {
        var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer),CancellationToken.None);
        handleMessage(result, buffer);

    }
}

async Task BoradCast(string message)
{
    var bytes = Encoding.UTF8.GetBytes(message);
    foreach (var scoket in connection)
    {
        if(scoket.State == WebSocketState.Open)
        {
            var arraySagment = new ArraySegment<byte>(bytes, 0, bytes.Length);
            await scoket.SendAsync(arraySagment,WebSocketMessageType.Text, true, CancellationToken.None);
        }

    }


}

app.Run();
