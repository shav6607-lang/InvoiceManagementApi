using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace InvoiceManagementApi.Filters
{
    public class GlobalExceptionFilter : IAsyncExceptionFilter
    {
        private readonly ILogger<GlobalExceptionFilter> _logger;

        public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger)
        {
            _logger = logger;
        }

        public Task OnExceptionAsync(ExceptionContext context)
        {
            _logger.LogError(context.Exception, "Unhandled exception in request {Path}", context.HttpContext.Request.Path);

            var problem = new ProblemDetails
            {
                Title = "An unexpected error occurred",
                Detail = context.Exception.Message,
                Status = 500
            };

            context.Result = new ObjectResult(problem)
            {
                StatusCode = 500
            };

            context.ExceptionHandled = true;
            return Task.CompletedTask;
        }
    }
}