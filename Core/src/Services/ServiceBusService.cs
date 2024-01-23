using aia_api.Configuration.Records;
using InterfacesAia.Services;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;

namespace aia_api.Services
{
	public class ServiceBusService : IServiceBusService
    {
        private readonly ILogger<ServiceBusService> _logger;
        private readonly IOptions<Settings> _settings;
        private HubConnection _connection;

        public ServiceBusService(ILogger<ServiceBusService> logger, IOptions<Settings> settings)
        {
            _logger = logger;
            _settings = settings;
        }

        public async Task<HubConnection> ExecuteAsync()
        {
            var uri = _settings.Value.ServiceBusUrl;
            _connection = new HubConnectionBuilder().WithUrl(uri).WithAutomaticReconnect().Build();

            try
            {
                await _connection.StartAsync();
                _logger.LogInformation("Connection state: {state}", _connection.State);
            }
            catch (HttpRequestException e)
            {
                _logger.LogCritical("Error: {message}, {stackTrace}", e.Message, e.StackTrace);
            }
            return _connection;
        }

        public HubConnection GetConnection() => _connection;
    }
}

