namespace Dwight;

public record ApiResponse<T>(T? Value, ClientError? Error)
{
    
}
