using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using TasnimBook.Data.DataSource;
using TasnimBook.Data.Interfaces;
using TasnimBook.Data.Services;
using System;
using System.IO;
using System.Text;
using System.Threading.RateLimiting;
using TasnimBook.Data;
using webapicore.Services;
using webapicore.Fillter;

namespace webapicore
{
    public class Startup
    {
        public static IConfigurationRoot configuration;
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Build configuration
            configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
                .AddJsonFile("appsettings.json", false)
                .Build();

            services.Configure<JwtConfig>(configuration.GetSection("JwtConfig"));

            // Add access to generic IConfigurationRoot
            services.AddSingleton<IConfigurationRoot>(configuration);

            //services.AddDbContext<DbContextAnti>(options => options.UseMySql(configuration["ConnectionStrings:onlinemysql"],
            //    ServerVersion.AutoDetect(Configuration["ConnectionStrings:onlinemysql"]), b => b.MigrationsAssembly("webapicore")));
            services.AddDbContext<DbContextAnti>(options => options.UseSqlServer(Configuration["ConnectionStrings:local"]));
            
            var key = Encoding.UTF8.GetBytes(Configuration["jwtConfig:Secret"]);
            var jwtValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = Configuration["jwtConfig:Issuer"],
                ValidateAudience = true,
                ValidAudience = Configuration["jwtConfig:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1),
                RequireExpirationTime = true,
                NameClaimType = System.Security.Claims.ClaimTypes.Name,
                RoleClaimType = System.Security.Claims.ClaimTypes.Role
            };
           
            services.AddSingleton(jwtValidationParameters);
            services.AddAuthentication(option =>
            {
                option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                option.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

            })
                .AddJwtBearer(jwt =>
                {
                    jwt.SaveToken = true;
                    jwt.TokenValidationParameters = jwtValidationParameters;
                });
            //services.AddDefaultIdentity<Users>(option => option.SignIn.RequireConfirmedAccount = true)
            //    .AddEntityFrameworkStores<DbContextAnti>();

            //services.AddDbContext<DbContextAnti>(options => options.UseMySql(Configuration["ConnectionStrings:localmysql"], ServerVersion.AutoDetect(Configuration["ConnectionStrings:localmysql"])));
            services.AddControllersWithViews()
           .AddNewtonsoftJson(options =>
           options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
               );
            services.AddMvc()
    .AddNewtonsoftJson(
        options => options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
    );

            services.AddScoped<IBooksRepository, SQLBooksRepository>();
            services.AddScoped<ICategoryRepository, SQLCatRepository>();
            services.AddScoped<IAuthorRepository, SQLAuthorsRepository>();
            services.AddScoped<IAppVersionReository, SQLAppVersionRepository>();
            services.AddScoped<ITokenRepository, SQLTokenRepository>();
            services.AddScoped<IUserAccountRepository, SQLUserAccountRepository>();
            services.AddScoped<IAccountTokenService, AccountTokenService>();
            services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
            services.Configure<PasswordHasherOptions>(options => options.IterationCount = 210_000);
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.AddPolicy("auth", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 10,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0,
                            AutoReplenishment = true
                        }));
            });
            services.AddControllers();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }
            app.UseStaticFiles();

            app.UseHttpsRedirection();
            app.UseDefaultFiles();

            app.UseRouting();

            app.UseRateLimiter();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseStatusCodePagesWithReExecute("/error/{0}");
            app.UseExceptionHandler("/error/500");
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });




        }
    }
}
