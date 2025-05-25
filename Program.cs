using CollectorHub.Services;
using CollectorHub.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

builder.Services.AddDbContext<DBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireClaim("Role", "1"));
    options.AddPolicy("UserOnly", policy => policy.RequireClaim("Role", "2"));
});

builder.Services.AddScoped<AuthService>();

builder.Services.AddControllersWithViews();

builder.Services.AddScoped<CollectorHub.Models.DBContext>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Collections"); // Перенаправляем на Collections вместо Home
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Collections}/{action=Index}/{id?}");

app.Use(async (context, next) =>
{
    if (context.Request.Path == "/")
    {
        if (context.User.Identity?.IsAuthenticated ?? false)
        {
            context.Response.Redirect("/Collections");
        }
        else
        {
            context.Response.Redirect("/Account/Login");
        }
        return;
    }
    await next();
});

app.Run();