using System.ComponentModel.DataAnnotations;

namespace HarmonyOS.Mediator.Data;

public class ToxicityIncident
{
    [Key]
    public int Id { get; set; }
    public long UserId { get; set; }
    public long ChatId { get; set; } 
    public string OriginalMessage { get; set; } = string.Empty;
    public string SuggestedAlternative { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
