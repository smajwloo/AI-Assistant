using System.IO.Abstractions;
using aia_api.Configuration.Records;
using InterfacesAia.Services;
using Microsoft.Extensions.Options;

namespace aia_api.Services;

public class FileSystemStorageService : IFileSystemStorageService
{
    private readonly IOptions<Settings> _settings;
    private readonly IFileSystem _fileSystem;
    private readonly IFileStreamFactory _fileStreamFactory;

    public FileSystemStorageService(IOptions<Settings> settings, IFileSystem fileSystem)
    {
        _settings = settings;
        _fileSystem = fileSystem;
        _fileStreamFactory = fileSystem.FileStream;
    }

    public async Task<string> StoreInTemp(HttpResponseMessage input, string fileName)
    {
        Stream responseStream = await input.Content.ReadAsStreamAsync();
        return await StoreInTemp(responseStream, fileName);
    }

    public async Task<string> StoreInTemp(Stream input, string fileName)
    {
        var directoryPath = _settings.Value.TempFolderPath;
        var fullPath = Path.Combine(directoryPath, fileName);

        if (!_fileSystem.Directory.Exists(directoryPath))
            _fileSystem.Directory.CreateDirectory(directoryPath);

        await using var fileStream = _fileStreamFactory.New(fullPath, FileMode.Create);
        input.Seek(0, SeekOrigin.Begin);
        await input.CopyToAsync(fileStream);
        return fullPath;
    }


}
