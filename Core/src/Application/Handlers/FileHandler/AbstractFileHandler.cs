using System.Net;
using aia_api.Configuration.Records;
using InterfacesAia.Handlers;
using Microsoft.Extensions.Options;

namespace aia_api.Application.Handlers.FileHandler;

public abstract class AbstractFileHandler : IUploadedFileHandler
{
    protected IUploadedFileHandler Next;
    protected Dictionary<string, int> ExtensionsCount = new();
    private readonly ILogger<AbstractFileHandler> _logger;
    private readonly Settings _supportedContentTypes;

    protected AbstractFileHandler(ILogger<AbstractFileHandler> logger, IOptions<Settings> settings)
    {
        _logger = logger;
        _supportedContentTypes = settings.Value;
    }

    public virtual Task<IHandlerResult> Handle(string clientConnectionId, string inputPath, string inputContentType)
    {
        _logger.LogInformation("Input path: {path}", inputPath);
        _logger.LogInformation("Input content type: {contentType}", inputContentType);

        var result = new HandlerResult
        {
            ErrorMessage = "No handler found.",
            Success = false,
            StatusCode = HttpStatusCode.NotImplemented
        };

        return Task.FromResult<IHandlerResult>(result);
    }

    public void SetNext(IUploadedFileHandler next)
    {
        Next = next;
    }

    protected bool IsValidFile(string inputContentType, string contentType)
    {
        if (inputContentType == contentType) return true;

        return false;
    }

    protected bool IsValidFile(string inputContentType, string[] contentTypes)
    {
        return contentTypes.Any(c => inputContentType == c);
    }

    protected void CountExtension(string extension)
    {
        if (!ExtensionsCount.ContainsKey(extension))
            ExtensionsCount[extension] = 1;
        else
            ExtensionsCount[extension]++;
    }

    protected bool IsSupportedExtension(string extension)
    {
        var extensions = _supportedContentTypes.AllowedFileTypes;
        return extensions.Contains(extension);
    }

    protected void LogExtensionsCount()
    {
        var logs = string.Join("\n", ExtensionsCount.Select(x => $"{x.Key}: {x.Value}"));
        _logger.LogInformation(logs);
        ExtensionsCount = new();
    }
}
