namespace ChampHeart.MVC.Models;

// Result of a call to the WebAPI, with the error message to show if it failed
public class ApiResult
{
    public bool Success { get; set; }

    public string ErrorMessage { get; set; } = string.Empty;

    public int StatusCode { get; set; }

    public static ApiResult Ok() =>
        new ApiResult { Success = true };

    public static ApiResult Fail(string errorMessage, int statusCode) =>
        new ApiResult { Success = false, ErrorMessage = errorMessage, StatusCode = statusCode };
}

public class ApiResult<T> : ApiResult
{
    public T? Data { get; set; }

    public static ApiResult<T> Ok(T data) =>
        new ApiResult<T> { Success = true, Data = data };

    public new static ApiResult<T> Fail(string errorMessage, int statusCode) =>
        new ApiResult<T> { Success = false, ErrorMessage = errorMessage, StatusCode = statusCode };
}
