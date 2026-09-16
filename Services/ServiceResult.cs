namespace ProductionMeeting.Services;

public class ServiceResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;

    public static ServiceResult Ok(string message) => new() { Success = true, Message = message };
    public static ServiceResult Fail(string message) => new() { Success = false, Message = message };
}
