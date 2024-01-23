using aia_api.Application.Helpers;
using aia_api.Configuration.Records;
using Azure;
using Azure.AI.OpenAI;
using InterfacesAia.Database;
using InterfacesAia.Services;
using Microsoft.Extensions.Options;

namespace aia_api.Application.OpenAi;

public class OpenAiApi
{
    private readonly ILogger<OpenAiApi> _logger;
    private readonly OpenAiSettings _openAiSettings;
    private readonly ISignalRService _signalRService;
    private readonly CommentManipulationHelper _commentManipulationHelper;
    private readonly IPredictionDatabaseService _predictionDatabaseService;

    public OpenAiApi(
        ILogger<OpenAiApi> logger,
        IOptions<OpenAiSettings> openAiSettings,
        ISignalRService signalRService,
        CommentManipulationHelper commentManipulationHelper,
        IPredictionDatabaseService predictionDatabaseService
        )
    {
        _logger = logger;
        _openAiSettings = openAiSettings.Value;
        _signalRService = signalRService;
        _commentManipulationHelper = commentManipulationHelper;
        _predictionDatabaseService = predictionDatabaseService;
    }

    public async Task<Dictionary<IDbPrediction, ChatChoice>> SendOpenAiCompletionAsync(List<IDbPrediction> dbPredictions)
    {
        OpenAIClient openAiClient = new OpenAIClient(_openAiSettings.ApiToken);
        var mapDbPredictionResponse = new Dictionary<IDbPrediction, ChatChoice>();
        var chatCompletionsOptionsList = PlaceIdAndCreateChatCompletions(dbPredictions);

        try
        {
            List<Task<Response<ChatCompletions>>> tasks = new List<Task<Response<ChatCompletions>>>();

            foreach (var chatCompletionsOptions in chatCompletionsOptionsList)
            {
                tasks.Add(openAiClient.GetChatCompletionsAsync(_openAiSettings.ModelName, chatCompletionsOptions));
            }

            var responses = await Task.WhenAll(tasks);

            foreach (var response in responses)
            {
                var llmResponseValue = response.Value.Choices.First().Message.Content;
                var stringIndex = llmResponseValue.IndexOf(Environment.NewLine, StringComparison.CurrentCulture);
                int id;
                Int32.TryParse(llmResponseValue.Substring(0, stringIndex), out id);

                foreach (var dbPrediction in dbPredictions)
                {
                    if (id != dbPrediction.Id) continue;

                    mapDbPredictionResponse.Add(dbPrediction, response.Value.Choices.First());
                    break;
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
        }

        return mapDbPredictionResponse;
    }

    public void ProcessApiResponse(ChatChoice openAiResponse, IDbPrediction dbPrediction)
    {
        _logger.LogDebug("LLM response for {fileName} was {response}", dbPrediction.FileName,
            openAiResponse.Message.Content);
        _logger.LogDebug("End of llm response for {fileName}", dbPrediction.FileName);
        string codeWithComments =
            _commentManipulationHelper.ReplaceCommentsInCode(openAiResponse.Message.Content, dbPrediction.InputCode);

        _predictionDatabaseService.UpdatePredictionResponseText(dbPrediction, openAiResponse.Message.Content);
        _predictionDatabaseService.UpdatePredictionEditedResponseText(dbPrediction, codeWithComments);
        _signalRService.SendLlmResponse(dbPrediction.ClientConnectionId, dbPrediction.FileName, dbPrediction.FileExtension, 
            codeWithComments, dbPrediction.InputCode);
    }

    private List<ChatCompletionsOptions> PlaceIdAndCreateChatCompletions(List<IDbPrediction> dbPredictions)
    {
        var chatCompletionsOptionsList = new List<ChatCompletionsOptions>();

        foreach (IDbPrediction dbPrediction in dbPredictions)
        {
            dbPrediction.Prompt = dbPrediction.Prompt.Replace("${ID}", dbPrediction.Id.ToString());
            chatCompletionsOptionsList.Add(CreateChatCompletionsOptions(dbPrediction.Prompt));
        }

        return chatCompletionsOptionsList;
    }

    private ChatCompletionsOptions CreateChatCompletionsOptions(string prompt)
    {
        return new ChatCompletionsOptions(new[]
        {
            new ChatMessage
            {
                Role = ChatRole.System,
                Content = _openAiSettings.SystemPrompt
            },
            new ChatMessage { Role = ChatRole.User, Content = prompt },
        })
        {
            Temperature = _openAiSettings.Temperature,
            MaxTokens = _openAiSettings.MaxTokens
        };
    }
}
