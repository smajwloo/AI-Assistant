namespace aia_api.Configuration.Records;

public record OpenAiSettings
{
    public string ApiToken { get; set; }
    public string ModelName { get; set; }
    public string SystemPrompt { get; set; }
    public string Prompt { get; set; }
    public float Temperature { get; set; }
    public int? MaxTokens { get; set; }
}
