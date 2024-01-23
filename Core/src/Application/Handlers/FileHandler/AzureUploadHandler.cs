using aia_api.Configuration.Records;
using aia_api.Services;
using InterfacesAia.Handlers;
using Microsoft.Extensions.Options;

namespace aia_api.Application.Handlers.FileHandler;

public class AzureUploadHandler : AbstractFileHandler
{
    private readonly ILogger<AzureUploadHandler> _logger;
    private readonly IOptions<Settings> _settings;
    private readonly AzureService _azureService;

    public AzureUploadHandler(ILogger<AzureUploadHandler> logger, IOptions<Settings> settings, AzureService azureService) : base(logger, settings)
    {
        _logger = logger;
        _settings = settings;
        _azureService = azureService;
    }

    public override async Task<IHandlerResult> Handle(string clientConnectionId, string inputPath, string inputContentType)
    {
        try
        {
            await _azureService.Pipeline(_settings.Value.TempFolderPath + "Output/", Path.GetFileName(inputPath));
            _logger.LogInformation("File successfully uploaded to Azure.");
        }
        catch (IOException e)
        {
            _logger.LogCritical("Something went wrong while reading or writing: {message}, {stackTrace}", e.Message, e.StackTrace);
        }
        catch (Exception e)
        {
            _logger.LogCritical("An unexpected error occurred: {message}, {stackTrace}", e.Message, e.StackTrace);
            throw;
        }

        if (Next == null)
            return await base.Handle(clientConnectionId, inputPath, inputContentType);

        return await Next.Handle(clientConnectionId, inputPath, inputContentType);
    }

}
