using System.Net;
using WebServerExample;


var httpListener = new HttpListener();
httpListener.Prefixes.Add("http://localhost:5000/");

var methods = typeof(WebHelper).GetMethods()
    .Where(mi => mi.GetCustomAttributes(true).Any(attr => attr is Mapping)).ToArray();
var defaultMethod = methods.First(x =>
{
    return x.GetCustomAttributes(true).Any(y => y is Mapping { Method: "*", Route: "*" });
});


httpListener.Start();
while (httpListener.IsListening)
{
    var context = await httpListener.GetContextAsync();
    var response = context.Response;
    _ = Task.Run(async () =>
    {
        var routeName = context.Request.Url?.Segments[1].Replace("/", "");
        
        var method = methods.FirstOrDefault(x =>
        {
            return x.GetCustomAttributes(true).Any(y =>
                y is Mapping mapping && mapping.Method == context.Request.HttpMethod && mapping.Route == routeName);
        });
        if (method == null)
        {
            await ((Task) defaultMethod.Invoke(null, new object[] { context }))!.ConfigureAwait(false);
        }
        else
        {
            await ((Task) method.Invoke(null, new object[] { context }))!.ConfigureAwait(false);
        }
        /*switch (request.Url?.LocalPath)
        {
            case "/home":
                await WebHelper.ShowIndex(context);
                break;
            case "/weather":
                await WebHelper.ShowWeatherApi(context);
                break;
            case "/user":
                await WebHelper.ShowUserGuid(context);
                break;
            default:
                await WebHelper.ShowStatic(context);
                break;
        }*/
        response.OutputStream.Close();
        response.Close();
    });
}
httpListener.Stop();
httpListener.Close();