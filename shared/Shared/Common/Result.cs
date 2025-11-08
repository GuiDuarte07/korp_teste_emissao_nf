using System.Text.Json.Serialization;

namespace Shared.Common
{
    public class Result<T>
    {
        public bool IsSuccess { get; set; }
        public T? Data { get; set; }
        public ErrorCode? ErrorCode { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;

        // Construtor para deserialização JSON
        [JsonConstructor]
        public Result(bool isSuccess, T? data, ErrorCode? errorCode, string errorMessage)
        {
            IsSuccess = isSuccess;
            Data = data;
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
        }

        public static Result<T> Success(T data) => new(true, data, null, string.Empty);

        public static Result<T> Failure(ErrorCode errorCode, string errorMessage) => new(false, default, errorCode, errorMessage);
    }

    public class Result
    {
        public bool IsSuccess { get; set; }
        public ErrorCode? ErrorCode { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;

        // Construtor para deserialização JSON
        [JsonConstructor]
        public Result(bool isSuccess, ErrorCode? errorCode, string errorMessage)
        {
            IsSuccess = isSuccess;
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
        }

        public static Result Success() => new(true, null, string.Empty);

        public static Result Failure(ErrorCode errorCode, string errorMessage) => new(false, errorCode, errorMessage);
    }
}
