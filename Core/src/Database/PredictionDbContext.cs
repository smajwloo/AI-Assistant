using Microsoft.EntityFrameworkCore;

namespace aia_api.Database;

public class PredictionDbContext : DbContext
{
    public DbSet<DbPrediction> Predictions { get; set; }
    public PredictionDbContext(DbContextOptions options) : base(options)
    { }
}
