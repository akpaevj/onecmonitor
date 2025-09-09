namespace OneSwiss.Server.Models;

public class TechLogSeance : DatabaseObject
{
    public string Description { get; set; } = string.Empty;
    public TechLogSeanceStartMode StartMode { get; set; } = TechLogSeanceStartMode.Immediately;
    public DateTime StartDateTime { get; set; } = DateTime.MinValue;
    public int Duration { get; set; } = 15;
    public bool DirectSending { get; set; }

    public DateTime FinishDateTime => StartMode == TechLogSeanceStartMode.Monitor
        ? DateTime.MaxValue
        : StartDateTime.AddMinutes(Duration);

    public virtual List<LogTemplate> Templates { get; set; } = [];
    public virtual List<Agent> Agents { get; set; } = [];
}