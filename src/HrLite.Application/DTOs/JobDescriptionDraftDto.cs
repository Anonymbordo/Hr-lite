namespace HrLite.Application.DTOs;

public class JobDescriptionDraftDto
{
    public string TitleSuggested { get; set; } = string.Empty;
    public List<string> Responsibilities { get; set; } = new();
    public List<string> Requirements { get; set; } = new();
    public List<string> NiceToHave { get; set; } = new();
    public string JobDescription { get; set; } = string.Empty;
}
