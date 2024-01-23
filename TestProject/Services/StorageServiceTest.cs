using aia_api.Services;
using Microsoft.Extensions.Options;
using Moq;
using System.IO.Abstractions.TestingHelpers;
using aia_api.Configuration.Records;

namespace TestProject.Services;

public class StorageServiceTest
{

    [Test]
    public Task StoreInTemp_ThrowsExceptionOnUndefinedContent()
    {
        // Arrange
        var mockSettings = new Mock<IOptions<Settings>>();
        var mockFileSystem = new MockFileSystem();

        var storageService = new FileSystemStorageService(mockSettings.Object, mockFileSystem);

        // Act & Assert
        Assert.ThrowsAsync<NullReferenceException>(() =>
            storageService.StoreInTemp(new HttpResponseMessage(), "some_file_name"));

        // if no throw, test fails
        return Task.CompletedTask;
    }

    [Test]
    public async Task StoreInTemp_SavesFileInExpectedLocation()
    {
        // Arrange
        var settings = new Settings { TempFolderPath = "some_temp_path" };
        var mockFileSystem = new MockFileSystem();
        var service = new FileSystemStorageService(Options.Create(settings), mockFileSystem);

        // Act
        await service.StoreInTemp(new HttpResponseMessage
        {
            Content = new StringContent("fake_zip_data")
        }, "some_file_name");

        // Assert
        Assert.That(mockFileSystem.FileExists("some_temp_path/some_file_name"), Is.True);
    }

    [Test]
    public async Task StoreInTemp_CreatesDirectory_IfNotExists()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        var settings = new Settings { TempFolderPath = "c:/some/temp/path" };
        var options = Options.Create(settings);
        byte[] dummyData = { 0x01, 0x02, 0x03, 0x04 };

        var storageService = new FileSystemStorageService(options, fileSystem);

        // Act
        await storageService.StoreInTemp(new MemoryStream(dummyData), "somefile.zip");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(fileSystem.Directory.Exists(settings.TempFolderPath), Is.True);
            Assert.That(fileSystem.File.Exists("c:/some/temp/path/somefile.zip"), Is.True);
        });
    }

    [Test]
    public async Task StoreInTemp_SavesFileCorrectly()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var settings = new Settings { TempFolderPath = "some/temp/path" };
        var options = Options.Create(settings);

        var storageService = new FileSystemStorageService(options, mockFileSystem);

        byte[] dummyData = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var memoryStream = new MemoryStream(dummyData);

        // Act
        var fullPath = await storageService.StoreInTemp(memoryStream, "somefile.zip");

        // Assert
        byte[] savedData = await mockFileSystem.File.ReadAllBytesAsync(fullPath);

        Assert.Multiple(() =>
        {
            Assert.That(mockFileSystem.FileExists(fullPath), Is.True);
            Assert.That(savedData, Is.EqualTo(dummyData));
        });
    }
}
