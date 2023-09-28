using System.Net;
using HttpListenerExample;

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
                await WebHelper.ShowIndex(context, ctx.Token);
                break;
            case "/weather":
                await WebHelper.ShowWeatherApi(context, ctx.Token);
                break;
            case "/create/db":
            {
                var db = new DbContext();
                await db.CreateDbAsync(ctx.Token);
                break;
            }
            case "/signup":
                await WebHelper.ShowSignUp(context, ctx.Token);
                break;
            case "/signin":
                await WebHelper.ShowSignIn(context, ctx.Token);
                break;
            case "/registration":
                await WebHelper.AddUserAsync(context,ctx.Token);
                break;
            case "/login":
                await WebHelper.ShowLogin(context, ctx.Token);
                break;
            default:
                await WebHelper.ShowStatic(context, ctx.Token);
                break;
        }
        response.OutputStream.Close();
        response.Close();
    });
}
httpListener.Stop();
httpListener.Close();