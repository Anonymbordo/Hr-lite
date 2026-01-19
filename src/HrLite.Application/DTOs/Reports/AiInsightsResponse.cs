namespace HrLite.Application.DTOs.Reports;

public class AiInsightsResponse
{
    public string Summary { get; set; } = string.Empty;
    public List<string> Insights { get; set; } = new();
    public List<string> RecommendedActions { get; set; } = new();
}
