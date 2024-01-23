using System.Net;
using aia_api.Configuration.Records;
using aia_api.Routes.DTO;
using aia_api.Services;
using InterfacesAia.Handlers;
using InterfacesAia.Helpers;
using InterfacesAia.Services;
using Microsoft.Extensions.Options;

namespace aia_api.Routes;

public class UploadRouter
{
    public static Func<ILogger<UploadRouter>, IOptions<Settings>, IFormFile, HttpContext, IFileHandlerFactory, IFileSystemStorageService, Task> ZipHandler()
    {
        return async (logger, settings, compressedFile, context, fileHandlerFactory, storageService) =>
        {
            if (!settings.Value.SupportedContentTypes.Contains(compressedFile.ContentType))
            {
                context.Response.StatusCode = (int) HttpStatusCode.BadRequest;
                logger.LogError("Invalid file type. Only ZIP files are allowed.");
                return;
            }

            var fileName = compressedFile.FileName;
            var handlerStreet = fileHandlerFactory.GetFileHandler();

            try
            {
                Stream inputStream = compressedFile.OpenReadStream();
                var path = await storageService.StoreInTemp(inputStream, fileName);
                var result = await handlerStreet.Handle("", path, compressedFile.ContentType);

                context.Response.StatusCode = (int) result.StatusCode;

                if (!result.Success)
                    logger.LogError("Error: {errorMessage}", result.ErrorMessage);
            }
            catch (Exception e)
            {
                context.Response.StatusCode = (int) HttpStatusCode.BadRequest;
                logger.LogCritical("Exception: {message}, StackTrace: {stackTrace}", e.Message, e.StackTrace);
            }
        };
    }

    public static  Func<ILogger<UploadRouter>, UploadRepoDTO, HttpContext, GitlabService, IFileHandlerFactory, Task> RepoHandler()
    {
        return async (logger, dto, context, gitlabApi, fileHandlerFactory) =>
        {
            var projectId = dto.projectId;
            var apiToken = dto.apiToken;

            if (projectId.Length == 0 || string.IsNullOrWhiteSpace(projectId))
            {
                logger.LogError("Invalid project id. Provide a valid project id.");
                context.Response.StatusCode = (int) HttpStatusCode.BadRequest;
                return;
            }

            if (apiToken.Length == 0 || string.IsNullOrWhiteSpace(apiToken))
            {
                logger.LogError("Invalid api token. Configure api token.");
                context.Response.StatusCode = (int) HttpStatusCode.BadRequest;
                return;
            }

            try
            {
                var downloadPath = await gitlabApi.DownloadRepository(projectId, apiToken);
                IUploadedFileHandler handlerStreet = fileHandlerFactory.GetFileHandler();
                var result = await handlerStreet.Handle("", downloadPath, "application/zip");

                context.Response.StatusCode = (int) result.StatusCode;

                if (!result.Success)
                    logger.LogError("Error: {errorMessage}", result.ErrorMessage);
            }
            catch (Exception e)
            {
                context.Response.StatusCode = (int) HttpStatusCode.InternalServerError;
                logger.LogCritical("Exception: {message}, StackTrace: {stackTrace}", e.Message, e.StackTrace);
                return;
            }

            context.Response.StatusCode = (int) HttpStatusCode.NoContent;
        };
    }

}
