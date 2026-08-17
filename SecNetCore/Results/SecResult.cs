namespace SecNetCore.Results
{
    public class SecResult<T>
    {
        public bool Success { get; set; }

        public T? Data { get; set; }

        public string? Message { get; set; }

        public List<string> Errors { get; set; } = new();

        public static SecResult<T> Ok(
            T data,
            string? message = null)
        {
            return new SecResult<T>
            {
                Success = true,
                Data = data,
                Message = message
            };
        }

        public static SecResult<T> Fail(
            string message)
        {
            return new SecResult<T>
            {
                Success = false,
                Message = message,
                Errors = new List<string> { message }
            };
        }
    }
}
