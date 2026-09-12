using Inventory_System.Components;
using Inventory_System.Data;
using Inventory_System.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=localhost;Port=3306;Database=InventoryDb;User=root;Password=yourpassword;";

var mySqlVersion = builder.Configuration.GetSection("MySql")["Version"] ?? "10.4";
var serverType = builder.Configuration.GetSection("MySql")["ServerType"] ?? "MariaDb";

ServerVersion serverVersion = serverType.Equals("MariaDb", StringComparison.OrdinalIgnoreCase)
    ? new MariaDbServerVersion(new Version(mySqlVersion))
    : new MySqlServerVersion(new Version(mySqlVersion));

builder.Services.AddDbContextFactory<InventoryDbContext>(options =>
    options.UseMySql(connectionString, serverVersion));

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<MonitoringService>();
builder.Services.AddScoped<ToastService>();

if (builder.Environment.IsDevelopment())
{
    builder.WebHost.UseUrls(SelectStartupUrls());
}
else
{
    var port = ResolveStartupPort();
    if (port.Length > 0)
    {
        builder.WebHost.UseUrls("http://0.0.0.0:" + port);
    }
}

var app = builder.Build();

await DatabaseInitializer.InitializeAsync(app.Services);

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

static string ResolveStartupPort()
{
    var raw = Environment.GetEnvironmentVariable("PORT");
    if (raw == null || raw.Length == 0) return "8080";

    var parsed = 0;
    var digits = 0;
    for (var i = 0; i < raw.Length; i = i + 1)
    {
        var c = raw[i];
        if (c < '0' || c > '9') return "8080";
        parsed = parsed * 10 + (c - '0');
        digits = digits + 1;
        if (digits > 5) return "8080";
    }

    if (digits == 0 || parsed < 1 || parsed > 65535) return "8080";
    return parsed.ToString();
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