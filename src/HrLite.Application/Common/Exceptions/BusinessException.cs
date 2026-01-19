namespace HrLite.Application.Common.Exceptions;

public class BusinessException : Exception
{
    public string ErrorCode { get; }
    public List<string> Details { get; }

    public BusinessException(string message, string errorCode = "BUSINESS_RULE_VIOLATION", List<string>? details = null)
        : base(message)
    {
        ErrorCode = errorCode;
        Details = details ?? new List<string>();
    }
}
