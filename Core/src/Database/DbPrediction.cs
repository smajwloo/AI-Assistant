using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using InterfacesAia.Database;

namespace aia_api.Database;

public class DbPrediction : IDbPrediction
{
    [Key]
    [Required]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string ClientConnectionId { get; set; }
    public string ModelName { get; set; }
    public string FileName { get; set; }
    public string FileExtension { get; set; }
    public string SystemPrompt { get; set; }
    public string Prompt { get; set; }
    public string InputCode { get; set; }
    public string? PredictionResponseText { get; set; }
    public string? EditedResponseText { get; set; }
}
