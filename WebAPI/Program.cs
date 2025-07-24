
using System.Text;
using DataAccessObjects.DB;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Repositories.Implementations;
using Repositories.Interfaces;
using Services.Implementations;
using Services.Interfaces;
using RentNest.Infrastructure.DataAccess;
using BusinessObjects.Consts;
using Microsoft.AspNetCore.Authentication.OAuth;
using DataAccessObjects;

using DataAccessObjects;
using RentNest.Core.Configs;
using DataAccessObjects.DataAccessLayer.DAO;


namespace WebAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            // ======= DATABASE =======
            builder.Services.AddDbContext<RentNestSystemContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
            builder.Services.Configure<AzureOpenAISettings>(
            builder.Configuration.GetSection("AzureOpenAISettings"));
            builder.Services.AddHttpContextAccessor();
            // ======= DEPENDENCY INJECTION =======
            // DAO
            builder.Services.AddScoped<AccommodationDAO>();
            builder.Services.AddScoped<PostDAO>();


            builder.Services.AddScoped<UserProfileDAO>();

            builder.Services.AddScoped<PackagePricingDAO>();
            builder.Services.AddScoped<TimeUnitPackageDAO>();
            builder.Services.AddScoped<AmenitiesDAO>();
            builder.Services.AddScoped<AccommodationDetailDAO>();
            builder.Services.AddScoped<AccommodationImageDAO>();
            builder.Services.AddScoped<AccommodationAmenityDAO>();
            builder.Services.AddScoped<AccommodationTypeDAO>();
            builder.Services.AddScoped<PostPackageDetailDAO>();
            builder.Services.AddScoped<FavoritePostDAO>();
            builder.Services.AddScoped<AccountDAO>();
            builder.Services.AddScoped<UserProfileDAO>();

            // Repository
            builder.Services.AddScoped<IAccountRepository, AccountRepository>();
            builder.Services.AddScoped<Repositories.Interfaces.IAccommodationRepository, AccommodationRepository>();
            builder.Services.AddScoped<Repositories.Interfaces.IPostRepository, PostRepository>();
            builder.Services.AddScoped<IFavoritePostRepository, FavoritePostRepository>();
            builder.Services.AddScoped<Repositories.Interfaces.IUserProfileRepository, UserProfileRepository>();

            builder.Services.AddScoped<IPackagePricingRepository, PackagePricingRepository>();
            builder.Services.AddScoped<IAmenitiesRepository, AmenitiesRepository>();
            builder.Services.AddScoped<Repositories.Interfaces.IAccommodationTypeRepository, AccommodationTypeRepository>(); // <-- THÊM DÒNG NÀY
            builder.Services.AddScoped<Repositories.Interfaces.ITimeUnitPackageRepository, TimeUnitPackageRepository>();


            // Service
            builder.Services.AddScoped<IPasswordHasherCustom, PasswordHasherCustom>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IAccountService, AccountService>();
            builder.Services.AddScoped<IAccommodationService, AccommodationService>();
            builder.Services.AddScoped<IPostService, PostService>();
            builder.Services.AddScoped<IFavoritePostService, FavoritePostService>();
            builder.Services.AddScoped<IUserProfileService, UserProfileService>();
            // builder.Services.AddScoped<IAzureOpenAIService, AzureOpenAIService>();

            builder.Services.AddScoped<IAzureOpenAIService, AzureOpenAIService>();

            builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

            builder.Services.AddScoped<IPackagePricingService, PackagePricingService>();
            builder.Services.AddScoped<IAmenitiesSerivce, AmenitiesService>();
            builder.Services.AddScoped<IAccommodationTypeService, AccommodationTypeService>(); // <-- THÊM DÒNG NÀY
            builder.Services.AddScoped<ITimeUnitPackageService, TimeUnitPackageService>();
            // ======= CONFIGURATION =======
            // --- JWT Authentication Configuration ---

            builder.Services.AddScoped<IMailService, MailService>();
            // ======= AUTHENTICATION CONFIGURATION (FIXED) =======
            var authSettings = builder.Configuration.GetSection("AuthSettings").Get<AuthSettings>();

            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme);

            builder.Services.AddAuthentication(options =>
            {
                // Đặt JWT làm default cho API authentication
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = AuthSchemes.Cookie; // Cookie cho external login
            })
            // JWT Bearer cho API authentication
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (string.IsNullOrEmpty(context.Token))
                        {
                            context.Token = context.Request.Cookies["accessToken"];
                        }
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                        {
                            context.Response.Headers.Append("Token-Expired", "true");
                        }
                        return Task.CompletedTask;
                    }
                };
            })
            // Cookie authentication cho external login flows
            .AddCookie(AuthSchemes.Cookie, options =>
            {
                options.LoginPath = "/Auth/Login";
                options.AccessDeniedPath = "/Auth/AccessDenied";
                options.ExpireTimeSpan = TimeSpan.FromDays(7);
                options.Cookie.Name = "auth_cookie";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.None;
                options.SlidingExpiration = true;
            })
            // Google authentication
            .AddGoogle(AuthSchemes.Google, options =>
            {
                options.ClientId = authSettings?.Google?.ClientId ?? "";
                options.ClientSecret = authSettings?.Google?.ClientSecret ?? "";
                options.CallbackPath = "/Auth/signIn-google";
                options.SignInScheme = AuthSchemes.Cookie;
            })
            // Facebook authentication
            .AddFacebook(AuthSchemes.Facebook, options =>
            {
                options.AppId = authSettings?.Facebook?.AppId ?? "";
                options.AppSecret = authSettings?.Facebook?.AppSecret ?? "";
                options.CallbackPath = "/Auth/signIn-facebook";
                options.SignInScheme = AuthSchemes.Cookie;
                options.Events = new OAuthEvents
                {
                    OnRemoteFailure = context =>
                    {
                        context.Response.Redirect("/Auth/Login");
                        context.HandleResponse();
                        return Task.CompletedTask;
                    }
                };
            });
            // ======= SESSION =======
            builder.Services.AddDistributedMemoryCache(); // In-memory cache cho phát triển
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(20); // Thời gian sống của session tạm thời cho 2FA
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Luôn dùng HTTPS trong production
                options.Cookie.SameSite = SameSiteMode.None; // Hoặc None nếu client và API khác domain và có cấu hình CORS phù hợp
            });
            // ======= CORS =======
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins("http://localhost:5048", "https://localhost:5048", "https://localhost:7031")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("MailSettings"));
            builder.Services.Configure<AuthSettings>(builder.Configuration.GetSection("AuthSettings"));

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();

                  builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "WebAPI",
                    Version = "v1"
                });

                // Thêm phần hỗ trợ JWT Bearer
                c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Description = "Nhập JWT theo định dạng: Bearer {your token}"
                });

                c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
            });


            builder.Services.AddSwaggerGen();
            builder.Services.AddMemoryCache();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            app.UseStaticFiles();
            app.UseHttpsRedirection();
            app.UseCors("AllowFrontend");
            app.UseSession();
            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
