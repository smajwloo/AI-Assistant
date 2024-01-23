using System.Net;
using aia_api.Configuration.Records;
using InterfacesAia.Handlers;
using Microsoft.Extensions.Options;

namespace aia_api.Application.Handlers.FileHandler;

/// <summary>
/// Checks if file can be handled by any of the handlers.
/// </summary>
public class FileValidator : AbstractFileHandler
{
    private readonly string[] _contentType = { "application/zip" };

    public FileValidator(ILogger<FileValidator> logger, IOptions<Settings> settings) : base(logger, settings)
    { }

    public override async Task<IHandlerResult> Handle(string clientConnectionId, string inputPath, string inputContentType)
    {
        if (!File.Exists(inputPath))
        {
            return new HandlerResult
            {
                ErrorMessage = "The specified file does not exist.",
                Success = false,
                StatusCode = HttpStatusCode.BadRequest
            };
        }

        var fileInfo = new FileInfo(inputPath);

        if (fileInfo.Length == 0)
        {
            return new HandlerResult
            {
                ErrorMessage = "The file is empty.",
                Success = false,
                StatusCode = HttpStatusCode.BadRequest
            };
        }

        if (!_contentType.Contains(inputContentType))
        {
            return new HandlerResult
            {
                ErrorMessage = "Invalid file type.",
                Success = false,
                StatusCode = HttpStatusCode.BadRequest
            };
        }

        if (Next == null)
            return await base.Handle(clientConnectionId, inputPath, inputContentType);

        return await Next.Handle(clientConnectionId, inputPath, inputContentType);
    }

}
