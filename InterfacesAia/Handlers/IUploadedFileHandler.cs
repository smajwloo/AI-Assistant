namespace InterfacesAia.Handlers;

/// <summary>
/// The handler for the chain of responsibility.
/// The output path gets created by the chain.
/// The inputContentType is used to determine if the handler can handle the file.
/// </summary>
public interface IUploadedFileHandler
{
    Task<IHandlerResult> Handle(string clientConnectionId, string inputPath, string inputContentType);
    void SetNext(IUploadedFileHandler next);
}
