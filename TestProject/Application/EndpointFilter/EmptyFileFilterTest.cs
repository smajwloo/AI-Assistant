using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using aia_api.Application.EndpointFilter;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;

namespace TestProject.Application.EndpointFilter;

[TestFixture]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public class EmptyFileFilterTest
{
    private Mock<ILogger<EmptyFileFilter>> _logger;
    private Mock<EndpointFilterInvocationContext> _context;
    private Mock<EndpointFilterDelegate> _next;

    [SetUp]
    public void Setup()
    {
        _logger = new Mock<ILogger<EmptyFileFilter>>();
        _context = new Mock<EndpointFilterInvocationContext>();
        _next = new Mock<EndpointFilterDelegate>();
    }

    [Test]
    public async Task InvokeEmptyFileFilter_ShouldReturn400_WhenContentZero()
    {
        // Arrange
        var requestMock = new Mock<HttpRequest>();
        var responseMock = new Mock<HttpResponse>();
        var expectedMessage = "No file received or file is empty.";
        var memoryStream = new MemoryStream();
        var pipeWriter = PipeWriter.Create(memoryStream);
        var formCollection = new FormCollection(new Dictionary<string, StringValues>(), new FormFileCollection());

        _context.Setup(x => x.HttpContext).Returns(new DefaultHttpContext());
        _context.Setup(x => x.HttpContext.Request).Returns(requestMock.Object);
        _context.Setup(x => x.HttpContext.Response).Returns(responseMock.Object);
        requestMock.SetupGet(x => x.ContentLength).Returns(0);
        responseMock.SetupGet(x => x.BodyWriter).Returns(pipeWriter);
        requestMock.Setup(x => x.ReadFormAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<IFormCollection>(formCollection));

        // Act
        var result = await new EmptyFileFilter(_logger.Object).InvokeAsync(_context.Object, _next.Object);

        // Assert
        memoryStream.Position = 0;
        var reader = new StreamReader(memoryStream);
        var capturedMessage = await reader.ReadToEndAsync();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(null));
            Assert.That(capturedMessage, Is.EqualTo(expectedMessage));
        });
    }

    [Test]
    public async Task InvokeEmptyFileFilter_ShouldReturn400_WhenFileIsNull()
    {
        // Arrange
        var requestMock = new Mock<HttpRequest>();
        var responseMock = new Mock<HttpResponse>();
        var expectedMessage = "No file received or file is empty.";
        var memoryStream = new MemoryStream();
        var pipeWriter = PipeWriter.Create(memoryStream);
        IFormCollection formCollection = new FormCollection(new Dictionary<string, StringValues>(), new FormFileCollection());


        _context.Setup(x => x.HttpContext).Returns(new DefaultHttpContext());
        _context.Setup(x => x.HttpContext.Request).Returns(requestMock.Object);
        _context.Setup(x => x.HttpContext.Response).Returns(responseMock.Object);
        requestMock.SetupGet(x => x.ContentLength).Returns(1);
        responseMock.SetupGet(x => x.BodyWriter).Returns(pipeWriter);
        requestMock.Setup(x => x.ReadFormAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<IFormCollection>(formCollection));

        // Act
        var result = await new EmptyFileFilter(_logger.Object).InvokeAsync(_context.Object, _next.Object);

        // Assert
        memoryStream.Position = 0;
        var reader = new StreamReader(memoryStream);
        var capturedMessage = await reader.ReadToEndAsync();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(null));
            Assert.That(capturedMessage, Is.EqualTo(expectedMessage));
        });
    }

    [Test]
    public async Task InvokeAsync_ShouldCallNext_WhenFileIsNotNull()
    {
        // Arrange
        var expectedValue = new object();
        var nextMock = new Mock<EndpointFilterDelegate>();
        nextMock.Setup(x => x(It.IsAny<EndpointFilterInvocationContext>()))
            .Returns(new ValueTask<object?>(expectedValue));
        var requestMock = new Mock<HttpRequest>();
        var responseMock = new Mock<HttpResponse>();
        var memoryStream = new MemoryStream();
        var pipeWriter = PipeWriter.Create(memoryStream);
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(x => x.Length).Returns(1);
        IFormCollection formCollection = new FormCollection(new Dictionary<string, StringValues>(), new FormFileCollection { fileMock.Object });

        _context.Setup(x => x.HttpContext).Returns(new DefaultHttpContext());
        _context.Setup(x => x.HttpContext.Request).Returns(requestMock.Object);
        _context.Setup(x => x.HttpContext.Response).Returns(responseMock.Object);
        requestMock.SetupGet(x => x.ContentLength).Returns(1);
        responseMock.SetupGet(x => x.BodyWriter).Returns(pipeWriter);
        requestMock.Setup(x => x.ReadFormAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<IFormCollection>(formCollection));

        // Act
        var result = await new EmptyFileFilter(_logger.Object).InvokeAsync(_context.Object, nextMock.Object);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(expectedValue));
            nextMock.Verify(x => x(It.IsAny<EndpointFilterInvocationContext>()), Times.Once);
        });
    }

    [Test]
    public async Task InvokeAsync_ShouldHandleException_WhenExceptionThrown()
    {
        // Arrange
        var requestMock = new Mock<HttpRequest>();
        var responseMock = new Mock<HttpResponse>();

        _context.Setup(x => x.HttpContext).Returns(new DefaultHttpContext());
        _context.Setup(x => x.HttpContext.Request).Returns(requestMock.Object);
        _context.Setup(x => x.HttpContext.Response).Returns(responseMock.Object);
        requestMock.SetupGet(x => x.ContentLength).Throws<Exception>();

        // Act
        var result = await new EmptyFileFilter(_logger.Object).InvokeAsync(_context.Object, _next.Object);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(null));
            responseMock.VerifySet(x => x.StatusCode = 400, Times.Once);
        });

    }

}
