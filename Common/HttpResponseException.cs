using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

namespace WarehouseWebApi.Common
{
    // https://tech.tanaka733.net/entry/2020/02/use-exceptions-to-modify-the-response

    public class HttpResponseException : Exception
    {
        public int Status { get; set; }

        public HttpResponseException(int status, string message, Exception innerException)
         : base(message, innerException)
        {
            Status = status;
        }

        public HttpResponseException(int status, Exception innerException)
         : base(innerException.Message, innerException)
        {
            Status = status;
        }

        public HttpResponseException(int status, string message)
         : base(message)
        {
            Status = status;
        }
    }

    public class HttpResponseExceptionFilter : IActionFilter, IOrderedFilter
    {
        public int Order => int.MaxValue - 10;

        public void OnActionExecuting(ActionExecutingContext context) { }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Exception is HttpResponseException httpResponseException)
            {
                context.Result = new ObjectResult(httpResponseException.Message)
                {
                    StatusCode = httpResponseException.Status
                };

                context.ExceptionHandled = true;
            }
        }
    }
}
