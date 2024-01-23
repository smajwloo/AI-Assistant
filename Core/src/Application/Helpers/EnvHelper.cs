namespace aia_api.Application.Helpers;

public class EnvHelper
{
    public static bool IsDev()
    {
        return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
    }

    public static bool OpenAiEnabled()
    {
        return Environment.GetEnvironmentVariable("OPENAI_ENABLED") == "true";
    }
}
