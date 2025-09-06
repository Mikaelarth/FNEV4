namespace FNEV4.Core.DTOs
{
    public class OperationResultDto
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public static OperationResultDto Success(string? message = null)
        {
            return new OperationResultDto
            {
                IsSuccess = true,
                SuccessMessage = message
            };
        }

        public static OperationResultDto Failure(string errorMessage)
        {
            return new OperationResultDto
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
        }
    }
}
