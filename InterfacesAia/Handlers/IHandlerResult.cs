using System.Net;

namespace InterfacesAia.Handlers;

public interface IHandlerResult
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; }
    public HttpStatusCode StatusCode { get; set; }
}
