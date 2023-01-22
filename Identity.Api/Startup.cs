using System;
using System.Reflection;
using HealthChecks.UI.Client;
using IdentityServer4.Services;
using Identity.Api.Data;
using Identity.Api.Devspaces;
using Identity.Api.Models;
using Identity.Api.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Identity.Api.Migrations.ApplicationDb;
using IdentityServer4.EntityFramework.DbContexts;
using System.Collections.Generic;
using System.Linq;
using Identity.Api.Configuration;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.EntityFramework.Entities;

namespace Identity.Api
{
    public class Startup
    {
        public Startup(IConfigurationRoot configuration)
        {
            Configuration = configuration;
        }
        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;
            var connectionString = Configuration["ConnectionString"];
            RegisterAppInsights(services);

            // Add framework services.
            services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlServer(connectionString,
                    sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly(migrationsAssembly);
                        //Configuring Connection Resiliency: https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency 
                        sqlOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                    }));

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            services.Configure<AppSettings>(Configuration);

            //if (Configuration.GetValue<string>("IsClusterEnv") == bool.TrueString)
            //{
            //    services.AddDataProtection(opts =>
            //    {
            //        opts.ApplicationDiscriminator = "eshop.identity";
            //    })
            //    .PersistKeysToRedis(ConnectionMultiplexer.Connect(Configuration["DPConnectionString"]), "DataProtection-Keys");
            //}

            services.AddHealthChecks()
                .AddCheck("self", () => HealthCheckResult.Healthy())
                .AddSqlServer(Configuration["ConnectionString"],
                    name: "IdentityDB-check",
                    tags: new string[] { "IdentityDB" });

            services.AddTransient<ILoginService<ApplicationUser>, EFLoginService>();
            services.AddTransient<IRedirectService, RedirectService>();


            // Adds IdentityServer
            services.AddIdentityServer(x =>
            {
                x.IssuerUri = "null";
                x.Authentication.CookieLifetime = TimeSpan.FromHours(2);
            })
            .AddDevspacesIfNeeded(Configuration.GetValue("EnableDevspaces", false))
            .AddSigningCredential(Certificate.Certificate.Get())
            .AddConfigurationStore(options =>
            {
                options.ConfigureDbContext = builder => builder.UseSqlServer(connectionString,
                    sqlServerOptionsAction: sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly(migrationsAssembly);
                        //Configuring Connection Resiliency: https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency 
                        sqlOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                    });
            })
            .AddOperationalStore(options =>
            {
                options.ConfigureDbContext = builder => builder.UseSqlServer(connectionString,
                    sqlServerOptionsAction: sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly(migrationsAssembly);
                        //Configuring Connection Resiliency: https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency 
                        sqlOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                    });
            })
            .AddAspNetIdentity<ApplicationUser>()
            .Services.AddTransient<IProfileService, ProfileService>();

            //services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_3_0);
            //services.AddWebOptimizer();
            services.AddWebOptimizer(minifyJavaScript: false, minifyCss: false);
            services.AddControllers();
            services.AddControllersWithViews();
            services.AddRazorPages();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // this will do the initial DB population
            InitializeDatabase(app);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            var pathBase = Configuration["PATH_BASE"];
            if (!string.IsNullOrEmpty(pathBase))
            {
                app.UsePathBase(pathBase);
            }
            app.UseWebOptimizer();
            app.UseStaticFiles();

            // Make work identity server redirections in Edge and lastest versions of browers. WARN: Not valid in a production environment.
            app.Use(async (context, next) =>
            {
                context.Response.Headers.Add("Content-Security-Policy", "script-src 'self'");
                context.Response.Headers.Add("Access-Control-Allow-Origin", "https://nominatim.openstreetmap.org");
                await next();
            });

            app.UseForwardedHeaders();
            // Adds IdentityServer
            app.UseIdentityServer();

            // Fix a problem with chrome. Chrome enabled a new feature "Cookies without SameSite must be secure", 
            // the coockies shold be expided from https, but in eShop, the internal comunicacion in aks and docker compose is http.
            // To avoid this problem, the policy of cookies shold be in Lax mode.
            app.UseCookiePolicy(new CookiePolicyOptions { MinimumSameSitePolicy = Microsoft.AspNetCore.Http.SameSiteMode.Lax });
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/hc", new HealthCheckOptions()
                {
                    Predicate = _ => true,
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });
                endpoints.MapHealthChecks("/liveness", new HealthCheckOptions
                {
                    Predicate = r => r.Name.Contains("self")
                });
            });
        }

        private void RegisterAppInsights(IServiceCollection services)
        {
            services.AddApplicationInsightsTelemetry(Configuration);
            services.AddApplicationInsightsKubernetesEnricher();
        }

        private void InitializeDatabase(IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope();
            scope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>().Database.Migrate();
            var configContext = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
            configContext.Database.Migrate();
            //callbacks urls from config:
            var clientUrls = new Dictionary<string, string>
            {
                { "blazorWeb", Configuration.GetValue<string>("blazorClient") }
            };

            if (!configContext.Clients.Any())
            {
                foreach (var client in Config.GetClients(clientUrls))
                {
                    configContext.Clients.Add(client.ToEntity());
                }
                configContext.SaveChangesAsync();
            }

            if (!configContext.IdentityResources.Any())
            {
                foreach (var resource in Config.IdentityResources)
                {
                    configContext.IdentityResources.Add(resource.ToEntity());
                }
                configContext.SaveChanges();
            }

            if (!configContext.ApiScopes.Any())
            {
                foreach (var resource in Config.ApiScopes)
                {
                    configContext.ApiScopes.Add(resource.ToEntity());
                }
                configContext.SaveChanges();
            }
        }
    }
}
