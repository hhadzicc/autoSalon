using Autosalon_OneZone.Models;
using Autosalon_OneZone.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Autosalon_OneZone.Data
{
    public static class DatabaseInitializer
    {
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

        public static async Task ResetDemoDataAsync(
            IServiceProvider services,
            IConfiguration configuration,
            ILogger logger,
            CancellationToken cancellationToken = default)
        {
            using var scope = services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            var strategy = dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await dbContext.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);

                await dbContext.Recenzije.ExecuteDeleteAsync(cancellationToken);
                await dbContext.PodrskaUpiti.ExecuteDeleteAsync(cancellationToken);
                await dbContext.Placanja.ExecuteDeleteAsync(cancellationToken);
                await dbContext.StavkeKorpe.ExecuteDeleteAsync(cancellationToken);
                await dbContext.Narudzbe.ExecuteDeleteAsync(cancellationToken);
                await dbContext.Kartice.ExecuteDeleteAsync(cancellationToken);
                await dbContext.Krediti.ExecuteDeleteAsync(cancellationToken);
                await dbContext.Korpe.ExecuteUpdateAsync(
                    setters => setters.SetProperty(cart => cart.UkupnaCijena, 0m),
                    cancellationToken);
                await dbContext.Vozila.ExecuteDeleteAsync(cancellationToken);

                await EnsureRolesAsync(roleManager, logger);

                ApplicationUser? adminUser = null;
                if (HasCredentials(configuration, "AdminUserSecrets"))
                {
                    adminUser = await EnsureUserAsync(
                        userManager,
                        dbContext,
                        configuration["AdminUserSecrets:Email"]!,
                        configuration["AdminUserSecrets:Password"]!,
                        configuration["AdminUserSecrets:FirstName"] ?? "Demo",
                        configuration["AdminUserSecrets:LastName"] ?? "Admin",
                        AppRoles.Administrator,
                        logger);
                }

                var sellerUser = await EnsureUserAsync(
                    userManager,
                    dbContext,
                    configuration["DemoUsers:SellerEmail"] ?? "prodavac@autosalon.local",
                    configuration["DemoUsers:SellerPassword"] ?? "Prodavac123!",
                    "Demo",
                    "Prodavac",
                    AppRoles.Seller,
                    logger);

                var buyerUser = await EnsureUserAsync(
                    userManager,
                    dbContext,
                    configuration["DemoUsers:BuyerEmail"] ?? "kupac@autosalon.local",
                    configuration["DemoUsers:BuyerPassword"] ?? "Kupac123!",
                    "Demo",
                    "Kupac",
                    AppRoles.Buyer,
                    logger);

                await SeedVehiclesAsync(dbContext, logger);
                await SeedReviewsAndSupportAsync(dbContext, adminUser, sellerUser, buyerUser, logger);
                await transaction.CommitAsync(cancellationToken);
            });

            logger.LogInformation("Demo data reset completed.");
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

            ApplicationUser? adminUser = null;
            ApplicationUser? sellerUser = null;
            ApplicationUser? buyerUser = null;

            if (hasAdminConfig)
            {
                adminUser = await EnsureUserAsync(
                    userManager,
                    dbContext,
                    configuration["AdminUserSecrets:Email"]!,
                    configuration["AdminUserSecrets:Password"]!,
                    configuration["AdminUserSecrets:FirstName"] ?? "Demo",
                    configuration["AdminUserSecrets:LastName"] ?? "Admin",
                    AppRoles.Administrator,
                    logger);
            }

            if (seedDemoData)
            {
                sellerUser = await EnsureUserAsync(
                    userManager,
                    dbContext,
                    configuration["DemoUsers:SellerEmail"] ?? "prodavac@autosalon.local",
                    configuration["DemoUsers:SellerPassword"] ?? "Prodavac123!",
                    "Demo",
                    "Prodavac",
                    AppRoles.Seller,
                    logger);

                buyerUser = await EnsureUserAsync(
                    userManager,
                    dbContext,
                    configuration["DemoUsers:BuyerEmail"] ?? "kupac@autosalon.local",
                    configuration["DemoUsers:BuyerPassword"] ?? "Kupac123!",
                    "Demo",
                    "Kupac",
                    AppRoles.Buyer,
                    logger);

                await SeedVehiclesAsync(dbContext, logger);
                await SeedReviewsAndSupportAsync(dbContext, adminUser, sellerUser, buyerUser, logger);
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
            foreach (var roleName in AppRoles.All)
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

            var desiredUserName = BuildSeedUserName(email, roleName);
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = desiredUserName,
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

                if (!string.Equals(user.UserName, desiredUserName, StringComparison.Ordinal))
                {
                    var existingUserWithName = await userManager.FindByNameAsync(desiredUserName);
                    if (existingUserWithName == null || existingUserWithName.Id == user.Id)
                    {
                        user.UserName = desiredUserName;
                        profileChanged = true;
                    }
                    else
                    {
                        logger.LogWarning(
                            "Cannot update {RoleName} seed username for {Email} to {UserName} because that username is already taken.",
                            roleName,
                            email,
                            desiredUserName);
                    }
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

            await EnsureExclusiveSeedRoleAsync(userManager, user, roleName, email, logger);

            if (roleName == AppRoles.Buyer)
            {
                await EnsureCartAsync(dbContext, user.Id);
            }

            return user;
        }

        private static async Task EnsureExclusiveSeedRoleAsync(
            UserManager<ApplicationUser> userManager,
            ApplicationUser user,
            string targetRole,
            string email,
            ILogger logger)
        {
            foreach (var roleName in AppRoles.All.Where(role => role != targetRole))
            {
                if (!await userManager.IsInRoleAsync(user, roleName))
                {
                    continue;
                }

                var removeResult = await userManager.RemoveFromRoleAsync(user, roleName);
                if (removeResult.Succeeded)
                {
                    logger.LogInformation("Removed extra demo role {RoleName} from seed user {Email}.", roleName, email);
                    continue;
                }

                logger.LogError("Failed to remove extra demo role {RoleName} from seed user {Email}: {Errors}", roleName, email, FormatErrors(removeResult));
            }
        }

        private static string BuildSeedUserName(string email, string roleName)
        {
            var localPart = email.Split('@', 2)[0];
            var sanitized = new string(localPart.Where(char.IsLetterOrDigit).ToArray());

            if (!string.IsNullOrWhiteSpace(sanitized))
            {
                return sanitized;
            }

            var roleFallback = new string(roleName.Where(char.IsLetterOrDigit).ToArray());
            return string.IsNullOrWhiteSpace(roleFallback) ? "demo" : roleFallback.ToLowerInvariant();
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
                    Boja = TipBoje.Siva,
                    Kilometraza = 48500,
                    Cijena = 37500,
                    Slika = "seed-volkswagen-golf8-2021-showroom.webp",
                    Opis = "Demo vozilo spremno za lokalno pokretanje aplikacije."
                },
                new Vozilo
                {
                    Marka = "Porsche",
                    Model = "Panamera 4 E-Hybrid",
                    Godiste = 2024,
                    Gorivo = TipGoriva.Hibrid,
                    Kubikaza = 2.9m,
                    Boja = TipBoje.Crna,
                    Kilometraza = 9200,
                    Cijena = 149900,
                    Slika = "seed-porsche-panamera-2024-showroom.webp",
                    Opis = "Premium demo model za prikaz detalja i procesa narudžbe."
                },
                new Vozilo
                {
                    Marka = "Audi",
                    Model = "A4",
                    Godiste = 2020,
                    Gorivo = TipGoriva.Dizel,
                    Kubikaza = 2.0m,
                    Boja = TipBoje.Bijela,
                    Kilometraza = 76000,
                    Cijena = 43900,
                    Slika = "seed-audi-a4-2020-showroom.webp",
                    Opis = "Pouzdana limuzina za svakodnevnu vožnju."
                },
                new Vozilo
                {
                    Marka = "BMW",
                    Model = "X5",
                    Godiste = 2022,
                    Gorivo = TipGoriva.Dizel,
                    Kubikaza = 3.0m,
                    Boja = TipBoje.Plava,
                    Kilometraza = 38500,
                    Cijena = 89500,
                    Slika = "seed-bmw-x5-2022-showroom.webp",
                    Opis = "SUV demo vozilo sa bogatom opremom."
                },
                new Vozilo
                {
                    Marka = "Tesla",
                    Model = "Model 3",
                    Godiste = 2023,
                    Gorivo = TipGoriva.Elektro,
                    Kubikaza = null,
                    Boja = TipBoje.Crvena,
                    Kilometraza = 21400,
                    Cijena = 72800,
                    Slika = "seed-tesla-model3-2023-showroom.webp",
                    Opis = "Električno demo vozilo za prikaz različitih tipova pogona."
                },
                new Vozilo
                {
                    Marka = "Mercedes-Benz",
                    Model = "C 220",
                    Godiste = 2019,
                    Gorivo = TipGoriva.Dizel,
                    Kubikaza = 2.0m,
                    Boja = TipBoje.Srebrna,
                    Kilometraza = 93000,
                    Cijena = 41500,
                    Slika = "seed-mercedes-c220-2019-showroom.webp",
                    Opis = "Demo sedan za testiranje pretrage i korpe."
                },
                new Vozilo
                {
                    Marka = "Audi",
                    Model = "e-tron GT quattro",
                    Godiste = 2022,
                    Gorivo = TipGoriva.Elektro,
                    Kubikaza = null,
                    Boja = TipBoje.Bijela,
                    Kilometraza = 28400,
                    Cijena = 129500,
                    Slika = "seed-audi-etron-gt-2022-showroom.webp",
                    Opis = "Električni gran turismo sa naprednom opremom i sportskim karakterom."
                },
                new Vozilo
                {
                    Marka = "BMW",
                    Model = "M4 Competition",
                    Godiste = 2022,
                    Gorivo = TipGoriva.Benzin,
                    Kubikaza = 3.0m,
                    Boja = TipBoje.Zuta,
                    Kilometraza = 18500,
                    Cijena = 118900,
                    Slika = "seed-bmw-m4-competition-2022-showroom.webp",
                    Opis = "Sportski coupe sa visokim performansama i upečatljivim izgledom."
                },
                new Vozilo
                {
                    Marka = "Mercedes-Benz",
                    Model = "GLC 300",
                    Godiste = 2020,
                    Gorivo = TipGoriva.Benzin,
                    Kubikaza = 2.0m,
                    Boja = TipBoje.Bijela,
                    Kilometraza = 66400,
                    Cijena = 56900,
                    Slika = "seed-mercedes-glc300-2020-showroom.webp",
                    Opis = "Premium SUV za porodičnu i poslovnu vožnju."
                },
                new Vozilo
                {
                    Marka = "Toyota",
                    Model = "Yaris",
                    Godiste = 2017,
                    Gorivo = TipGoriva.Benzin,
                    Kubikaza = 1.3m,
                    Boja = TipBoje.Crvena,
                    Kilometraza = 112000,
                    Cijena = 16900,
                    Slika = "seed-toyota-yaris-2017-showroom.webp",
                    Opis = "Pristupačno gradsko vozilo sa niskom potrošnjom."
                },
                new Vozilo
                {
                    Marka = "Renault",
                    Model = "Clio",
                    Godiste = 2018,
                    Gorivo = TipGoriva.Dizel,
                    Kubikaza = 1.5m,
                    Boja = TipBoje.Bijela,
                    Kilometraza = 98500,
                    Cijena = 14500,
                    Slika = "seed-renault-clio-2018-showroom.webp",
                    Opis = "Ekonomično vozilo za svakodnevnu gradsku vožnju."
                },
                new Vozilo
                {
                    Marka = "Opel",
                    Model = "Astra",
                    Godiste = 2016,
                    Gorivo = TipGoriva.Benzin,
                    Kubikaza = 1.4m,
                    Boja = TipBoje.Siva,
                    Kilometraza = 124000,
                    Cijena = 13500,
                    Slika = "seed-opel-astra-2016-showroom.webp",
                    Opis = "Pouzdan hatchback za kupce koji traže niži budžet."
                },
                new Vozilo
                {
                    Marka = "Ford",
                    Model = "Fiesta",
                    Godiste = 2017,
                    Gorivo = TipGoriva.Benzin,
                    Kubikaza = 1.25m,
                    Boja = TipBoje.Plava,
                    Kilometraza = 108000,
                    Cijena = 11900,
                    Slika = "seed-ford-fiesta-2017-showroom.webp",
                    Opis = "Kompaktno vozilo za gradsku vožnju i početnike."
                },
                new Vozilo
                {
                    Marka = "Hyundai",
                    Model = "i20",
                    Godiste = 2019,
                    Gorivo = TipGoriva.Benzin,
                    Kubikaza = 1.2m,
                    Boja = TipBoje.Srebrna,
                    Kilometraza = 72000,
                    Cijena = 15500,
                    Slika = "seed-hyundai-i20-2019-showroom.webp",
                    Opis = "Dobro očuvano vozilo sa jednostavnim održavanjem."
                },
                new Vozilo
                {
                    Marka = "Dacia",
                    Model = "Sandero",
                    Godiste = 2020,
                    Gorivo = TipGoriva.Plin,
                    Kubikaza = 1.0m,
                    Boja = TipBoje.Bijela,
                    Kilometraza = 68000,
                    Cijena = 12900,
                    Slika = "seed-dacia-sandero-2020-showroom.webp",
                    Opis = "Povoljan demo model sa plinskim pogonom."
                },
                new Vozilo
                {
                    Marka = "Lexus",
                    Model = "LC 500",
                    Godiste = 2021,
                    Gorivo = TipGoriva.Benzin,
                    Kubikaza = 5.0m,
                    Boja = TipBoje.Bijela,
                    Kilometraza = 18500,
                    Cijena = 109900,
                    Slika = "seed-lexus-lc500-2021-showroom.webp",
                    Opis = "Luksuzni grand tourer sa atmosferskim V8 motorom i prepoznatljivim dizajnom."
                },
                new Vozilo
                {
                    Marka = "Jaguar",
                    Model = "F-Type R",
                    Godiste = 2021,
                    Gorivo = TipGoriva.Benzin,
                    Kubikaza = 5.0m,
                    Boja = TipBoje.Crvena,
                    Kilometraza = 22800,
                    Cijena = 114500,
                    Slika = "seed-jaguar-ftype-r-2021-showroom.webp",
                    Opis = "Sportski coupe sa snažnim V8 motorom i elegantnim britanskim karakterom."
                },
                new Vozilo
                {
                    Marka = "Porsche",
                    Model = "Taycan 4S",
                    Godiste = 2021,
                    Gorivo = TipGoriva.Elektro,
                    Kubikaza = null,
                    Boja = TipBoje.Plava,
                    Kilometraza = 31000,
                    Cijena = 103900,
                    Slika = "seed-porsche-taycan-4s-2021-showroom.webp",
                    Opis = "Električna sportska limuzina sa pogonom na sve točkove i odličnim voznim osobinama."
                },
                new Vozilo
                {
                    Marka = "Land Rover",
                    Model = "Defender 110 P400",
                    Godiste = 2021,
                    Gorivo = TipGoriva.Hibrid,
                    Kubikaza = 3.0m,
                    Boja = TipBoje.Zelena,
                    Kilometraza = 42000,
                    Cijena = 92900,
                    Slika = "seed-land-rover-defender-p400-2021-showroom.webp",
                    Opis = "Premium terensko vozilo sa blagim hibridnim pogonom i prostranom kabinom."
                },
                new Vozilo
                {
                    Marka = "Range Rover",
                    Model = "Sport P400e",
                    Godiste = 2022,
                    Gorivo = TipGoriva.Hibrid,
                    Kubikaza = 2.0m,
                    Boja = TipBoje.Crna,
                    Kilometraza = 36000,
                    Cijena = 104900,
                    Slika = "seed-range-rover-sport-p400e-2022-showroom.webp",
                    Opis = "Luksuzni plug-in hibridni SUV koji spaja udobnost, performanse i svakodnevnu praktičnost."
                },
                new Vozilo
                {
                    Marka = "Volvo",
                    Model = "XC90 Recharge T8",
                    Godiste = 2022,
                    Gorivo = TipGoriva.Hibrid,
                    Kubikaza = 2.0m,
                    Boja = TipBoje.Bijela,
                    Kilometraza = 45500,
                    Cijena = 84900,
                    Slika = "seed-volvo-xc90-recharge-2022-showroom.webp",
                    Opis = "Prostrani plug-in hibridni SUV sa bogatom sigurnosnom opremom i sedam sjedišta."
                },
                new Vozilo
                {
                    Marka = "Genesis",
                    Model = "G80 3.5T AWD",
                    Godiste = 2022,
                    Gorivo = TipGoriva.Benzin,
                    Kubikaza = 3.5m,
                    Boja = TipBoje.Plava,
                    Kilometraza = 29000,
                    Cijena = 69900,
                    Slika = "seed-genesis-g80-2022-showroom.webp",
                    Opis = "Elegantna premium limuzina sa pogonom na sve točkove i visokim nivoom udobnosti."
                },
                new Vozilo
                {
                    Marka = "Maserati",
                    Model = "Ghibli Trofeo",
                    Godiste = 2021,
                    Gorivo = TipGoriva.Benzin,
                    Kubikaza = 3.8m,
                    Boja = TipBoje.Crna,
                    Kilometraza = 33500,
                    Cijena = 98900,
                    Slika = "seed-maserati-ghibli-trofeo-2021-showroom.webp",
                    Opis = "Sportska luksuzna limuzina sa V8 motorom i izraženim italijanskim karakterom."
                },
                new Vozilo
                {
                    Marka = "Audi",
                    Model = "RS 6 Avant",
                    Godiste = 2021,
                    Gorivo = TipGoriva.Benzin,
                    Kubikaza = 4.0m,
                    Boja = TipBoje.Siva,
                    Kilometraza = 27500,
                    Cijena = 139900,
                    Slika = "seed-audi-rs6-avant-2021-showroom.webp",
                    Opis = "Karavan visokih performansi koji kombinuje prostranost, luksuz i sportski pogon."
                },
                new Vozilo
                {
                    Marka = "Mercedes-AMG",
                    Model = "GT 53 4MATIC+",
                    Godiste = 2022,
                    Gorivo = TipGoriva.Hibrid,
                    Kubikaza = 3.0m,
                    Boja = TipBoje.Crna,
                    Kilometraza = 24000,
                    Cijena = 124900,
                    Slika = "seed-mercedes-amg-gt53-2022-showroom.webp",
                    Opis = "Četverovratni sportski coupe sa blagim hibridnim sistemom i pogonom na sve točkove."
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
                    if (ApplySeedVehicleUpdates(existingVehicle, vehicle))
                    {
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

        private static bool ApplySeedVehicleUpdates(Vozilo existingVehicle, Vozilo seedVehicle)
        {
            var changed = false;

            if (existingVehicle.Godiste != seedVehicle.Godiste)
            {
                existingVehicle.Godiste = seedVehicle.Godiste;
                changed = true;
            }

            if (existingVehicle.Gorivo != seedVehicle.Gorivo)
            {
                existingVehicle.Gorivo = seedVehicle.Gorivo;
                changed = true;
            }

            if (existingVehicle.Kubikaza != seedVehicle.Kubikaza)
            {
                existingVehicle.Kubikaza = seedVehicle.Kubikaza;
                changed = true;
            }

            if (existingVehicle.Boja != seedVehicle.Boja)
            {
                existingVehicle.Boja = seedVehicle.Boja;
                changed = true;
            }

            if (existingVehicle.Kilometraza != seedVehicle.Kilometraza)
            {
                existingVehicle.Kilometraza = seedVehicle.Kilometraza;
                changed = true;
            }

            if (existingVehicle.Cijena != seedVehicle.Cijena)
            {
                existingVehicle.Cijena = seedVehicle.Cijena;
                changed = true;
            }

            if (existingVehicle.Slika != seedVehicle.Slika)
            {
                existingVehicle.Slika = seedVehicle.Slika;
                changed = true;
            }

            if (existingVehicle.Opis != seedVehicle.Opis)
            {
                existingVehicle.Opis = seedVehicle.Opis;
                changed = true;
            }

            return changed;
        }

        private static async Task SeedReviewsAndSupportAsync(
            ApplicationDbContext dbContext,
            ApplicationUser? adminUser,
            ApplicationUser? sellerUser,
            ApplicationUser? buyerUser,
            ILogger logger)
        {
            var reviewUsers = new[] { buyerUser, sellerUser, adminUser }
                .Where(user => user != null)
                .Cast<ApplicationUser>()
                .ToArray();

            if (reviewUsers.Length == 0)
            {
                logger.LogWarning("Skipping demo reviews and support inquiries because no seed users are available.");
                return;
            }

            var vehicles = await dbContext.Vozila.ToListAsync();
            if (vehicles.Count == 0)
            {
                logger.LogWarning("Skipping demo reviews because no vehicles are available.");
                return;
            }

            var purchaseUser = buyerUser ?? reviewUsers[0];
            var vehicleByName = vehicles
                .OrderBy(vehicle => vehicle.VoziloID)
                .GroupBy(
                    vehicle => $"{vehicle.Marka} {vehicle.Model}",
                    StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.First(),
                    StringComparer.OrdinalIgnoreCase);

            var demoReviews = new[]
            {
                new { Vehicle = "Lexus LC 500", Rating = 5, PurchaseDaysAgo = 150, ReviewDelayDays = 6, LegacyComment = "Izuzetno udobno i brzo vozilo, cijeli proces kupovine je bio profesionalan.", Comment = "V8 motor, udobnost i završna obrada ostavili su odličan utisak. Kupovina je protekla profesionalno." },
                new { Vehicle = "Jaguar F-Type R", Rating = 5, PurchaseDaysAgo = 137, ReviewDelayDays = 4, LegacyComment = "Golf je pregledan, uredan i dobar za svakodnevnu vožnju.", Comment = "Odličan sportski automobil sa upečatljivim zvukom i preciznim upravljanjem." },
                new { Vehicle = "Porsche Taycan 4S", Rating = 5, PurchaseDaysAgo = 124, ReviewDelayDays = 8, LegacyComment = "Odličan balans cijene, opreme i potrošnje. Preporuka za porodičnu vožnju.", Comment = "Taycan je izuzetno tih, brz i stabilan, a kvalitet enterijera je na očekivanom premium nivou." },
                new { Vehicle = "Land Rover Defender 110 P400", Rating = 4, PurchaseDaysAgo = 111, ReviewDelayDays = 5, LegacyComment = "Prostran SUV, dobra oprema i veoma stabilan na otvorenoj cesti.", Comment = "Prostran, udoban i siguran na dužim putovanjima. Posebno mi se dopada preglednost iz kabine." },
                new { Vehicle = "Range Rover Sport P400e", Rating = 5, PurchaseDaysAgo = 98, ReviewDelayDays = 7, LegacyComment = "Elektricni pogon je tih i brz, autonomija je odlicna za svakodnevnu upotrebu.", Comment = "Hibridni pogon je tih u gradu, a vozilo ostaje snažno i vrlo udobno na otvorenoj cesti." },
                new { Vehicle = "Volvo XC90 Recharge T8", Rating = 5, PurchaseDaysAgo = 85, ReviewDelayDays = 3, LegacyComment = "Pouzdana limuzina, udobna i ekonomična za duže relacije.", Comment = "Odličan porodični SUV sa mnogo prostora, kvalitetnim sjedištima i uvjerljivim sigurnosnim sistemima." },
                new { Vehicle = "Genesis G80 3.5T AWD", Rating = 4, PurchaseDaysAgo = 72, ReviewDelayDays = 6, LegacyComment = "Vrhunski izgled i performanse, auto ostavlja premium utisak.", Comment = "Vrlo mirna i udobna vožnja, kvalitetna kabina i bogata oprema za ovu cjenovnu klasu." },
                new { Vehicle = "Maserati Ghibli Trofeo", Rating = 4, PurchaseDaysAgo = 59, ReviewDelayDays = 5, LegacyComment = "Povoljan i praktičan izbor za gradsku vožnju.", Comment = "Karakteran automobil sa snažnim motorom i odličnim osjećajem za volanom." },
                new { Vehicle = "Audi RS 6 Avant", Rating = 5, PurchaseDaysAgo = 46, ReviewDelayDays = 4, LegacyComment = "Dobar budžet auto, ekonomičan i jednostavan za održavanje.", Comment = "Nevjerovatno praktičan i brz automobil. Prostor i performanse su spojeni bez kompromisa." },
                new { Vehicle = "Mercedes-AMG GT 53 4MATIC+", Rating = 5, PurchaseDaysAgo = 33, ReviewDelayDays = 7, LegacyComment = "Odnos cijene i koristi je jako dobar, posebno za lokalnu vožnju.", Comment = "Elegantna i brza limuzina sa odličnim pogonom na sve točkove i veoma kvalitetnim enterijerom." }
            };

            var addedReviews = 0;
            var updatedReviews = 0;
            var addedPurchases = 0;
            var seedDate = DateTime.UtcNow.Date;

            foreach (var review in demoReviews)
            {
                if (!vehicleByName.TryGetValue(review.Vehicle, out var vehicle))
                {
                    logger.LogWarning("Skipping demo purchase for {VehicleName} because the vehicle was not seeded.", review.Vehicle);
                    continue;
                }

                var purchaseDate = seedDate.AddDays(-review.PurchaseDaysAgo);
                var reviewDate = purchaseDate.AddDays(review.ReviewDelayDays);
                var purchasedItem = await dbContext.StavkeKorpe
                    .Include(item => item.Narudzba)
                    .FirstOrDefaultAsync(item =>
                        item.VoziloID == vehicle.VoziloID &&
                        item.NarudzbaID != null &&
                        item.Narudzba.KorisnikId == purchaseUser.Id);

                if (purchasedItem == null)
                {
                    var order = new Narudzba
                    {
                        KorisnikId = purchaseUser.Id,
                        DatumNarudzbe = purchaseDate,
                        Status = StatusNarudzbe.Placena,
                        UkupnaCijena = vehicle.Cijena ?? 0,
                        StavkeKorpe = new List<StavkaKorpe>
                        {
                            new StavkaKorpe
                            {
                                VoziloID = vehicle.VoziloID,
                                Kolicina = 1,
                                CijenaStavke = vehicle.Cijena ?? 0
                            }
                        },
                        Placanje = new Placanje
                        {
                            DatumPlacanja = purchaseDate,
                            Iznos = vehicle.Cijena ?? 0,
                            Status = StatusPlacanja.Uspjesno
                        }
                    };

                    dbContext.Narudzbe.Add(order);
                    addedPurchases++;
                }
                else
                {
                    purchasedItem.Kolicina = 1;
                    purchasedItem.CijenaStavke = vehicle.Cijena ?? 0;
                    purchasedItem.Narudzba.DatumNarudzbe = purchaseDate;
                    purchasedItem.Narudzba.Status = StatusNarudzbe.Placena;
                    purchasedItem.Narudzba.UkupnaCijena = vehicle.Cijena ?? 0;

                    var payment = await dbContext.Placanja.FindAsync(purchasedItem.NarudzbaID!.Value);
                    if (payment == null)
                    {
                        dbContext.Placanja.Add(new Placanje
                        {
                            NarudzbaID = purchasedItem.NarudzbaID.Value,
                            DatumPlacanja = purchaseDate,
                            Iznos = vehicle.Cijena ?? 0,
                            Status = StatusPlacanja.Uspjesno
                        });
                    }
                    else
                    {
                        payment.DatumPlacanja = purchaseDate;
                        payment.Iznos = vehicle.Cijena ?? 0;
                        payment.Status = StatusPlacanja.Uspjesno;
                    }
                }

                var existingReview = await dbContext.Recenzije.FirstOrDefaultAsync(item =>
                    item.Komentar == review.LegacyComment ||
                    item.Komentar == review.Comment ||
                    (item.KorisnikId == purchaseUser.Id && item.VoziloID == vehicle.VoziloID));

                if (existingReview == null)
                {
                    dbContext.Recenzije.Add(new Recenzija
                    {
                        VoziloID = vehicle.VoziloID,
                        KorisnikId = purchaseUser.Id,
                        Ocjena = review.Rating,
                        Komentar = review.Comment,
                        DatumRecenzije = reviewDate
                    });
                    addedReviews++;
                    continue;
                }

                existingReview.VoziloID = vehicle.VoziloID;
                existingReview.KorisnikId = purchaseUser.Id;
                existingReview.Ocjena = review.Rating;
                existingReview.Komentar = review.Comment;
                existingReview.DatumRecenzije = reviewDate;
                updatedReviews++;
            }

            var supportUser = buyerUser ?? reviewUsers[0];
            var demoInquiries = new[]
            {
                new { Title = "Pitanje oko rezervacije vozila", Body = "Zanima me koliko dugo mogu rezervisati vozilo prije kupovine.", Status = StatusUpita.CekaPodrsku },
                new { Title = "Provjera dostupnosti testne vožnje", Body = "Da li je moguće zakazati testnu vožnju za vikend?", Status = StatusUpita.UObradi },
                new { Title = "Informacije o finansiranju", Body = "Molim vas za više informacija o opcijama plaćanja na rate.", Status = StatusUpita.CekaKorisnika }
            };

            var addedInquiries = 0;
            foreach (var inquiry in demoInquiries)
            {
                if (await dbContext.PodrskaUpiti.AnyAsync(p => p.Naslov == inquiry.Title && p.KorisnikId == supportUser.Id))
                {
                    continue;
                }

                var inquiryDate = DateTime.UtcNow.AddDays(-(addedInquiries + 1));
                var ticket = new Podrska
                {
                    KorisnikId = supportUser.Id,
                    Naslov = inquiry.Title,
                    Status = inquiry.Status,
                    DatumUpita = inquiryDate,
                    DatumZadnjeAktivnosti = inquiryDate,
                    Jezik = "bs-Latn-BA",
                    DodijeljenKorisnikId = inquiry.Status == StatusUpita.CekaPodrsku ? null : sellerUser?.Id,
                    DatumDodjele = inquiry.Status == StatusUpita.CekaPodrsku ? null : inquiryDate,
                    Poruke = new List<PorukaPodrske>
                    {
                        new()
                        {
                            PosiljalacId = supportUser.Id,
                            TipAutora = TipAutoraPorukePodrske.Korisnik,
                            Sadrzaj = inquiry.Body,
                            DatumSlanja = inquiryDate
                        }
                    }
                };
                if (inquiry.Status == StatusUpita.CekaKorisnika && sellerUser != null)
                {
                    var replyDate = inquiryDate.AddHours(3);
                    ticket.Poruke.Add(new PorukaPodrske
                    {
                        PosiljalacId = sellerUser.Id,
                        TipAutora = TipAutoraPorukePodrske.Osoblje,
                        Sadrzaj = "Rado ćemo vam pripremiti ponudu. Da li vam više odgovara kartično plaćanje ili kredit?",
                        DatumSlanja = replyDate
                    });
                    ticket.DatumZadnjeAktivnosti = replyDate;
                }
                dbContext.PodrskaUpiti.Add(ticket);
                addedInquiries++;
            }

            if (addedReviews == 0 && updatedReviews == 0 && addedPurchases == 0 && addedInquiries == 0)
            {
                return;
            }

            await dbContext.SaveChangesAsync();
            logger.LogInformation(
                "Seeded {PurchaseCount} purchases, {ReviewCount} reviews, updated {UpdatedReviewCount} reviews and seeded {InquiryCount} support inquiries.",
                addedPurchases,
                addedReviews,
                updatedReviews,
                addedInquiries);
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
