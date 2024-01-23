using InterfacesAia.Services;
using Microsoft.AspNetCore.SignalR.Client;

namespace aia_api.Services;

public class SignalRService : ISignalRService
{
    private readonly ILogger<SignalRService> _logger;
    private readonly IServiceBusService _serviceBusService;

    public SignalRService(ILogger<SignalRService> logger, IServiceBusService serviceBusService)
    {
        _logger = logger;
        _serviceBusService = serviceBusService;
    }
    
    public async void SendLlmResponse(string connectionId, string fileName, string fileExtension, string content, string inputCode)
    {
        HubConnection connection = _serviceBusService.GetConnection();
        if (!IsConnected()) return;

        await connection.InvokeAsync("ReturnLlmResponse", connectionId, fileName, fileExtension, content, inputCode);
    }

    public async void SendTotalFiles(string connectionId, int totalFiles)
    {
        HubConnection connection = _serviceBusService.GetConnection();
        if (!IsConnected()) return;

        await connection.InvokeAsync("ReturnTotalFiles", connectionId, totalFiles);
    }
    
    public async Task InvokeProgressInformationMessage(string connectionId, string progressInformationMessage)
    {
        HubConnection connection = _serviceBusService.GetConnection();
        if (!IsConnected()) return;
        
        await connection.InvokeAsync("ReturnProgressInformation", connectionId, progressInformationMessage);
    }
    
    public async Task InvokeErrorMessage(string connectionId, string errorMessage)
    {
        HubConnection connection = _serviceBusService.GetConnection();
        if (!IsConnected()) return;
        
        await connection.InvokeAsync("ReturnError", connectionId, errorMessage);
    }

    private bool IsConnected()
    {
        return _serviceBusService.GetConnection().State == HubConnectionState.Connected;
    }
}