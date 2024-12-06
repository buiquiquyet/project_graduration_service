namespace asp.Helper.ApiResponse
{
    public class ApiResponseDTO<T>
    {
        public T data { get; set; }
        public string message { get; set; }
    }
}
