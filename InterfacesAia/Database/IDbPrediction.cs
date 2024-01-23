using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InterfacesAia.Database;

public interface IDbPrediction
{
    [Key]
    [Required]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    int Id { get; }
    string ClientConnectionId { get; set; }
    string ModelName { get; set; }
    string FileName { get; set; }
    string FileExtension { get; set; }
    string SystemPrompt { get; set; }
    string Prompt { get; set; }
    string InputCode { get; set; }
    string? PredictionResponseText { get; set; }
    string? EditedResponseText { get; set; }
}
