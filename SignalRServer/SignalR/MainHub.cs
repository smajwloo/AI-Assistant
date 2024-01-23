using Microsoft.AspNetCore.SignalR;

namespace SignalR
{
	public class MainHub : Hub<IMainHub>
	{
        private readonly ILogger<MainHub> _logger;

        public MainHub(ILogger<MainHub> logger)
        {
            _logger = logger;
        }
        
        public async Task UploadChunk(string connectionId, string fileName, string contentType, string chunkAsBase64, int index, int totalChunks)
        {
            if (await SendErrorIfEmpty(connectionId, chunkAsBase64)) return;

            try
            {
                byte[] chunk = Convert.FromBase64String(chunkAsBase64);
                await Clients.Others.UploadChunk(connectionId, fileName, contentType, chunk, index, totalChunks);
                _logger.LogInformation("Chunk {index} of file {fileName} send to clients", index, fileName);
            }
            catch (FormatException e)
            {
                await Clients.Caller.ReceiveError(connectionId, "The file is not converted to base64 correctly.");
                _logger.LogCritical("Error: {message}, stacktrace: {stackTrace}", e.Message, e.StackTrace);
            }
        }

        public async Task ReturnLlmResponse(string connectionId, string fileName, string contentType, string fileContent, string oldFileContent)
        {
            if (await SendErrorIfEmpty(connectionId, fileContent) || await SendErrorIfEmpty(connectionId, oldFileContent)) return;

            await Clients.Client(connectionId).ReceiveLlmResponse(connectionId, fileName, contentType, fileContent, oldFileContent);
            _logger.LogInformation("File {fileName} with contentType {contentType} send to client with id {connectionId}", fileName, contentType, connectionId);
        }

        public async Task ReturnProgressInformation(string connectionId, string progressInformationMessage)
        {
            await Clients.Client(connectionId).ReceiveProgressInformation(connectionId, progressInformationMessage);
            _logger.LogInformation("Progress information message send: {message}", progressInformationMessage);
        }

        public async Task ReturnError(string connectionId, string errorMessage)
        {
            await Clients.Client(connectionId).ReceiveError(connectionId, errorMessage);
            _logger.LogInformation("Received error: {message}", errorMessage);
        }

        public async Task ReturnTotalFiles(string connectionId, int totalFiles)
        {
            await Clients.Client(connectionId).ReceiveTotalFiles(connectionId, totalFiles);
            _logger.LogInformation("Received total files: {amount}", totalFiles);
        }

        public async Task<string> GetConnectionId()
        {
            return Context.ConnectionId;
        }

        private async Task<bool> SendErrorIfEmpty(string connectionId, string base64)
        {
            if (!string.IsNullOrEmpty(base64)) return false;
            await Clients.Caller.ReceiveError(connectionId, "No file received or file is empty.");
            return true;
        }
    }
}

