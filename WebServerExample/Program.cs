using System.Net;
using System.Reflection;
using System.Text;
using CacheExample;
using StackExchange.Redis;
using WebServerExample;


var httpListener = new HttpListener();
httpListener.Prefixes.Add("http://localhost:5000/");

var methods = typeof(WebHelper).GetMethods()
    .Where(mi => mi.GetCustomAttributes(true).Any(attr => attr is Mapping)).ToArray();
var defaultMethod = methods.First(x =>
{
    return x.GetCustomAttributes(true).Any(y => y is Mapping { Method: "*", Route: "*" });
});

var store = RedisStore.RedisCache;

httpListener.Start();
while (httpListener.IsListening)
{
    var context = await httpListener.GetContextAsync();
    var response = context.Response;
    _ = Task.Run(async () =>
    {
        var method = GetMethodByContext(context);
        if (method != null)
        {
            await ((Task) method.Invoke(null, new object[] { context }))!.ConfigureAwait(false);
        }
        response.OutputStream.Close();
        response.Close();
    });
}
httpListener.Stop();
httpListener.Close();

MethodInfo GetMethodByContext(HttpListenerContext context)
{
    var sessionId = context.Request.Cookies["session-id"]?.Value;
    var role = "any";
    if (sessionId != null)
    {
        var session = store.StringGet(new RedisKey(sessionId));
        if (session.HasValue)
        {
            role = "any, user";
            //Todo: подумать может как то можно сохранять чтобы в кэш постоянно не лазить
            var userId = session.ToString();
            context.Request.Headers.Add("userId", userId); //как вариант - все равно после обработки запрос останется на стороне сервера (но это не точно)
        }
    }
    var routeName = context.Request.Url?.Segments[1].Replace("/", "");
        
    var method = methods.FirstOrDefault(x =>
    {
        return x.GetCustomAttributes(true).Any(y =>
            y is Mapping mapping && mapping.Method == context.Request.HttpMethod &&
            mapping.Route == routeName);
    });
    if (method != null && method.GetCustomAttributes(true).Any(x => x is Mapping mapping && !mapping.Role.Contains(role)))
    {
        context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
        context.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("Вы не авторизованы. Для доступа к этому ресурсу, нужно быть авторизованным как пользователь!"));
        return null;
    }
    return method == null ? defaultMethod : method;
}  