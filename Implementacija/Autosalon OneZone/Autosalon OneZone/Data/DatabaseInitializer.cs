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
            else
            {
                var profileChanged = false;

                if (!user.EmailConfirmed)
                {
                    user.EmailConfirmed = true;
                    profileChanged = true;
                }

                if (user.Ime != firstName)
                {
                    user.Ime = firstName;
                    profileChanged = true;
                }

                if (user.Prezime != lastName)
                {
                    user.Prezime = lastName;
                    profileChanged = true;
                }

                if (profileChanged)
                {
                    var updateResult = await userManager.UpdateAsync(user);
                    if (!updateResult.Succeeded)
                    {
                        logger.LogError("Failed to update {RoleName} seed user {Email}: {Errors}", roleName, email, FormatErrors(updateResult));
                        return user;
                    }
                }
            }

            await EnsureSeedPasswordAsync(userManager, user, password, roleName, email, logger);

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

        private static async Task EnsureSeedPasswordAsync(
            UserManager<ApplicationUser> userManager,
            ApplicationUser user,
            string password,
            string roleName,
            string email,
            ILogger logger)
        {
            if (await userManager.CheckPasswordAsync(user, password))
            {
                return;
            }

            IdentityResult passwordResult;
            if (await userManager.HasPasswordAsync(user))
            {
                var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
                passwordResult = await userManager.ResetPasswordAsync(user, resetToken, password);
            }
            else
            {
                passwordResult = await userManager.AddPasswordAsync(user, password);
            }

            if (passwordResult.Succeeded)
            {
                logger.LogInformation("Updated password for {RoleName} seed user {Email}.", roleName, email);
                return;
            }

            logger.LogError("Failed to update password for {RoleName} seed user {Email}: {Errors}", roleName, email, FormatErrors(passwordResult));
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
            var seedVehicles = new List<Vozilo>
            {
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
                    Slika = "seed-volkswagen-golf8-2021.jpg",
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
                    Opis = "Premium demo model za prikaz detalja i procesa narudžbe."
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
                    Slika = "seed-audi-a4-2020.jpg",
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
                    Slika = "seed-bmw-x5-2022.jpg",
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
                    Slika = "seed-tesla-model3-2023.jpg",
                    Opis = "Električno demo vozilo za prikaz različitih tipova pogona."
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
                    Slika = "seed-mercedes-c220-2019.jpg",
                    Opis = "Demo sedan za testiranje pretrage i korpe."
                },
                new Vozilo
                {
                    Marka = "Audi",
                    Model = "e-tron GT quattro",
                    Godiste = 2022,
                    Gorivo = TipGoriva.Elektro,
                    Kubikaza = 0m,
                    Boja = "Bijela",
                    Kilometraza = 28400,
                    Cijena = 129500m,
                    Slika = "seed-audi-etron-gt-2022.jpg",
                    Opis = "Električni gran turismo sa naprednom opremom i sportskim karakterom."
                },
                new Vozilo
                {
                    Marka = "BMW",
                    Model = "M4 Competition",
                    Godiste = 2022,
                    Gorivo = TipGoriva.Benzin,
                    Kubikaza = 3.0m,
                    Boja = "Žuta",
                    Kilometraza = 18500,
                    Cijena = 118900m,
                    Slika = "seed-bmw-m4-competition-2022.jpg",
                    Opis = "Sportski coupe sa visokim performansama i upečatljivim izgledom."
                },
                new Vozilo
                {
                    Marka = "Mercedes-Benz",
                    Model = "GLC 300",
                    Godiste = 2020,
                    Gorivo = TipGoriva.Benzin,
                    Kubikaza = 2.0m,
                    Boja = "Bijela",
                    Kilometraza = 66400,
                    Cijena = 56900m,
                    Slika = "seed-mercedes-glc300-2020.jpg",
                    Opis = "Premium SUV za porodičnu i poslovnu vožnju."
                }
            };

            var addedVehicles = 0;
            var updatedVehicles = 0;

            foreach (var vehicle in seedVehicles)
            {
                var existingVehicle = await dbContext.Vozila.FirstOrDefaultAsync(v =>
                    v.Marka == vehicle.Marka &&
                    v.Model == vehicle.Model);

                if (existingVehicle != null)
                {
                    if (existingVehicle.Slika != vehicle.Slika)
                    {
                        existingVehicle.Slika = vehicle.Slika;
                        updatedVehicles++;
                    }

                    continue;
                }

                dbContext.Vozila.Add(vehicle);
                addedVehicles++;
            }

            if (addedVehicles == 0 && updatedVehicles == 0)
            {
                return;
            }

            await dbContext.SaveChangesAsync();
            logger.LogInformation("Seeded {AddedVehicleCount} and updated {UpdatedVehicleCount} demo vehicles.", addedVehicles, updatedVehicles);
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
