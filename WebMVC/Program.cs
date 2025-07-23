using Microsoft.AspNetCore.Authentication.Cookies;
using WebMVC.API;

namespace WebMVC
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            // Ensure configuration is loaded from appsettings.json
            builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            // ✅ Đăng ký HttpClient & các API services cho MVC gọi API bên ngoài
            builder.Services.AddScoped<AccountApiService>();
            builder.Services.AddScoped<AuthApiService>();
            builder.Services.AddScoped<PostApiService>();
            builder.Services.AddHttpClient();
            builder.Services.AddHttpContextAccessor();
            // ✅ Session để lưu token
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.None; // Hoặc None nếu MVC chạy HTTP
                options.Cookie.SameSite = SameSiteMode.Lax;
            });
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                 .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
                 {
                     options.LoginPath = "/Auth/Login";
                     options.AccessDeniedPath = "/Auth/AccessDenied";
                 });
            builder.Services.AddControllersWithViews();


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseRouting();
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSession();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
