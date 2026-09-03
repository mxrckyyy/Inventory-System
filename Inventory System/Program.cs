using Inventory_System.Components;
using Inventory_System.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<AuthService>();
builder.Services.AddSingleton<InventoryService>();
builder.Services.AddSingleton<CategoryService>();
builder.Services.AddSingleton<ToastService>();

if (builder.Environment.IsDevelopment())
{
    builder.WebHost.UseUrls(SelectStartupUrls());
}

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static string[] SelectStartupUrls()
{
    var httpPort = 5180;
    var httpsPort = 7180;

    if (IsPortInUse(httpPort) || IsPortInUse(httpsPort))
    {
        httpPort = GetAvailablePort();
        httpsPort = GetAvailablePort(httpPort);
    }

    return new[] { $"http://localhost:{httpPort}", $"https://localhost:{httpsPort}" };
}

static bool IsPortInUse(int port)
{
    try
    {
        var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, port);
        listener.Start();
        listener.Stop();
        return false;
    }
    catch
    {
        return true;
    }
}

static int GetAvailablePort(int exclude = 0)
{
    var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
    listener.Start();
    var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
    listener.Stop();

    if (exclude > 0 && port == exclude)
    {
        return GetAvailablePort(exclude);
    }

    return port;
}
