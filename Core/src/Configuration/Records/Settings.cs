namespace aia_api.Configuration.Records;

public record Settings
{
    public string[] SupportedContentTypes { get; set; }
    public string[] AllowedFileTypes { get; set; }
    public string TempFolderPath { get; set; }
    public string ServiceBusUrl { get; set; }
};
