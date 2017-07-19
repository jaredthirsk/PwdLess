using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PwdLess.Filters
{
    public class TraceExceptionsAttribute : ExceptionFilterAttribute
    {
        public override void OnException(ExceptionContext context)
        {
            //_logger.LogTrace($"Exception raised: {context.Exception.ToString()}");
            context.Result = new BadRequestObjectResult("Something went wrong."); // TODO: customise so some errors are more specific
        }
    }
}
