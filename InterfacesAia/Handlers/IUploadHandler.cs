namespace InterfacesAia.Handlers;

public interface IUploadHandler
{
	void ReceiveFileChunk(string connectionId, string fileName, string contentType, byte[] chunk, int index, int totalChunks);
}

