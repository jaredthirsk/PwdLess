using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using PwdLess.Services;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using AspNetCoreRateLimit;

namespace PwdLess
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                //.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container
        public void ConfigureServices(IServiceCollection services)
        {
            // PwdLess services
            services.AddDistributedMemoryCache(); // CAN REPLACE WITH AddDistrbutedRedisCache for Redis support
            services.AddSingleton(Configuration);
            services.AddScoped<ISenderService, EmailService>(); // CAN REPLACE WITH ConsoleEmailTestingService
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ITemplateProcessor, EmailTemplateProcessor>();
            services.AddScoped<ICallbackService, CallbackService>();

            // rate-limiting services
            services.Configure<IpRateLimitOptions>(Configuration.GetSection("IpRateLimiting"));
            services.Configure<IpRateLimitPolicies>(Configuration.GetSection("IpRateLimitPolicies"));
            services.AddSingleton<IIpPolicyStore, DistributedCacheIpPolicyStore>();
            services.AddSingleton<IRateLimitCounterStore, DistributedCacheRateLimitCounterStore>();

            // framework services
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseIpRateLimiting();

            #region Optional JWT Validation feature
            var tokenSecretKey = Encoding.UTF8.GetBytes(Configuration["PwdLess:Jwt:SecretKey"]);

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                RequireSignedTokens = true,
                IssuerSigningKey = new SymmetricSecurityKey(tokenSecretKey),
                ValidateIssuer = true,
                ValidIssuer = Configuration["PwdLess:Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = Configuration["PwdLess:Jwt:Audience"],
                ValidateLifetime = true,
                RequireExpirationTime = true,
                ClockSkew = new TimeSpan(0, 5, 0),
                ValidateActor = false,
            };
 
            app.UseJwtBearerAuthentication(new JwtBearerOptions
            {
                AutomaticAuthenticate = true,
                TokenValidationParameters = tokenValidationParameters,
            });
            #endregion

            app.UseMvc();
        }
    }
}
