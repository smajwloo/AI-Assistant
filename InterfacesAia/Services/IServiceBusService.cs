using Microsoft.AspNetCore.SignalR.Client;

namespace InterfacesAia.Services
{
	public interface IServiceBusService
	{
        Task<HubConnection> ExecuteAsync();
        HubConnection GetConnection();
    }
}

