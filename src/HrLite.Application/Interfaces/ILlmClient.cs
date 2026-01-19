namespace HrLite.Application.Interfaces;

public interface ILlmClient
{
    Task<string> GenerateInsightsAsync(string aggregatedData, CancellationToken cancellationToken = default);
}
