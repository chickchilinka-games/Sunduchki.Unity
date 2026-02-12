namespace Modules.AuthenticationSystem.Data
{
    public class OperationResult
    {
        protected OperationResult(bool success, string error)
        {
            Success = success;
            Error = error;
        }
        public bool Success { get; }
        public string Error { get; }

        public static OperationResult Failed(string error)
        {
            return new OperationResult(false, error);
        }

        public static OperationResult Succeeded()
        {
            return new OperationResult(true, string.Empty);
        }
    }
}
