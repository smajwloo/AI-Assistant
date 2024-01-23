using InterfacesAia.Handlers;

namespace InterfacesAia.Helpers;

public interface IFileHandlerFactory
{
    IUploadedFileHandler GetFileHandler();
}
