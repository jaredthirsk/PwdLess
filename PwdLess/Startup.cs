using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PwdLess.Data;
using PwdLess.Models;
using PwdLess.Services;
using System.Threading;
using OpenIddict.Core;
using OpenIddict.Models;
using AspNet.Security.OpenIdConnect.Primitives;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using PwdLess.Filters;

namespace PwdLess
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            Configuration = configuration;
            Env = env;
        }

        public IConfiguration Configuration { get; }
        public IHostingEnvironment Env { get; }

        public void ConfigureServices(IServiceCollection services)
        {

            services.AddMvc();

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                if (Env.IsDevelopment())
                    options.UseSqlServer(Configuration["ConnectionStrings:DefaultConnection"]);
                else
                    options.UseSqlServer(Configuration["ConnectionStrings:DefaultConnection"]);

                options.UseOpenIddict();
            });

            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.User.RequireUniqueEmail = false;
            })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            if (!String.IsNullOrWhiteSpace(Configuration["ExternalAuth:Google:ClientId"]))
            {
                services.AddAuthentication().AddGoogle(googleOptions =>
                {
                    googleOptions.ClientId = Configuration["ExternalAuth:Google:ClientId"];
                    googleOptions.ClientSecret = Configuration["ExternalAuth:Google:ClientSecret"];
                });
            }

            services.Configure<IdentityOptions>(options =>
            {
                options.ClaimsIdentity.UserNameClaimType = OpenIdConnectConstants.Claims.Name;
                options.ClaimsIdentity.UserIdClaimType = OpenIdConnectConstants.Claims.Subject;
                options.ClaimsIdentity.RoleClaimType = OpenIdConnectConstants.Claims.Role;
            });

            services.AddOpenIddict(options =>
            {
                options.AddEntityFrameworkCoreStores<ApplicationDbContext>();
                options.AddMvcBinders();
                options.EnableAuthorizationEndpoint("/connect/authorize")
                       .EnableLogoutEndpoint("/connect/logout")
                       .EnableIntrospectionEndpoint("/connect/introspect")
                       .EnableUserinfoEndpoint("/api/userinfo");
                options.AllowImplicitFlow();
                if (Env.IsDevelopment())
                {
                    options.DisableHttpsRequirement();
                    options.AddEphemeralSigningKey();
                }
                else
                {
                    options.DisableHttpsRequirement();
                    // Create the CspParameters object and set the key container name used to store the RSA key pair.  
                    CspParameters cp = new CspParameters
                    {
                        KeyContainerName = "PwdLess1"
                    };

                    // Generate a public/private key pair.  
                    RSACryptoServiceProvider RSA = new RSACryptoServiceProvider(2048, cp);

                    // Save the public key information to an RSAParameters structure.  
                    RSAParameters RSAKeyInfo = RSA.ExportParameters(true);

                    options.AddSigningKey(new RsaSecurityKey(RSAKeyInfo));

                    // TODO research X509 & RSA Keys
                }
                options.UseJsonWebTokens();
            });

            services.AddAuthentication(options =>
            {
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            });

            //services.AddCors();

            if (Env.IsDevelopment())
            {
                services.AddTransient<IEmailSender, DevMessageSender>();
                services.AddTransient<ISmsSender, DevMessageSender>();
            }
            else
            {
                services.AddTransient<IEmailSender, MailKitMessageSender>();
            }

            services.AddScoped<ValidateRecaptchaAttribute>();

            services.AddTransient<NoticeService>();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });

            // TODO refactor & make customizable
            InitializeAsync(app.ApplicationServices, CancellationToken.None).GetAwaiter().GetResult();
        }


        private async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken)
        {
            using (var scope = services.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await context.Database.EnsureCreatedAsync();

                var manager = scope.ServiceProvider.GetRequiredService<OpenIddictApplicationManager<OpenIddictApplication>>();

                if (await manager.FindByClientIdAsync("client-app", cancellationToken) == null)
                {
                    var descriptor = new OpenIddictApplicationDescriptor
                    {
                        ClientId = "client-app",
                        DisplayName = "Client App",
                        PostLogoutRedirectUris = { new Uri("http://localhost:8000/signout-oidc") },
                        RedirectUris = { new Uri("http://localhost:8000/signin-oidc") },
                    };

                    await manager.CreateAsync(descriptor, cancellationToken);
                }
            }
        }
    }
}
