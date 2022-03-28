using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Entities;
using IdentityServer4.EntityFramework.Mappers;
using Identity.Api.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
using Identity.Api.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using IdentityModel;
using Microsoft.Extensions.Logging;
using Identity.Api.Migrations.ApplicationDb;

namespace Identity.Api.Data
{
    public class SeedData
    {
        public static void EnsureSeedData(IConfiguration configuration, ILogger Log)
        {
            var connectionString = configuration["ConnectionString"];
            var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<ApplicationDbContext>(options =>
               options.UseSqlServer(connectionString,
                    sqlServerOptionsAction: sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly(migrationsAssembly);
                        //Configuring Connection Resiliency: https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency 
                        sqlOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                    }));

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            using (var serviceProvider = services.BuildServiceProvider())
            {
                using (var scope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    context.Database.Migrate();

                    var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                    var user1 = userMgr.FindByNameAsync("ajalil051983@gmail.com").Result;
                    if (user1 == null)
                    {
                        user1 = new ApplicationUser
                        {
                            Email = "ajalil051983@gmail.com",
                            LastName = "EL",
                            Name = "Jalil",
                            PhoneNumber = "1234567890",
                            UserName = "ajalil051983@gmail.com",
                            Status = true,
                            CabinetId = "EA-0000001",
                            NormalizedEmail = "ajalil051983@gmail.com",
                            NormalizedUserName = "ajalil051983@gmail.com",
                            SecurityStamp = Guid.NewGuid().ToString("D"),
                            Address = "adresse de test",
                            SpecialtyId = "Généraliste",
                            CityId = "Rabat",
                            Latitude = "33.966576",
                            Longitude = "-6.904013",
                            EmailConfirmed = true,
                        };
                        var result = userMgr.CreateAsync(user1, "Mot@Pass1").Result;
                        if (!result.Succeeded)
                        {
                            throw new Exception(result.Errors.First().Description);
                        }

                        result = userMgr.AddClaimsAsync(user1, new Claim[]{
                            new Claim(JwtClaimTypes.Name, $"{user1.LastName}{user1.Name}"),
                            new Claim(JwtClaimTypes.GivenName, user1.Name),
                            new Claim(JwtClaimTypes.FamilyName, user1.LastName),
                            new Claim(JwtClaimTypes.WebSite, ""),
                        }).Result;
                        if (!result.Succeeded)
                        {
                            throw new Exception(result.Errors.First().Description);
                        }
                        Log.LogDebug("alice created");
                    }
                    else
                    {
                        Log.LogDebug("ajalil051983@gmail.com already exists");
                    }

                    var bob = userMgr.FindByNameAsync("bob").Result;
                    if (bob == null)
                    {
                        bob = new ApplicationUser
                        {
                            UserName = "bob",
                            Email = "BobSmith@email.com",
                            EmailConfirmed = true,
                            LastName = "dodo",
                            Name = "bob",
                            Status = true,
                            CabinetId = "EA-0000001",
                            SecurityStamp = Guid.NewGuid().ToString("D"),
                            Address = "adresse de test bob",
                            SpecialtyId = "Généraliste",
                            CityId = "Rabat",
                            Latitude = "33.906576",
                            Longitude = "-6.104013",
                        };
                        var result = userMgr.CreateAsync(bob, "Pass123$").Result;
                        if (!result.Succeeded)
                        {
                            throw new Exception(result.Errors.First().Description);
                        }

                        result = userMgr.AddClaimsAsync(bob, new Claim[]{
                            new Claim(JwtClaimTypes.Name, "Bob Smith"),
                            new Claim(JwtClaimTypes.GivenName, "Bob"),
                            new Claim(JwtClaimTypes.FamilyName, "Smith"),
                            new Claim(JwtClaimTypes.WebSite, "http://bob.com"),
                            new Claim("location", "somewhere")
                        }).Result;
                        if (!result.Succeeded)
                        {
                            throw new Exception(result.Errors.First().Description);
                        }
                        Log.LogDebug("bob created");
                    }
                    else
                    {
                        Log.LogDebug("bob already exists");
                    }                    
                }
            }
        }
    }
}
