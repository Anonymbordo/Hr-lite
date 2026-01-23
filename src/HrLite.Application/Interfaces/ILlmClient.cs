namespace HrLite.Application.Interfaces;

public interface ILlmClient
{
    Task<string> GenerateInsightsAsync(string aggregatedData, CancellationToken cancellationToken = default);

    Task<string> NormalizeLeaveReasonAsync(string text, IReadOnlyList<string> allowedLeaveTypeCodes, CancellationToken cancellationToken = default);
    Task<string> ExplainLeaveDecisionAsync(string decisionFactsJson, CancellationToken cancellationToken = default);
}
