namespace InterfacesAia.Services;

public interface ISignalRService
{
    void SendLlmResponse(string connectionId, string fileName, string fileExtension, string content, string inputCode);
    void SendTotalFiles(string connectionId, int totalFiles);
    Task InvokeProgressInformationMessage(string connectionId, string progressInformationMessage);
    Task InvokeErrorMessage(string connectionId, string errorMessage);
}