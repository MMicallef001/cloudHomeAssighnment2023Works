using cloudHomeAssighnment2023.DataAccess;
using Google.Cloud.SecretManager.V1;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace cloudHomeAssighnment2023
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment host)
        {
            Configuration = configuration;
            
            string credential_path = host.ContentRootPath + "/cloudhomeassignment-385408-f736856263bb.json";
            System.Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credential_path);
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();

            string projectId = Configuration["projectid"].ToString();


            var clientId = GetSecretValue("oath_keys", "Authentication:Google:ClientId");
            var secretKey = GetSecretValue("oath_keys", "Authentication:Google:ClientSecret");

            services.Configure<CookiePolicyOptions>(options =>
            {
                options.MinimumSameSitePolicy = SameSiteMode.Unspecified;
                options.OnAppendCookie = cookieContext =>
                    CheckSameSite(cookieContext.Context, cookieContext.CookieOptions);
                options.OnDeleteCookie = cookieContext =>
                    CheckSameSite(cookieContext.Context, cookieContext.CookieOptions);
            });

            services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
            })
            .AddCookie()
            .AddGoogle(options =>
            {
                options.ClientId = clientId;
                options.ClientSecret = secretKey;
            });

            services.AddScoped<FirestoreMoviesRepositary>(provider => new FirestoreMoviesRepositary(projectId));

            services.AddScoped<FirestoreHistoryRepositary>(provider =>new FirestoreHistoryRepositary(projectId));

            services.AddScoped<PubSubTranscriptionRepositary>(provider => new PubSubTranscriptionRepositary(projectId));


        }


        private string GetSecretValue(string nameOfSecret, string key)
        {

            SecretManagerServiceClient client = SecretManagerServiceClient.Create();

            SecretVersionName secretVersionName = new SecretVersionName(Configuration["projectid"].ToString(),
                nameOfSecret,
                "1");

            AccessSecretVersionResponse result = client.AccessSecretVersion(secretVersionName);

            String payload = result.Payload.Data.ToStringUtf8();

            var deserializedKeys = JObject.Parse(payload);
            var secret = deserializedKeys[key].ToString();
            return secret;
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
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseCookiePolicy();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        private void CheckSameSite(HttpContext httpContext, CookieOptions options)
        {
            if (options.SameSite == SameSiteMode.None)
            {
                var userAgent = httpContext.Request.Headers["User-Agent"].ToString();
                // TODO: Use your User Agent library of choice here.
                if (true)
                {
                    // For .NET Core < 3.1 set SameSite = (SameSiteMode)(-1)
                    options.SameSite = SameSiteMode.Unspecified;
                }
            }
        }
    }
}
