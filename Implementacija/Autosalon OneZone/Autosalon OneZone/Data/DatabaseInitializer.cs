using Autosalon_OneZone.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Autosalon_OneZone.Data
{
    public static class DatabaseInitializer
    {
        private const string AdminRole = "Administrator";
        private const string SellerRole = "Prodavac";
        private const string BuyerRole = "Kupac";

        public static async Task InitializeAsync(WebApplication app)
        {
            var configuration = app.Services.GetRequiredService<IConfiguration>();
            var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitializer");

            var applyMigrations = configuration.GetValue("Database:ApplyMigrations", false);
            var seedDemoData = configuration.GetValue("Database:SeedDemoData", false);
            var hasAdminConfig = HasCredentials(configuration, "AdminUserSecrets");

            if (!applyMigrations && !seedDemoData && !hasAdminConfig)
            {
                logger.LogInformation("Database bootstrap skipped because no migration, admin, or demo seed settings are enabled.");
                return;
            }

            await ExecuteWithRetryAsync(
                () => InitializeDatabaseAsync(app.Services, configuration, applyMigrations, seedDemoData, hasAdminConfig, logger),
                logger);
        }

        private static async Task InitializeDatabaseAsync(
            IServiceProvider services,
            IConfiguration configuration,
            bool applyMigrations,
            bool seedDemoData,
            bool hasAdminConfig,
            ILogger logger)
        {
            using var scope = services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            if (applyMigrations)
            {
                logger.LogInformation("Applying database migrations.");
                await dbContext.Database.MigrateAsync();
            }

            await EnsureRolesAsync(roleManager, logger);

            if (hasAdminConfig)
            {
                await EnsureUserAsync(
                    userManager,
                    dbContext,
                    configuration["AdminUserSecrets:Email"]!,
                    configuration["AdminUserSecrets:Password"]!,
                    configuration["AdminUserSecrets:FirstName"] ?? "Demo",
                    configuration["AdminUserSecrets:LastName"] ?? "Admin",
                    AdminRole,
                    logger);
            }

            if (seedDemoData)
            {
                await EnsureUserAsync(
                    userManager,
                    dbContext,
                    configuration["DemoUsers:SellerEmail"] ?? "prodavac@autosalon.local",
                    configuration["DemoUsers:SellerPassword"] ?? "Prodavac123!",
                    "Demo",
                    "Prodavac",
                    SellerRole,
                    logger);

                await EnsureUserAsync(
                    userManager,
                    dbContext,
                    configuration["DemoUsers:BuyerEmail"] ?? "kupac@autosalon.local",
                    configuration["DemoUsers:BuyerPassword"] ?? "Kupac123!",
                    "Demo",
                    "Kupac",
                    BuyerRole,
                    logger);

                await SeedVehiclesAsync(dbContext, logger);
            }
        }

        private static async Task ExecuteWithRetryAsync(Func<Task> action, ILogger logger)
        {
            const int maxAttempts = 30;

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    await action();
                    return;
                }
                catch (Exception ex) when (attempt < maxAttempts)
                {
                    logger.LogWarning(ex, "Database bootstrap failed on attempt {Attempt}/{MaxAttempts}. Retrying in 5 seconds.", attempt, maxAttempts);
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }
            }

            await action();
        }

        private static async Task EnsureRolesAsync(RoleManager<IdentityRole> roleManager, ILogger logger)
        {
            foreach (var roleName in new[] { AdminRole, SellerRole, BuyerRole })
            {
                if (await roleManager.RoleExistsAsync(roleName))
                {
                    continue;
                }

                var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                if (result.Succeeded)
                {
                    logger.LogInformation("Created role {RoleName}.", roleName);
                    continue;
                }

                logger.LogError("Failed to create role {RoleName}: {Errors}", roleName, FormatErrors(result));
            }
        }

        private static async Task<ApplicationUser?> EnsureUserAsync(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext,
            string email,
            string password,
            string firstName,
            string lastName,
            string roleName,
            ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                logger.LogWarning("Skipping {RoleName} seed user because email or password is empty.", roleName);
                return null;
            }

            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    Ime = firstName,
                    Prezime = lastName
                };

                var createResult = await userManager.CreateAsync(user, password);
                if (!createResult.Succeeded)
                {
                    logger.LogError("Failed to create {RoleName} seed user {Email}: {Errors}", roleName, email, FormatErrors(createResult));
                    return null;
                }

                logger.LogInformation("Created {RoleName} seed user {Email}.", roleName, email);
            }

            if (!await userManager.IsInRoleAsync(user, roleName))
            {
                var roleResult = await userManager.AddToRoleAsync(user, roleName);
                if (!roleResult.Succeeded)
                {
                    logger.LogError("Failed to assign {Email} to role {RoleName}: {Errors}", email, roleName, FormatErrors(roleResult));
                    return user;
                }
            }

            if (roleName == BuyerRole)
            {
                await EnsureCartAsync(dbContext, user.Id);
            }

            return user;
        }

        private static async Task EnsureCartAsync(ApplicationDbContext dbContext, string userId)
        {
            if (await dbContext.Korpe.AnyAsync(k => k.KorisnikId == userId))
            {
                return;
            }

            dbContext.Korpe.Add(new Korpa
            {
                KorisnikId = userId,
                UkupnaCijena = 0
            });

            await dbContext.SaveChangesAsync();
        }

        private static async Task SeedVehiclesAsync(ApplicationDbContext dbContext, ILogger logger)
        {
            if (await dbContext.Vozila.AnyAsync())
            {
                return;
            }

            dbContext.Vozila.AddRange(
                new Vozilo
                {
                    Marka = "Volkswagen",
                    Model = "Golf 8",
                    Godiste = 2021,
                    Gorivo = TipGoriva.Benzin,
                    Kubikaza = 1.5m,
                    Boja = "Siva",
                    Kilometraza = 48500,
                    Cijena = 37500m,
                    Slika = "001527a1-f449-42cf-8421-5b861ac9473a_AAGolf 8.jpg",
                    Opis = "Demo vozilo spremno za lokalno pokretanje aplikacije."
                },
                new Vozilo
                {
                    Marka = "Porsche",
                    Model = "Panamera 4 E-Hybrid",
                    Godiste = 2024,
                    Gorivo = TipGoriva.Hibrid,
                    Kubikaza = 2.9m,
                    Boja = "Crna",
                    Kilometraza = 9200,
                    Cijena = 149900m,
                    Slika = "f3ddfedf-b427-40f3-9dc2-236a9251b322_2024-porsche-panamera-4-e-hybrid-108-6643725bab45b.jpeg",
                    Opis = "Premium demo model za prikaz detalja i procesa narudzbe."
                },
                new Vozilo
                {
                    Marka = "Audi",
                    Model = "A4",
                    Godiste = 2020,
                    Gorivo = TipGoriva.Dizel,
                    Kubikaza = 2.0m,
                    Boja = "Bijela",
                    Kilometraza = 76000,
                    Cijena = 43900m,
                    Slika = "04fa33ac-2c64-44d0-82fc-e69437abf1f9_photo_2024-06-08_15-20-20.jpg",
                    Opis = "Pouzdana limuzina za svakodnevnu voznju."
                },
                new Vozilo
                {
                    Marka = "BMW",
                    Model = "X5",
                    Godiste = 2022,
                    Gorivo = TipGoriva.Dizel,
                    Kubikaza = 3.0m,
                    Boja = "Plava",
                    Kilometraza = 38500,
                    Cijena = 89500m,
                    Slika = "1011929f-6ed9-4d2d-8fc1-6bb0ade46462_photo_2024-06-08_15-20-21.jpg",
                    Opis = "SUV demo vozilo sa bogatom opremom."
                },
                new Vozilo
                {
                    Marka = "Tesla",
                    Model = "Model 3",
                    Godiste = 2023,
                    Gorivo = TipGoriva.Elektro,
                    Kubikaza = 0m,
                    Boja = "Crvena",
                    Kilometraza = 21400,
                    Cijena = 72800m,
                    Slika = "15ce749e-99dc-4775-80f4-d8ac9479dae2_photo_2024-06-08_15-20-20.jpg",
                    Opis = "Elektricno demo vozilo za prikaz razlicitih tipova pogona."
                },
                new Vozilo
                {
                    Marka = "Mercedes-Benz",
                    Model = "C 220",
                    Godiste = 2019,
                    Gorivo = TipGoriva.Dizel,
                    Kubikaza = 2.0m,
                    Boja = "Srebrna",
                    Kilometraza = 93000,
                    Cijena = 41500m,
                    Slika = "17c0d7b4-3943-4f50-8c70-4dab8f8e3fd0_photo_2024-06-08_15-20-22.jpg",
                    Opis = "Demo sedan za testiranje pretrage i korpe."
                });

            await dbContext.SaveChangesAsync();
            logger.LogInformation("Seeded demo vehicles.");
        }

        private static bool HasCredentials(IConfiguration configuration, string sectionName)
        {
            return !string.IsNullOrWhiteSpace(configuration[$"{sectionName}:Email"])
                && !string.IsNullOrWhiteSpace(configuration[$"{sectionName}:Password"]);
        }

        private static string FormatErrors(IdentityResult result)
        {
            return string.Join("; ", result.Errors.Select(error => error.Description));
        }
    }
}
