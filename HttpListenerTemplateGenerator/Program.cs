using System.Net;
using System.Text;
using HttpListenerTemplateGenerator;

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
        switch (request.Url?.LocalPath)
        {
            case "/home":
                await GetHomePage(context, ctx.Token);
                break;
        }

        response.OutputStream.Close();
        response.Close();
    });
}
httpListener.Stop();
httpListener.Close();

return;


async Task GetHomePage(HttpListenerContext httpListenerContext, CancellationToken ctxToken)
{
    const string template = """
                                  <html>
                                       <head>
                                            <title>{{title}}</title>
                                       </head>
                                       <body>
                                            <h1>Welcome, {{name}}!</h1>
                                                <p>Your role is: {{role}}</p>
                                                <ul>
                                                    {{#foreach item in items}}
                                                        <li>{{item}}</li>
                                                    {{/foreach}}
                                                </ul>
                                                {{#if isAdmin}}
                                                    <p>Welcome, Admin!</p>
                                                {{/if}}
                                       </body>
                                   </html>
                            """;
    
    var data = new Dictionary<string, string>
    {
        { "title", "User Dashboard" },
        { "name", "Alice" },
        { "role", "Administrator" },
        {"isAdmin", "true" }
    };
    
    var result = TemplateGenerator.Render(template, data);
    httpListenerContext.Response.StatusCode = (int)HttpStatusCode.OK;
    httpListenerContext.Response.ContentType = "text/html";
    httpListenerContext.Response.ContentEncoding = Encoding.UTF8;
    
    await httpListenerContext.Response.OutputStream.WriteAsync(
        Encoding.UTF8.GetBytes(result), ctxToken);
}