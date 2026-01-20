namespace HrLite.Application.Interfaces;

public interface IAiService
{
    // Görev ismine göre (örn: "Senior Developer") yapay zekadan açıklama isteyeceğiz.
    Task<string> GenerateJobDescriptionAsync(string roleName, string departmentName);
}