using Inventory_System.Components;
using Inventory_System.Data;
using Inventory_System.Services;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes
        .Append("application/octet-stream")
        .Append("application/json");
});

var connectionString = ResolveConnectionString(builder.Configuration, out var source);
Console.WriteLine("[Inventory System] Database source: " + source);

if (connectionString.IndexOf("Connection Timeout", StringComparison.OrdinalIgnoreCase) < 0)
{
    connectionString = connectionString.Trim().TrimEnd(';') + ";Connection Timeout=5;";
}

var mySqlVersion = builder.Configuration.GetSection("MySql")["Version"] ?? "10.4";
var serverType = builder.Configuration.GetSection("MySql")["ServerType"] ?? "MariaDb";

ServerVersion serverVersion = serverType.Equals("MariaDb", StringComparison.OrdinalIgnoreCase)
    ? new MariaDbServerVersion(new Version(mySqlVersion))
    : new MySqlServerVersion(new Version(mySqlVersion));

builder.Services.AddDbContextFactory<InventoryDbContext>(options =>
    options.UseMySql(connectionString, serverVersion,
        mySqlOptions => mySqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(2),
            errorNumbersToAdd: null)));

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

app.UseResponseCompression();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static string ResolveConnectionString(IConfiguration configuration, out string source)
{
    var configured = configuration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrWhiteSpace(configured))
    {
        source = "DefaultConnection (appsettings.json or ConnectionStrings__DefaultConnection)";
        return configured.Trim();
    }

    var databaseUrl = configuration["DATABASE_URL"];
    if (!string.IsNullOrWhiteSpace(databaseUrl))
    {
        source = "DATABASE_URL environment variable";
        return ConvertDatabaseUrl(databaseUrl.Trim());
    }

    source = "NOT CONFIGURED - using ephemeral fallback; database features are disabled until a connection string is provided";
    return "Server=127.0.0.1;Port=3306;Database=inventory_db;User=root;Password=;Connection Timeout=2;";
}

static string ConvertDatabaseUrl(string databaseUrl)
{
    var normalized = databaseUrl;
    if (normalized.StartsWith("mariadb://", StringComparison.OrdinalIgnoreCase))
    {
        normalized = "mysql://" + normalized.Substring("mariadb://".Length);
    }

    if (!normalized.StartsWith("mysql://", StringComparison.OrdinalIgnoreCase))
    {
        return normalized;
    }

    var uri = new Uri(normalized);
    var userInfo = uri.UserInfo;
    var user = string.Empty;
    var password = string.Empty;
    var separator = userInfo.IndexOf(':');
    if (separator >= 0)
    {
        user = userInfo.Substring(0, separator);
        password = userInfo.Substring(separator + 1);
    }
    else
    {
        user = userInfo;
    }

    var database = uri.AbsolutePath.Trim('/');
    if (database.Length == 0) database = "inventory_db";
    var port = uri.IsDefaultPort ? 3306 : uri.Port;

    return "Server=" + uri.Host
         + ";Port=" + port
         + ";Database=" + Uri.UnescapeDataString(database)
         + ";User=" + Uri.UnescapeDataString(user)
         + ";Password=" + Uri.UnescapeDataString(password) + ";";
}

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