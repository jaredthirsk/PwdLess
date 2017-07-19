using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace PwdLess.Filters
{
    public class SetUserIdAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            string userId = ((ControllerBase)context.Controller).User.FindFirst(ClaimTypes.NameIdentifier).Value;
            context.ActionArguments.Add(new KeyValuePair<string, object>("userId", userId));
        }
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            // do something after the action executes
        }
    }
}
