namespace InterfacesAia.Services;

public interface IFileSystemStorageService
{
    /// <summary>
    /// Stores the content of a response in the temp folder.
    /// Configure this folder in appsettings.json
    /// </summary>
    /// <param name="input">Should contain .Content</param>
    /// <returns>Filename of the outputfile</returns>
    /// <throws>Exception if content is undefined</throws>
    Task<string> StoreInTemp(HttpResponseMessage input, string fileName);

    /// <summary>
    /// Stores the content of a stream in the temp folder.
    /// Configure this folder in appsettings.json
    /// </summary>
    /// <param name="input">Any stream</param>
    /// <returns>Filename of the outputfile</returns>
    Task<string> StoreInTemp(Stream input, string fileName);
}
