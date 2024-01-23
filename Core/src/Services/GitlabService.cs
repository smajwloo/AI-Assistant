using InterfacesAia.Services;

namespace aia_api.Services;

public class GitlabService
{
    private readonly HttpClient _gitlabHttpClient;
    private readonly IFileSystemStorageService _fileSystemStorageService;

    public GitlabService(IHttpClientFactory httpClientFactory, IFileSystemStorageService fileSystemStorageService)
    {
        _gitlabHttpClient = httpClientFactory.CreateClient("gitlabApiV4Client");
        _fileSystemStorageService = fileSystemStorageService;
    }

    /// <summary>
    /// Downloads a repository from GitLab using the provided project ID and API token.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="apiToken">The API token for authentication.</param>
    /// <param name="path">The path where the repository will be downloaded.</param>
    /// <returns>The path where the repository was downloaded.</returns>
    /// <exception cref="Exception">Thrown if the repository download fails.</exception>
    public async Task<string> DownloadRepository(string projectId, string apiToken)
    {
        var url = $"/api/v4/projects/{projectId}/repository/archive.zip";
        _gitlabHttpClient.DefaultRequestHeaders.Add("Private-Token", apiToken);

        var response = await _gitlabHttpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Could not download repository. Status code: {response.StatusCode}  Reason: {response.ReasonPhrase}");

        var date = DateTime.Now.ToString("dd-MM-yyyy HH-mm-ss").Replace(" ", "_");
        var fileName = $"{projectId}_{date}.zip";

        return await _fileSystemStorageService.StoreInTemp(response, fileName);
    }

}
