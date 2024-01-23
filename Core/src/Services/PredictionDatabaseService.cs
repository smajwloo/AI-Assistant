using aia_api.Database;
using InterfacesAia.Database;
using InterfacesAia.Services;
using Microsoft.EntityFrameworkCore;

namespace aia_api.Services;

public class PredictionDatabaseService : IPredictionDatabaseService
{
    private readonly ILogger<PredictionDatabaseService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public PredictionDatabaseService(ILogger<PredictionDatabaseService> logger, IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task<IDbPrediction> CreatePrediction(IDbPrediction prediction)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PredictionDbContext>();
        
        await context.AddAsync((DbPrediction) prediction);
        await context.SaveChangesAsync();

        return prediction;
    }
    
    public IDbPrediction GetPrediction(int predictionId)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PredictionDbContext>();
        
        return context.Predictions.First(p => p.Id == predictionId);
    }

    public async void UpdatePredictionResponseText(IDbPrediction dbPrediction, string responseText)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PredictionDbContext>();
        
        try
        {
            dbPrediction.PredictionResponseText = responseText;
            context.Entry(dbPrediction).State = EntityState.Modified;
            await context.SaveChangesAsync();
        }
        catch (Exception e)
        {
            _logger.LogCritical("Error: {message}, {stackTrace}", e.Message, e.StackTrace);
            throw;
        }
    }
    
    public async void UpdatePredictionEditedResponseText(IDbPrediction dbPrediction, string editedResponseText)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PredictionDbContext>();
        
        try
        {
            dbPrediction.EditedResponseText = editedResponseText;
            context.Entry(dbPrediction).State = EntityState.Modified;
            await context.SaveChangesAsync();
        }
        catch (Exception e)
        {
            _logger.LogCritical("Error: {message}, {stackTrace}", e.Message, e.StackTrace);
            throw;
        }
    }
}
