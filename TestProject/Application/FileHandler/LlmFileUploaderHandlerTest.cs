using System.IO.Abstractions;
using System.Net;
using aia_api.Application.Handlers.FileHandler;
using aia_api.Application.Helpers;
using aia_api.Application.OpenAi;
using aia_api.Configuration.Records;
using aia_api.Database;
using aia_api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace TestProject.Application.FileHandler;

public class LlmFileUploaderHandlerTest
{
    private const string FileName = "testzip.zip";
    private const string InputContentType = "application/zip";
    private const string InputPathFolder = "../../../Testfiles/";
    
    private PredictionDatabaseService _predictionDatabaseService;
    private PredictionDbContext _dbContext;
    private Mock<ILogger<LlmFileUploaderHandler>> _llmFileUploaderHandlerLoggerMock;
    private Mock<ILogger<OpenAiApi>> _openAiApiLoggerMock;
    private Mock<ILogger<SignalRService>> _signalRServiceLoggerMock;
    private Mock<ILogger<ServiceBusService>> _serviceBusServiceLoggerMock;
    private Mock<ILogger<CommentManipulationHelper>> _commentManipulationHelperLoggerMock;
    private IOptions<Settings> _settings;
    private IOptions<OpenAiSettings> _openAiSettings;
    private CommentManipulationHelper _commentManipulationHelper;
    private string _inputPath;
    private string _clientConnectionId;

    [SetUp]
    public void Setup()
    {
        Environment.SetEnvironmentVariable("OPENAI_ENABLED", "true");

        var dbContextOptions = new DbContextOptionsBuilder<PredictionDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .LogTo(Console.WriteLine)
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _dbContext = new PredictionDbContext(dbContextOptions);
        var serviceScopeFactory = new Mock<IServiceScopeFactory>();
        var mockScope = new Mock<IServiceScope>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockLogger = new Mock<ILogger<PredictionDatabaseService>>();
        mockServiceProvider.Setup(x => x.GetService(typeof(PredictionDbContext))).Returns(_dbContext);
        mockScope.Setup(x => x.ServiceProvider).Returns(mockServiceProvider.Object);
        serviceScopeFactory.Setup(x => x.CreateScope()).Returns(mockScope.Object);

        _llmFileUploaderHandlerLoggerMock = new Mock<ILogger<LlmFileUploaderHandler>>();
        _openAiApiLoggerMock = new Mock<ILogger<OpenAiApi>>();
        _signalRServiceLoggerMock = new Mock<ILogger<SignalRService>>();
        _serviceBusServiceLoggerMock = new Mock<ILogger<ServiceBusService>>(); 
        _commentManipulationHelperLoggerMock = new Mock<ILogger<CommentManipulationHelper>>();
        
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.test.json")
            .AddEnvironmentVariables() 
            .Build();
        
        var settings = new Settings();
        config.GetSection("Settings").Bind(settings);
        _settings = Options.Create(settings);
        
        var openAiSettings = new OpenAiSettings();
        config.GetSection("OpenAiSettings").Bind(openAiSettings);
        _openAiSettings = Options.Create(openAiSettings);
        
        _predictionDatabaseService = new PredictionDatabaseService(mockLogger.Object, serviceScopeFactory.Object);
        _commentManipulationHelper = new CommentManipulationHelper(_commentManipulationHelperLoggerMock.Object);

        
        _inputPath = Path.Combine(InputPathFolder, FileName);
        _clientConnectionId = Guid.NewGuid().ToString();
    }

    [Test]
    public async Task Handle_ValidInput_ReturnsHandlerResult()
    {
        // Arrange
        var serviceBusService = new ServiceBusService(_serviceBusServiceLoggerMock.Object, _settings);
        await serviceBusService.ExecuteAsync();
        var signalRService = new SignalRService(_signalRServiceLoggerMock.Object, serviceBusService);
        var openAiApi = new OpenAiApi(_openAiApiLoggerMock.Object, _openAiSettings, signalRService, _commentManipulationHelper, _predictionDatabaseService);
        
        var handler = new LlmFileUploaderHandler(_llmFileUploaderHandlerLoggerMock.Object, _settings, _openAiSettings, 
                                                    openAiApi, new FileSystem(), signalRService, _predictionDatabaseService);
        
        // Act
        var result = await handler.Handle(_clientConnectionId, _inputPath, InputContentType);
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(result.ErrorMessage, Is.EqualTo("OK"));
        });
    }
    
    [Test]
    public async Task Handle_ErrorsMoreThanZero_ReturnsSuccessFalseHandlerResult()
    {
        // Arrange
        var serviceBusService = new ServiceBusService(_serviceBusServiceLoggerMock.Object, _settings);
        await serviceBusService.ExecuteAsync();
        var signalRService = new SignalRService(_signalRServiceLoggerMock.Object, serviceBusService);
        _openAiSettings.Value.MaxTokens = 1;
        var openAiApi = new OpenAiApi(_openAiApiLoggerMock.Object, _openAiSettings, signalRService, _commentManipulationHelper, _predictionDatabaseService);
        
        var handler = new LlmFileUploaderHandler(_llmFileUploaderHandlerLoggerMock.Object, _settings, _openAiSettings, 
                                                    openAiApi, new FileSystem(), signalRService, _predictionDatabaseService);
        
        // Act
        var result = await handler.Handle(_clientConnectionId, _inputPath, InputContentType);
    
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(result.ErrorMessage, Is.Not.Empty);
        });
    }
    
    [Test]
    public async Task Handle_CreatesDbPredictions_DbContainsPrediction()
    {
        // Arrange
        var serviceBusService = new ServiceBusService(_serviceBusServiceLoggerMock.Object, _settings);
        await serviceBusService.ExecuteAsync();
        var signalRService = new SignalRService(_signalRServiceLoggerMock.Object, serviceBusService);
        var openAiApi = new OpenAiApi(_openAiApiLoggerMock.Object, _openAiSettings, signalRService, _commentManipulationHelper, _predictionDatabaseService);
        
        var handler = new LlmFileUploaderHandler(_llmFileUploaderHandlerLoggerMock.Object, _settings, _openAiSettings, 
                                                    openAiApi, new FileSystem(), signalRService, _predictionDatabaseService);
        
        // Act
        await handler.Handle(_clientConnectionId, _inputPath, InputContentType);
        var retrievedPredictions = _dbContext.Predictions.ToList();
    
        // Assert
        Assert.That(retrievedPredictions, Is.Not.Null);
        Assert.That(retrievedPredictions, Is.Not.Empty);
    
    }
    
     [Test]
    public async Task Handle_CreatesDbPredictions_WithContent()
    {
        // Arrange
        var serviceBusService = new ServiceBusService(_serviceBusServiceLoggerMock.Object, _settings);
        await serviceBusService.ExecuteAsync();
        var signalRService = new SignalRService(_signalRServiceLoggerMock.Object, serviceBusService);
        var openAiApi = new OpenAiApi(_openAiApiLoggerMock.Object, _openAiSettings, signalRService, _commentManipulationHelper, _predictionDatabaseService);
        
        var handler = new LlmFileUploaderHandler(_llmFileUploaderHandlerLoggerMock.Object, _settings, _openAiSettings, 
                                                    openAiApi, new FileSystem(), signalRService, _predictionDatabaseService);
    
        // Act
        await handler.Handle(_clientConnectionId, _inputPath, InputContentType);
        var retrievedPredictions = _dbContext.Predictions.ToList();
    
        // Assert
        foreach (var prediction in retrievedPredictions)
        {
            Assert.Multiple(() =>
            {
                Assert.That(prediction.FileName, Is.Not.Null);
                Assert.That(prediction.FileExtension, Is.Not.Null);
                Assert.That(prediction.Prompt, Is.Not.Null);
            });
        }
    }
}
