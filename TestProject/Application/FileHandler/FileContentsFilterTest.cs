using System.IO.Abstractions.TestingHelpers;
using System.IO.Compression;
using aia_api.Application.Handlers.FileHandler;
using aia_api.Application.Helpers;
using aia_api.Configuration.Records;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace TestProject.Application.FileHandler;

public class FileContentsFilterTest
{
    private Mock<IOptions<Settings>> _settingsMock;
    private Mock<ILogger<FileContentsFilter>> _loggerMock;
    private List<string> _codeSnippets;
    private List<string> _commentsList;
    private List<string> _eslintList;
    private string _clientConnectionId;

    [SetUp]
    public void SetUp()
    {
        _settingsMock = new Mock<IOptions<Settings>>();
        _settingsMock.Setup(s => s.Value).Returns(new Settings
        {
            TempFolderPath = "some/temp/",
            AllowedFileTypes = new []{ ".ts"},
            SupportedContentTypes = new []{ "application/zip" }
        });
        _loggerMock = new Mock<ILogger<FileContentsFilter>>();

        _codeSnippets = File.ReadAllText("./Testfiles/_code_snippets.txt").Split(new[] { "```" }, StringSplitOptions.RemoveEmptyEntries).Where(snippet => !string.IsNullOrWhiteSpace(snippet)).ToList();
        var commentsContent = File.ReadAllText("./Testfiles/_comments.txt");
        var eslintComments = File.ReadAllText("./Testfiles/_eslint_comments.txt");

        _commentsList = commentsContent.Split(new[] { "---" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(c => c.Trim())
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .ToList();

        _eslintList = eslintComments.Split(new[] { "---" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(c => c.Trim())
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .ToList();
        _clientConnectionId = Guid.NewGuid().ToString();
    }

    [Test]
    public async Task Handle_ProcessesZipFileCorrectly()
    {
        // Arrange
        var mockFs = new MockFileSystem();

        byte[] zipData;
        using (MemoryStream ms = new MemoryStream())
        {
            using (ZipArchive archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
            {
                ZipArchiveEntry entry = archive.CreateEntry("test.txt");
                using (StreamWriter writer = new StreamWriter(entry.Open()))
                {
                    writer.Write("Test content");
                }
            }
            zipData = ms.ToArray();
        }

        mockFs.AddFile("somefile.zip", new MockFileData(zipData));

        var abstractFileHandlerLoggerMock = new Mock<ILogger<AbstractFileHandler>>();
        var commentCheckerLoggerMock = new Mock<ILogger<CommentChecker>>();
        var commentChecker = new CommentChecker(commentCheckerLoggerMock.Object);
        var zipHandler = new FileContentsFilter(_loggerMock.Object, _settingsMock.Object, mockFs, commentChecker);
        var nextHandlerMock = new Mock<AbstractFileHandler>(abstractFileHandlerLoggerMock.Object, _settingsMock.Object);
        zipHandler.SetNext(nextHandlerMock.Object);

        // Act
        await zipHandler.Handle(_clientConnectionId, "somefile.zip", "application/zip");

        // Assert
        nextHandlerMock.Verify(x => x.Handle(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task Handle_ForwardsToNextHandler_WhenNotZip()
    {
        // Arrange
        var abstractFileHandlerLoggerMock = new Mock<ILogger<AbstractFileHandler>>();
        var commentCheckerLoggerMock = new Mock<ILogger<CommentChecker>>();
        var commentChecker = new CommentChecker(commentCheckerLoggerMock.Object);
        var zipHandler = new FileContentsFilter(_loggerMock.Object, _settingsMock.Object, new MockFileSystem(), commentChecker);
        var nextHandlerMock = new Mock<AbstractFileHandler>(abstractFileHandlerLoggerMock.Object, _settingsMock.Object);
        zipHandler.SetNext(nextHandlerMock.Object);

        // Act
        await zipHandler.Handle(_clientConnectionId, "somefile.txt", "text/plain");

        // Assert
        nextHandlerMock.Verify(x => x.Handle(It.IsAny<string>(), "somefile.txt", "text/plain"), Times.Once);
    }

    [Test]
    public void Handle_ShouldNotThrow_WhenNoNextHandler()
    {
        // Arrange
        var commentCheckerLoggerMock = new Mock<ILogger<CommentChecker>>();
        var commentChecker = new CommentChecker(commentCheckerLoggerMock.Object);
        var zipHandler = new FileContentsFilter(_loggerMock.Object, _settingsMock.Object, new MockFileSystem(), commentChecker);

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await zipHandler.Handle(_clientConnectionId, "somefile.txt", "text/plain"));
    }

    [Test]
    public async Task Filter_ShouldFind_AllComments()
    {
        // Arrange
        var mockFs = new MockFileSystem();

        var combinedCodeComments = new List<string>();
        foreach (var comment in _commentsList)
        {
            var random = new Random();
            var codeSnippet = _codeSnippets[random.Next(0, _codeSnippets.Count - 1)];
            combinedCodeComments.Add($"{comment}{codeSnippet}");
        }

        byte[] zipData;
        using (var ms = new MemoryStream())
        {
            using (ZipArchive archiveBase = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
            {
                for (int i = 0; i < combinedCodeComments.Count; i++)
                {
                    var entry = archiveBase.CreateEntry($"file{i}.ts");
                    using StreamWriter writer = new StreamWriter(entry.Open());
                    writer.Write(combinedCodeComments[i]);
                }
            }
            zipData = ms.ToArray();
        }

        mockFs.AddFile("/temp/somefile.zip", new MockFileData(zipData));
        var commentCheckerLoggerMock = new Mock<ILogger<CommentChecker>>();
        var commentChecker = new CommentChecker(commentCheckerLoggerMock.Object);
        var fileContentsFilter = new FileContentsFilter(_loggerMock.Object, _settingsMock.Object, mockFs, commentChecker);

        // Act
        await fileContentsFilter.Handle(_clientConnectionId, "/temp/somefile.zip", "application/zip");

        // Assert
        var fileExists = mockFs.FileExists("/some/temp/Output/somefile.zip");
        Assert.That(fileExists, Is.True);

        var filteredBytes = await mockFs.File.ReadAllBytesAsync("/some/temp/Output/somefile.zip");
        var originalBytes = await mockFs.File.ReadAllBytesAsync("/temp/somefile.zip");
        using var filteredZipStream = new MemoryStream(filteredBytes);
        using ZipArchive filteredArchive = new ZipArchive(filteredZipStream, ZipArchiveMode.Read);
        using var originalZipStream = new MemoryStream(originalBytes);
        using ZipArchive originalArchive = new ZipArchive(originalZipStream, ZipArchiveMode.Read);

        Assert.That(filteredArchive.Entries, Has.Count.EqualTo(originalArchive.Entries.Count));
    }

    [Test]
    public async Task Filter_ShouldIgnore_EslintComments()
    {
        // Arrange
        var mockFs = new MockFileSystem();
        // filename, content
        var combinedCodeComments = new List<Tuple<string, string>>();
        var eslintCommentCount = 0;
        var combinationCount = 20;

        for (var i = 0; i < combinationCount; i++)
        {
            var random = new Random();
            var randomCodeSnippet = _codeSnippets[random.Next(0, _codeSnippets.Count - 1)];
            var randomTrue = random.Next(0, 6) >= 3;
            if (randomTrue)
            {
                var eslintSnippet = _eslintList[random.Next(0, _eslintList.Count - 1)];
                var tuple = Tuple.Create($"eslint-file{i}.ts", $"{eslintSnippet}{randomCodeSnippet}");
                combinedCodeComments.Add(tuple);
                eslintCommentCount++;
            }
            else
            {
                var comment = _commentsList[random.Next(0, _commentsList.Count - 1)];
                var tuple = Tuple.Create($"file{i}.ts", $"{comment}{randomCodeSnippet}");
                combinedCodeComments.Add(tuple);
            }
        }

        byte[] zipData;
        using (var ms = new MemoryStream())
        {
            using (ZipArchive archiveBase = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
            {
                for (int i = 0; i < combinedCodeComments.Count; i++)
                {
                    var entry = archiveBase.CreateEntry(combinedCodeComments[i].Item1);
                    using StreamWriter writer = new StreamWriter(entry.Open());
                    writer.Write(combinedCodeComments[i].Item2);
                }
            }
            zipData = ms.ToArray();
        }

        mockFs.AddFile("/temp/somefile.zip", new MockFileData(zipData));
        var commentCheckerLoggerMock = new Mock<ILogger<CommentChecker>>();
        var commentChecker = new CommentChecker(commentCheckerLoggerMock.Object);
        var fileContentsFilter = new FileContentsFilter(_loggerMock.Object, _settingsMock.Object, mockFs, commentChecker);

        // Act
        await fileContentsFilter.Handle(_clientConnectionId, "/temp/somefile.zip", "application/zip");

        // Assert
        var fileExists = mockFs.FileExists("/some/temp/Output/somefile.zip");
        Assert.That(fileExists, Is.True);

        var filteredBytes = await mockFs.File.ReadAllBytesAsync("/some/temp/Output/somefile.zip");
        using var filteredZipStream = new MemoryStream(filteredBytes);
        using ZipArchive filteredArchive = new ZipArchive(filteredZipStream, ZipArchiveMode.Read);

        Assert.That(filteredArchive.Entries, Has.Count.Not.EqualTo(combinationCount));
        Assert.That(filteredArchive.Entries, Has.Count.EqualTo(combinationCount - eslintCommentCount));
    }

    [Test]
    public async Task Filter_ShouldFind_InlineComments()
    {
        // Arrange
        var mockFs = new MockFileSystem();
        var combinedCodeComments = new List<Tuple<string, string>>(); // Tuple<filename, content>
        const int combinationCount = 20;
        var random = new Random();
    
        for (var i = 0; i < combinationCount; i++)
        {
            var randomCodeSnippet = _codeSnippets[random.Next(0, _codeSnippets.Count - 1)];
            var randomComment = _commentsList[random.Next(40, _commentsList.Count - 1)];
            
            if (randomComment == string.Empty) throw new Exception("Empty comment");
            
            var codeWithComment = $"{randomComment}{randomCodeSnippet}";
            var tuple = Tuple.Create($"file{i}.ts", codeWithComment);
            combinedCodeComments.Add(tuple);
        }

        byte[] zipData;
        using (var ms = new MemoryStream())
        {
            using (ZipArchive archiveBase = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
            {
                for (int i = 0; i < combinedCodeComments.Count; i++)
                {
                    var entry = archiveBase.CreateEntry(combinedCodeComments[i].Item1);
                    using StreamWriter writer = new StreamWriter(entry.Open());
                    writer.Write(combinedCodeComments[i].Item2);
                }
            }
            zipData = ms.ToArray();
        }

        mockFs.AddFile("/temp/somefile.zip", new MockFileData(zipData));
        var commentCheckerLoggerMock = new Mock<ILogger<CommentChecker>>();
        var commentChecker = new CommentChecker(commentCheckerLoggerMock.Object);
        var fileContentsFilter = new FileContentsFilter(_loggerMock.Object, _settingsMock.Object, mockFs, commentChecker);

        // Act
        await fileContentsFilter.Handle(_clientConnectionId, "/temp/somefile.zip", "application/zip");

        // Assert
        var fileExists = mockFs.FileExists("/some/temp/Output/somefile.zip");
        Assert.That(fileExists, Is.True);

        var filteredBytes = await mockFs.File.ReadAllBytesAsync("/some/temp/Output/somefile.zip");
        using var filteredZipStream = new MemoryStream(filteredBytes);
        using ZipArchive filteredArchive = new ZipArchive(filteredZipStream, ZipArchiveMode.Read);

        Assert.That(filteredArchive.Entries, Has.Count.EqualTo(combinationCount));
    }
}
