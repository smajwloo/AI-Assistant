using SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.MaximumReceiveMessageSize = 1000 * 1000 * 100; // Max 100 MB
});

var app = builder.Build();
app.MapHub<MainHub>("/uploadZip", options =>
{
    options.ApplicationMaxBufferSize = 0; // No Maximum
    options.TransportMaxBufferSize = 0; // No Maximum
});

app.Run();