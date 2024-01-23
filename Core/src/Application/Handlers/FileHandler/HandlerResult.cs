using System.Net;
using InterfacesAia.Handlers;

namespace aia_api.Application.Handlers.FileHandler;

public class HandlerResult : IHandlerResult
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; }
    public HttpStatusCode StatusCode { get; set; }
}
