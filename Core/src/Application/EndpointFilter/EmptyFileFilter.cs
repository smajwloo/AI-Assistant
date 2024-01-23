namespace aia_api.Application.EndpointFilter;

public class EmptyFileFilter : IEndpointFilter
{
    private readonly ILogger<EmptyFileFilter> _logger;

    public EmptyFileFilter(ILogger<EmptyFileFilter> logger)
    {
        _logger = logger;
    }
    
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            var form = context.HttpContext.Request.ReadFormAsync();
            var file = form.Result.Files.Count > 0 ? form.Result.Files[0] : null;

            if (file != null) return await next(context);
            context.HttpContext.Response.StatusCode = 400;
            await context.HttpContext.Response.WriteAsync("No file received or file is empty.");
            return null;
        }
        catch (Exception e)
        {
            _logger.LogCritical("Error: {message}, {stackTrace}", e.Message, e.StackTrace);
            context.HttpContext.Response.StatusCode = 400;
            return null;
        }
    }
}
