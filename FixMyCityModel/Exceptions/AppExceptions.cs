using System;

namespace FixMyCity.Exceptions
{
    public class DataAccessException : Exception
    {
        public string StoredProcedure { get; private set; }

        public DataAccessException(string message, string storedProcedure, Exception inner)
            : base(message, inner)
        {
            StoredProcedure = storedProcedure;
        }
    }

    public class BusinessException : Exception
    {
        public string ErrorCode { get; private set; }

        public BusinessException(string message, string errorCode = "BUSINESS_ERROR")
            : base(message)
        {
            ErrorCode = errorCode;
        }
    }

    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message) { }
    }
}
