using HrLite.Application.DTOs;
using System.Text.Json;

namespace HrLite.Application.Common;

public static class JobDescriptionTemplate
{
    public static string BuildJson(string roleName, string? departmentName)
    {
        var department = NormalizeDepartmentName(departmentName);
        var title = BuildTitle(roleName, department);

        var draft = new JobDescriptionDraftDto
        {
            TitleSuggested = title,
            Responsibilities = BuildResponsibilities(department),
            Requirements = BuildRequirements(department),
            NiceToHave = BuildNiceToHave(department),
            JobDescription = BuildJobDescription(title, department)
        };

        return JsonSerializer.Serialize(draft);
    }

    private static string NormalizeDepartmentName(string? departmentName)
    {
        if (string.IsNullOrWhiteSpace(departmentName))
        {
            return "Genel";
        }

        var trimmed = departmentName.Trim();

        return trimmed switch
        {
            "Human Resources" => "Insan Kaynaklari",
            "Engineering" => "Yazilim",
            "Sales" => "Satis",
            "Finance" => "Finans",
            _ => trimmed
        };
    }

    private static string BuildTitle(string roleName, string departmentName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return $"{departmentName} Uzmani";
        }

        if (roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            return "Sistem Yonetimi Uzmani";
        }

        if (roleName.Equals("HR", StringComparison.OrdinalIgnoreCase))
        {
            return "Insan Kaynaklari Uzmani";
        }

        if (roleName.Equals("Employee", StringComparison.OrdinalIgnoreCase))
        {
            return MapDepartmentToTitle(departmentName);
        }

        if (!string.IsNullOrWhiteSpace(departmentName) &&
            !departmentName.Equals("Genel", StringComparison.OrdinalIgnoreCase) &&
            !roleName.Contains(departmentName, StringComparison.OrdinalIgnoreCase))
        {
            return $"{departmentName} {roleName}";
        }

        return roleName;
    }

    private static string MapDepartmentToTitle(string departmentName)
    {
        return departmentName switch
        {
            "Yazilim" => "Yazilim Muhendisi",
            "Satis" => "Satis Uzmani",
            "Finans" => "Finans Uzmani",
            "Insan Kaynaklari" => "Insan Kaynaklari Uzmani",
            "Genel" => "Genel Uzman",
            _ => $"{departmentName} Uzmani"
        };
    }

    private static List<string> BuildResponsibilities(string departmentName)
    {
        return new List<string>
        {
            $"{departmentName} sureclerinde ihtiyac analizi yapmak ve uygun cozumler gelistirmek.",
            "Ekip icindeki paydaslarla koordinasyon saglamak ve ilerlemeyi takip etmek.",
            "Kalite, guvenlik ve uyumluluk standartlarina uygun calismak.",
            "Dokumantasyon ve raporlama calismalarini duzenli surdurmek."
        };
    }

    private static List<string> BuildRequirements(string departmentName)
    {
        return new List<string>
        {
            $"{departmentName} alaninda temel bilgi ve deneyim.",
            "Analitik dusunme ve problem cozme becerisi.",
            "Etkili iletisim ve ekip calismasina yatkinlik.",
            "Planlama, onceliklendirme ve zaman yonetimi becerisi."
        };
    }

    private static List<string> BuildNiceToHave(string departmentName)
    {
        return new List<string>
        {
            $"{departmentName} odakli sertifika veya egitim programlarina katilim.",
            "Surec iyilestirme veya otomasyon projelerinde deneyim."
        };
    }

    private static string BuildJobDescription(string title, string departmentName)
    {
        var paragraph1 =
            $"Bu pozisyon, {title} olarak {departmentName} disiplininde is ihtiyaclarini karsilamaya odaklanir. " +
            "Rol, gereksinim analizi, planlama, uygulama ve surec iyilestirme adimlarinda aktif sorumluluk alir. " +
            "Paydaslarla duzenli iletisim kurarak kapsam, hedef ve beklentileri netlestirir ve teslimat sureclerini destekler.";

        var paragraph2 =
            $"Sirket icinde bu rol, {departmentName} departmaninin hedeflerine ulasmasinda kritik bir baglanti noktasi olarak calisir. " +
            "Ekipler arasi koordinasyon, risklerin azaltilmasi ve surekli iyilestirme yaklasimi ile is sonucuna katki saglar. " +
            "Disiplinli calisma ve raporlama sayesinde karar vericilere guvenilir girdi sunar.";

        return $"{paragraph1}\n\n{paragraph2}";
    }
}
