using System.IO.Abstractions;
using System.IO.Compression;
using aia_api.Application.Helpers;
using aia_api.Configuration.Records;
using InterfacesAia.Handlers;
using Microsoft.Extensions.Options;

namespace aia_api.Application.Handlers.FileHandler;

/// <summary>
/// Filters out files that do not contain comments.
/// Filters out files that are not supported.
/// </summary>
public class FileContentsFilter : AbstractFileHandler
{
    private readonly Settings _settings;
    private readonly ILogger<FileContentsFilter> _logger;
    private readonly IFileSystem _fileSystem;
    private readonly CommentChecker _commentChecker;

    public FileContentsFilter(
        ILogger<FileContentsFilter> logger,
        IOptions<Settings> settings,
        IFileSystem fileSystem,
        CommentChecker commentChecker
    ) : base(logger, settings)
    {
        _settings = settings.Value;
        _logger = logger;
        _fileSystem = fileSystem;
        _commentChecker = commentChecker;
    }

    public override async Task<IHandlerResult> Handle(string clientConnectionId, string inputPath, string inputContentType)
    {
        if (!IsValidFile(inputContentType, _settings.SupportedContentTypes))
        {
            if (Next != null)
                return await Next.Handle(clientConnectionId, inputPath,  inputContentType);
            return await base.Handle(clientConnectionId, inputPath, inputContentType);
        }

        using ZipArchive archive = InitializeInputArchive(inputPath);
        using ZipArchive outputArchive =
            InitializeOutputArchive(_fileSystem.Path.Combine(_settings.TempFolderPath + "Output/", _fileSystem.Path.GetFileName(inputPath)));

        await FilterEntries(archive, outputArchive);

        if (EnvHelper.IsDev())
            LogExtensionsCount();

        outputArchive.Dispose();
        archive.Dispose();

        if (Next == null)
            return await base.Handle(clientConnectionId, inputPath, inputContentType);
        return await Next.Handle(clientConnectionId, inputPath, inputContentType);
    }

    private ZipArchive InitializeInputArchive(string path)
    {
        var fileStream = _fileSystem.FileStream.New(path, FileMode.Open, FileAccess.Read);
        return new ZipArchive(fileStream, ZipArchiveMode.Read);
    }

    private ZipArchive InitializeOutputArchive(string outputPath)
    {
        if (!_fileSystem.Directory.Exists(_settings.TempFolderPath + "Output/"))
            _fileSystem.Directory.CreateDirectory(_settings.TempFolderPath + "Output/");

        var fs = _fileSystem.FileStream.New(outputPath, FileMode.Create, FileAccess.Write);
        return new ZipArchive(fs, ZipArchiveMode.Create);
    }

    private async Task FilterEntries(ZipArchive archive, ZipArchive outputArchive)
    {
        foreach (var entry in archive.Entries)
        {
            var extension = _fileSystem.Path.GetExtension(entry.FullName);
            if (string.IsNullOrEmpty(extension)) continue;

            CountExtension(extension);

            if (IsSupportedExtension(extension) && _commentChecker.HasComments(entry, extension))
            {
                _logger.LogInformation("Filtering {entry}...", entry.FullName);
                await CopyEntryToNewArchive(entry, outputArchive);
            }
        }
        _commentChecker.LogLogsAndClear();
    }

    private async Task CopyEntryToNewArchive(ZipArchiveEntry entry, ZipArchive outputArchive)
    {
        var newEntry = outputArchive.CreateEntry(entry.FullName);
        await using var originalStream = entry.Open();
        await using var newStream = newEntry.Open();
        await originalStream.CopyToAsync(newStream);
    }
}
