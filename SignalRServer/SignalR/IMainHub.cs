namespace SignalR
{
    public interface IMainHub
    {
        public Task UploadChunk(string connectionId, string fileName, string contentType, byte[] chunk, int index, int totalChunks);
        public Task ReceiveLlmResponse(string connectionId, string fileName, string contentType, string fileContent, string oldFileContent);
        public Task ReceiveTotalFiles(string connectionId, int totalFiles);
        public Task ReceiveProgressInformation(string connectionId, string progressInformationMessage);
        public Task ReceiveError(string connectionId, string errorMessage);
        public Task<string> GetConnectionId();
    }
}