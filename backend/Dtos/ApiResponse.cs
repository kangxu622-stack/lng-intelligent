namespace backend.Dtos;

public sealed class ApiResponse<T>
{
    public int Code { get; set; }

    public string Message { get; set; } = string.Empty;

    public T? Data { get; set; }

    public static ApiResponse<T> Success(T data, string message = "Success")
    {
        return new ApiResponse<T>
        {
            Code = 200,
            Message = message,
            Data = data
        };
    }

    public static ApiResponse<T> Error(int code, string message)
    {
        return new ApiResponse<T>
        {
            Code = code,
            Message = message,
            Data = default
        };
    }
}

public sealed class ApiResponse
{
    public int Code { get; set; }

    public string Message { get; set; } = string.Empty;

    public object? Data { get; set; }

    public static ApiResponse Success(object? data = null, string message = "Success")
    {
        return new ApiResponse
        {
            Code = 200,
            Message = message,
            Data = data
        };
    }

    public static ApiResponse Error(int code, string message)
    {
        return new ApiResponse
        {
            Code = code,
            Message = message,
            Data = null
        };
    }
}
