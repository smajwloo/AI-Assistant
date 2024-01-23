using System.IO.Abstractions;
using System.IO.Compression;
using System.Net;
using aia_api.Application.OpenAi;
using aia_api.Configuration.Records;
using aia_api.Database;
using Azure.AI.OpenAI;
using InterfacesAia.Database;
using InterfacesAia.Handlers;
using InterfacesAia.Services;
using Microsoft.Extensions.Options;

namespace aia_api.Application.Handlers.FileHandler;

/// <summary>
/// Upload files to OpenAi for processing
/// </summary>
public class LlmFileUploaderHandler : AbstractFileHandler
{
    private readonly ILogger<LlmFileUploaderHandler> _logger;
    private readonly Settings _settings;
    private readonly OpenAiSettings _openAiSettings;
    private readonly OpenAiApi _openAiApi;
    private readonly IFileSystem _fileSystem;
    private readonly ISignalRService _signalRService;
    private readonly IPredictionDatabaseService _predictionDatabaseService;
    private List<string> _errors;
    private string _clientConnectionId;

    public LlmFileUploaderHandler(
        ILogger<LlmFileUploaderHandler> logger,
        IOptions<Settings> settings,
        IOptions<OpenAiSettings> openAiSettings,
        OpenAiApi openAiApi,
        IFileSystem fileSystem,
        ISignalRService signalRService,
        IPredictionDatabaseService predictionDatabaseService
        ) : base(logger, settings)
    {
        _logger = logger;
        _settings = settings.Value;
        _openAiSettings = openAiSettings.Value;
        _openAiApi = openAiApi;
        _fileSystem = fileSystem;
        _signalRService = signalRService;
        _predictionDatabaseService = predictionDatabaseService;
    }

    /// <summary>
    /// This handle method expects a zip-file at the outputPath with the name of the file in the inputPath.
    /// If it does not exist, it will throw an exception.
    /// </summary>
    /// <throws>FileNotFoundException if zip-file cannot be found</throws>
    public override async Task<IHandlerResult> Handle(string clientConnectionId, string inputPath, string inputContentType)
    {
        _errors = new();
        _clientConnectionId = clientConnectionId;
        var fileName = _fileSystem.Path.GetFileName(inputPath);
        var outputFilePath = _fileSystem.Path.Combine(_settings.TempFolderPath + "Output/", fileName);
        var zipArchive = GetZipArchive(outputFilePath);
        
        _signalRService.SendTotalFiles(clientConnectionId, zipArchive.Entries.Count);

        await ProcessFiles(zipArchive);
        return CreateHandlerResult();
    }

    private ZipArchive GetZipArchive(string outputFilePath)
    {
        var fileStream = _fileSystem.FileStream.New(outputFilePath, FileMode.Open, FileAccess.Read);
        return new ZipArchive(fileStream, ZipArchiveMode.Read);
    }

    private async Task<IDbPrediction> SavePredictionToDatabase(ZipArchiveEntry file)
    {
        var fileExtension = _fileSystem.Path.GetExtension(file.FullName);

        using var reader = new StreamReader(file.Open());
        string inputCode = await reader.ReadToEndAsync();
        var customPrompt = _openAiSettings.Prompt.Replace("${code}", inputCode);

        var dbPrediction = new DbPrediction
        {
            ClientConnectionId = _clientConnectionId,
            ModelName = _openAiSettings.ModelName,
            FileExtension = fileExtension,
            FileName = file.FullName,
            SystemPrompt = _openAiSettings.SystemPrompt,
            Prompt = customPrompt,
            InputCode = inputCode
        };

        return await _predictionDatabaseService.CreatePrediction(dbPrediction);
    }

    private async Task ProcessFiles(ZipArchive zipArchive)
    {
        var dbPredictions = new List<IDbPrediction>();

        foreach (var file in zipArchive.Entries)
        {
            dbPredictions.Add(await SavePredictionToDatabase(file));
        }

        _logger.LogInformation("Starting LLM Processing");
        var sendCompletionStartTime = DateTime.Now;
        var gptCompletions = await _openAiApi.SendOpenAiCompletionAsync(dbPredictions);
        var sendCompletionEndTime = DateTime.Now;
        _logger.LogInformation("Done with LLM Processing");

        if (gptCompletions.Count == 0)
        {
            _errors.Add("Error: No content received from LLM.");
            return;
        }

        var processResponseStartTime = DateTime.Now;
        foreach (var completion in gptCompletions)
        {
            CheckIfErrors(completion.Value, completion.Key);
            if (_errors.Count > 0) return;

            _openAiApi.ProcessApiResponse(completion.Value, completion.Key);
            _logger.LogInformation("Llm response for {fileName} with id {id} was successfully processed", completion.Key.FileName, completion.Key.Id);
        }
        var processResponseEndTime = DateTime.Now;
        _logger.LogInformation("Duration LLM: {time} for {amount} files", sendCompletionEndTime - sendCompletionStartTime, dbPredictions.Count);
        _logger.LogInformation("Duration processing: {time}", processResponseEndTime - processResponseStartTime);
    }

    private void CheckIfErrors(ChatChoice openAiResponse, IDbPrediction prediction)
    {
        if (openAiResponse.Message.Content.Length <= 0)
            _errors.Add($"File: {prediction.FileName}, Error: No content received from LLM.");
        if (openAiResponse.FinishReason == CompletionsFinishReason.TokenLimitReached)
            _errors.Add($"File {prediction.FileName}, Error: Token limit reached for message.");
        if (openAiResponse.FinishReason == CompletionsFinishReason.ContentFiltered)
            _errors.Add($"File {prediction.FileName}, Error: Potentially sensitive content found and filtered from the LLM result.");
        if (openAiResponse.FinishReason == null)
            _errors.Add($"File {prediction.FileName}, Error: LLM is still processing the request.");
    }

    private HandlerResult CreateHandlerResult()
    {
        if (_errors.Count > 0)
        {
            return new HandlerResult
            {
                Success = false,
                StatusCode = HttpStatusCode.BadRequest,
                ErrorMessage = string.Join("; ", _errors)
            };
        }

        return new HandlerResult
        {
            Success = true,
            StatusCode = HttpStatusCode.OK,
            ErrorMessage = "OK"
        };
    }

}
