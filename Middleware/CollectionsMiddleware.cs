using CollectorHub.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CollectorHub.Middleware
{
    public class CollectionsMiddleware
    {
        private readonly RequestDelegate _next;

        public CollectionsMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, DBContext dbContext)
        {
            if (context.User.Identity.IsAuthenticated)
            {
                var userId = int.Parse(context.User.FindFirstValue("UserId"));
                var collections = await dbContext.Collections
                    .Include(c => c.visibility)
                    .Include(c => c.template)
                    .Include(c => c.parent)
                    .Include(c => c.Items)
                    .Include(c => c.Inverseparent)
                    .Where(c => c.user_id == userId)
                    .ToListAsync();

                context.Items["Collections"] = collections;
            }

            await _next(context);
        }
    }

    public static class CollectionsMiddlewareExtensions
    {
        public static IApplicationBuilder UseCollectionsMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CollectionsMiddleware>();
        }
    }
}