using System.IO.Abstractions;
using aia_api.Configuration.Records;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;

namespace aia_api.Services;

public class AzureService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly IFileSystem _fileSystem;
    private readonly string _blobContainerName;

    public AzureService(BlobServiceClient blobClient, IOptions<AzureBlobStorageSettings> settings, IFileSystem fileSystem)
    {
        _blobContainerName = settings.Value.BlobContainerName;
        _blobServiceClient = blobClient;
        _fileSystem = fileSystem;
    }

    public async Task Pipeline(string path, string blobFileName)
    {
        var fileStream =
            _fileSystem.FileStream.New(Path.Combine(path, blobFileName), FileMode.Open, FileAccess.ReadWrite);
        await UploadStreamToBlob(fileStream, blobFileName);
    }

    public async Task PipeLine(MemoryStream stream, string blobFileName)
    {
        stream.Position = 0;
        await UploadStreamToBlob(stream, blobFileName);
    }

    private async Task UploadStreamToBlob(Stream inputStream, string blobFileName)
    {
        var blobClient = CreateBlobClients(blobFileName);

        byte[] buffer = new byte[4 * 1024];
        await using var outputStream = await blobClient.OpenWriteAsync(true);
        int bytesRead;

        while ((bytesRead = await inputStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            await outputStream.WriteAsync(buffer, 0, bytesRead);
        }
    }

    private BlobClient CreateBlobClients(string fileName)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_blobContainerName);
        BlobClient blobClient = containerClient.GetBlobClient(fileName);
        return blobClient;
    }
}
