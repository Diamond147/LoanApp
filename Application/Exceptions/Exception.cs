

namespace Application.Exceptions
{
    public abstract class AppException : Exception
    {
        protected AppException(string message) : base(message) { }
    }

    public class ValidationException : AppException
    {
        public ValidationException(string message) : base(message) { }
    }

    public class NotFoundException : AppException
    {
        public NotFoundException(string message) : base(message) { }
    }

    public class ConflictException : AppException
    {
        public ConflictException(string message) : base(message) { }
    }

    public class UnauthorizedException : AppException
    {
        public UnauthorizedException(string message) : base(message) { }
    }

    public class ExternalServiceUnavailableException : AppException
    {
        public ExternalServiceUnavailableException(string message, Exception? inner = null)
            : base(message) { }
    }

}
