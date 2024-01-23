using aia_api.Application.EndpointFilter;
using aia_api.Configuration;
using aia_api.Database;
using aia_api.Routes;
using InterfacesAia.Handlers;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Http.Features;
using InterfacesAia.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.SetupDi(builder.Configuration);
builder.Services.AddSignalR();
builder.WebHost.UseKestrel(o => o.Limits.MaxRequestBodySize = long.MaxValue);
builder.Services.Configure<FormOptions>(x =>
{
    x.MultipartBodyLengthLimit = int.MaxValue;
});

var app = builder.Build();

IUploadHandler uploadHandler = app.Services.GetRequiredService<IUploadHandler>();
IServiceBusService serviceBusService = app.Services.GetRequiredService<IServiceBusService>();
HubConnection connection = await serviceBusService.ExecuteAsync();

connection.On<string, string, string, byte[], int, int>("UploadChunk", uploadHandler.ReceiveFileChunk);

var api = app.MapGroup("/api");
var db = app.MapGroup("/db");


api.MapPost("/upload/zip", UploadRouter.ZipHandler())
    .AddEndpointFilter<EmptyFileFilter>();

api.MapPost("/upload/repo", UploadRouter.RepoHandler());

api.MapGet("/health", () => Results.Ok("OK"));

db.MapDelete("/clear-db", (PredictionDbContext dbContext) =>
{
    var entitiesToRemove = dbContext.Predictions.ToList();

    foreach (var entity in entitiesToRemove)
        dbContext.Remove(entity);

    dbContext.SaveChanges();

    return Results.Ok("Database cleared successfully.");
});


app.Run();
