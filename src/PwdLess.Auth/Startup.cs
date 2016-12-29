using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PwdLess.Auth.Data;
using Microsoft.EntityFrameworkCore;
using PwdLess.Auth.Services;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace PwdLess.Auth
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

            if (env.IsEnvironment("Development"))
            {
                // This will push telemetry data through Application Insights pipeline faster, allowing you to view results immediately.
                builder.AddApplicationInsightsSettings(developerMode: true);
            }

            builder.AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container
        public void ConfigureServices(IServiceCollection services)
        {
            // Add my services
            services.AddDistributedMemoryCache();
            services.AddSingleton(Configuration);
            services.AddScoped<ISenderService, EmailService>(); // REPLACE WITH EmailService
            services.AddScoped<IAuthService, AuthService>();

            // Add framework services.
            services.AddApplicationInsightsTelemetry(Configuration);

            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();


            app.UseApplicationInsightsRequestTelemetry();

            app.UseApplicationInsightsExceptionTelemetry();


            /* VALIDATING TOKEN TO ENABLE DELETING AND UPDATING USERS
            var tokenSecretKey = Encoding.UTF8.GetBytes(Configuration["PwdLess:Jwt:SecretKey"]);

            var tokenValidationParameters = new TokenValidationParameters
            {
                // Token signature will be verified using a private key.
                ValidateIssuerSigningKey = true,
                RequireSignedTokens = true,
                IssuerSigningKey = new SymmetricSecurityKey(tokenSecretKey),

                // Token will only be valid if contains "accelist.com" for "iss" claim.
                ValidateIssuer = true,
                ValidIssuer = Configuration["PwdLess:Jwt:Issuer"],

                // Token will only be valid if contains "accelist.com" for "aud" claim.
                ValidateAudience = false,

                // Token will only be valid if not expired yet, with 5 minutes clock skew.
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
            */


            app.UseMvc();
        }
    }
}
