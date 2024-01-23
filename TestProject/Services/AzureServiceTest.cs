using System.IO.Abstractions.TestingHelpers;
using System.Text;
using aia_api.Configuration.Records;
using aia_api.Services;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;
using Moq;

namespace TestProject.Services;

[TestFixture]
public class AzureServiceTest
{
    private Mock<BlobServiceClient> _blobServiceClientMock;
    private Mock<IOptions<AzureBlobStorageSettings>> _settingsMock;
    private AzureService _azureService;
    private MockFileSystem _mockFileSystem;

    [SetUp]
    public void Setup()
    {
        _blobServiceClientMock = new Mock<BlobServiceClient>();
        _settingsMock = new Mock<IOptions<AzureBlobStorageSettings>>();
        _settingsMock.Setup(s => s.Value).Returns(new AzureBlobStorageSettings { BlobContainerName = "testContainer" });
        _mockFileSystem = new MockFileSystem();

        _azureService = new AzureService(_blobServiceClientMock.Object, _settingsMock.Object, _mockFileSystem);
    }

    [Test]
    public async Task Pipeline_ShouldCallBlobClientMethods()
    {
        // Arrange
        var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes("Test content"));
        var fileName = "testFile.zip";
        var blobContainerName = "testContainer";

        var blobWriteStreamMock = new Mock<Stream>();
        var blobContainerClientMock = new Mock<BlobContainerClient>();
        var blobClientMock = new Mock<BlobClient>();
        _blobServiceClientMock.Setup(x => x.GetBlobContainerClient(blobContainerName))
            .Returns(blobContainerClientMock.Object);
        blobContainerClientMock.Setup(x => x.GetBlobClient(fileName)).Returns(blobClientMock.Object);
        blobClientMock.Setup(x => x.OpenWriteAsync(true, default, default)).ReturnsAsync(blobWriteStreamMock.Object);

        // Act
        await _azureService.PipeLine(memoryStream, fileName);

        // Assert
        _blobServiceClientMock.Verify(x => x.GetBlobContainerClient(blobContainerName), Times.Once);
        blobClientMock.Verify(x => x.OpenWriteAsync( true, default, default), Times.Once);
    }

    [Test]
    public async Task Pipeline_ReadsFromStream()
    {
        // Arrange
        var memoryStream = new Mock<MemoryStream>();
        var fileName = "testFile.zip";
        var blobContainerName = "testContainer";
        var blobWriteStreamMock = new Mock<Stream>();
        var blobContainerClientMock = new Mock<BlobContainerClient>();
        var blobClientMock = new Mock<BlobClient>();

        memoryStream.Setup(x => x.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), default))
            .ReturnsAsync(0);

        _blobServiceClientMock.Setup(x => x.GetBlobContainerClient(blobContainerName))
            .Returns(blobContainerClientMock.Object);
        blobContainerClientMock.Setup(x => x.GetBlobClient(fileName)).Returns(blobClientMock.Object);
        blobClientMock.Setup(x => x.OpenWriteAsync(true, default, default)).ReturnsAsync(blobWriteStreamMock.Object);

        // Act
        await _azureService.PipeLine(memoryStream.Object, fileName);

        // Assert
        memoryStream.Verify(x => x.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), default), Times.AtLeastOnce);
    }

    [Test]
    public async Task Pipeline_WriteToBlobClientOutputStream()
    {
        // Arrange
        var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes("Test content"));

        var fileName = "testFile.zip";
        var blobContainerName = "testContainer";
        var blobWriteStreamMock = new Mock<Stream>();
        var blobContainerClientMock = new Mock<BlobContainerClient>();
        var blobClientMock = new Mock<BlobClient>();

        _blobServiceClientMock.Setup(x => x.GetBlobContainerClient(blobContainerName))
            .Returns(blobContainerClientMock.Object);
        blobContainerClientMock.Setup(x => x.GetBlobClient(fileName)).Returns(blobClientMock.Object);
        blobClientMock.Setup(x => x.OpenWriteAsync(true, default, default)).ReturnsAsync(blobWriteStreamMock.Object);

        // Act
        await _azureService.PipeLine(memoryStream, fileName);

        // Assert
        blobWriteStreamMock.Verify(x => x.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), default), Times.AtLeastOnce);
    }


    [Test]
    public async Task Pipeline_AsyncTimesThree()
    {
        // Arrange
        var memoryStream1 = new MemoryStream(Encoding.UTF8.GetBytes("Test content"));
        var memoryStream2 = new MemoryStream(Encoding.UTF8.GetBytes("Test content"));
        var memoryStream3 = new MemoryStream(Encoding.UTF8.GetBytes("Test content"));
        var fileName = "testFile.zip";
        var blobContainerName = "testContainer";

        var blobWriteStreamMock = new Mock<Stream>();
        var blobContainerClientMock = new Mock<BlobContainerClient>();
        var blobClientMock = new Mock<BlobClient>();
        _blobServiceClientMock.Setup(x => x.GetBlobContainerClient(blobContainerName))
            .Returns(blobContainerClientMock.Object);
        blobContainerClientMock.Setup(x => x.GetBlobClient(fileName)).Returns(blobClientMock.Object);
        blobClientMock.Setup(x => x.OpenWriteAsync(true, default, default)).ReturnsAsync(blobWriteStreamMock.Object);

        // Act
        var task1 = _azureService.PipeLine(memoryStream1, fileName);
        var task2 = _azureService.PipeLine(memoryStream2, fileName);
        var task3 = _azureService.PipeLine(memoryStream3, fileName);

        await Task.WhenAll(task1, task2, task3);

        // Assert
        _blobServiceClientMock.Verify(x => x.GetBlobContainerClient(blobContainerName), Times.Exactly(3));
        blobClientMock.Verify(x => x.OpenWriteAsync( true, default, default), Times.Exactly(3));
    }


}
