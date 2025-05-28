using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FluentisCore.Auth
{
    public class ConditionalAuthorizeAttribute : TypeFilterAttribute
    {
        public ConditionalAuthorizeAttribute() : base(typeof(ConditionalAuthorizeFilter))
        {
        }
    }
}