using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Net.payOS;
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
            //builder.Services.AddScoped<AccountApiService>();
            builder.Services.AddScoped<AuthApiService>();
            builder.Services.AddScoped<FavoritePostApiService>();
            builder.Services.AddScoped<ProfileApiService>();
            builder.Services.AddScoped<PasswordApiService>();
            builder.Services.AddScoped<PostApiService>();
            builder.Services.AddScoped<PayOSApiService>();
            builder.Services.AddScoped<PaymentPayOSApiService>();
            builder.Services.AddScoped<AccommodationODataApiService>();
            builder.Services.AddScoped<IHomeApiService, HomeApiService>();
            var apiBaseUrl = builder.Configuration["ApiSettings:ApiBaseUrl"];
            builder.Services.AddHttpClient<FavoritePostApiService>(client =>
            {
                client.BaseAddress = new Uri(apiBaseUrl);
            });

            builder.Services.AddHttpClient<AccommodationODataApiService>(client =>
            {
                client.BaseAddress = new Uri(apiBaseUrl);
            });

            builder.Services.AddHttpClient<AccountApiService>((sp, client) =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                client.BaseAddress = new Uri(config["ApiSettings:ApiBaseUrl"]!);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            });


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


            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy => policy.RequireRole("A"));
            });
            builder.Services.AddControllersWithViews();

            builder.Services.AddControllersWithViews()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles; // Giữ nguyên dòng này nếu đã có
        });
            builder.Services.AddSingleton(sp =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                return new PayOS(
                    config["PayOS:ClientId"],
                    config["PayOS:ApiKey"],
                    config["PayOS:ChecksumKey"]
                );
            });


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
