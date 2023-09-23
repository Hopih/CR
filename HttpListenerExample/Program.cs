using System.Net;
using HttpListenerExample;
using WebServerExample;

var httpListener = new HttpListener();
httpListener.Prefixes.Add("http://localhost:5000/");

httpListener.Start();
while (httpListener.IsListening)
{
    var context = await httpListener.GetContextAsync();
    var response = context.Response;
    var request = context.Request;
    var ctx = new CancellationTokenSource();
    _ = Task.Run(async () =>
    {
        switch (request.Url?.LocalPath){
            case "/home":
                await WebHelper.ShowIndex(context);
                break;
            case "/weather":
                await WebHelper.ShowWeatherApi(context);
                break;
            case "/create/db":
            {
                var db = new DbContext();
                await db.CreateDbAsync(ctx.Token);
                break;
            }
            case "/signup":
                await WebHelper.ShowSignUp(context);
                break;
            case "/registration":
                await WebHelper.AddUserAsync(context,ctx.Token);
                break;
            default:
                await WebHelper.ShowStatic(context);
                break;
        }
        response.OutputStream.Close();
        response.Close();
    });
}
httpListener.Stop();
httpListener.Close();