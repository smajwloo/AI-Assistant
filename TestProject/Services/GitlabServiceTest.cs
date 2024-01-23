using System.Net;
using aia_api.Services;
using InterfacesAia.Services;
using Moq;
using Moq.Protected;

namespace TestProject.Services;

public class GitlabServiceTest
{
    [Test]
    public void DownloadRepository_ThrowsExceptionOnFailedDownload()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError
            });


        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        httpClient.BaseAddress = new Uri("https://gitlab.com");
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var mockStorageService = new Mock<IFileSystemStorageService>();
        var service = new GitlabService(httpClientFactory.Object, mockStorageService.Object);

        // Act & Assert
        Assert.ThrowsAsync<Exception>(() => service.DownloadRepository("some_project_id", "some_api_token"));
    }

    [Test]
    public async Task DownloadRepository_CallsStorageServiceWithExpectedArguments_OnSuccessfulDownload()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        httpClient.BaseAddress = new Uri("https://gitlab.com");
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var mockStorageService = new Mock<IFileSystemStorageService>();

        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("fake_zip_data")
            });

        var service = new GitlabService(httpClientFactory.Object, mockStorageService.Object);

        // Act
        await service.DownloadRepository("some_project_id", "some_api_token");

        // Assert
        mockStorageService.Verify(x => x.StoreInTemp(It.IsAny<HttpResponseMessage>(),
            It.Is<string>(s => s.Contains("some_project_id"))), Times.Once);
    }


}
