using InterfacesAia.Database;

namespace InterfacesAia.Services;

public interface IPredictionDatabaseService
{
    Task<IDbPrediction> CreatePrediction(IDbPrediction prediction);
    IDbPrediction GetPrediction(int predictionId);
    void UpdatePredictionResponseText(IDbPrediction dbPrediction, string responseText);
    void UpdatePredictionEditedResponseText(IDbPrediction dbPrediction, string editedResponseText);
}
