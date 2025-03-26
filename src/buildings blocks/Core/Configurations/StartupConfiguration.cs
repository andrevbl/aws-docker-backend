using MyProject.Core.DomainObjects;
using MyProject.Infra.CrossCutting.Filters;
using MyProject.Template.BLL.Mail;
using MyProject.Template.BLL.Util;
using MyProject.WebApi.Core.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Logging;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace MyProject.WebApi.Core.Configurations
{
    public class StartupConfiguration
    {
        public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            X509Certificate2 cert = null;
            bool _useLocalCertStore = Convert.ToBoolean(configuration["UseLocalCertStore"]);

            if (_useLocalCertStore)
            {
                Console.WriteLine($"Log: Getting certificate on: {Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Certificates/docker.myproject.pfx")}");
                cert = new X509Certificate2(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Certificates/docker.myproject.pfx"), "", X509KeyStorageFlags.PersistKeySet);
                Console.WriteLine($"Log: Loaded certificate file: {cert.Issuer + " // " + (cert.HasPrivateKey ? "Has PK" : "No PK")}");
            }
            else Console.WriteLine("Log: NOT using a local certificate store");

            services.AddSingleton(configuration);
            services.AddSingleton(configuration.GetSection("WCFCredential").Get<WCFCredential>());

            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
            });
                        
            services.AddHttpContextAccessor();
            services.AddMvc(options => { options.Filters.Add<GlobalActionFilter>(); });
            services.AddControllers();
            services.AddControllersWithViews();
            services.TryAddScoped<ViewRenderService>();
            services.Configure<EmailConfig>(configuration.GetSection("Email"));
            services.AddScoped<EmailService>();
            //services.AddJwtConfiguration(configuration);
        }

        public static void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                IdentityModelEventSource.ShowPII = true;
            }

            //app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseCors(options => options.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
            app.UseAuthConfiguration();
            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
            app.UseStaticFiles();
        }
    }
}
