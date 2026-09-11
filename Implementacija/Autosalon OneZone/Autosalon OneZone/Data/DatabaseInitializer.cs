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
                    Opis = "Pouzdana limuzina za svakodnevnu vožnju."
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
                    Kubikaza = null,
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
                    Kubikaza = null,
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
                },
                new Vozilo
                {
                    Marka = "Toyota",
                    Model = "Yaris",
                    Godiste = 2017,
                    Gorivo = TipGoriva.Benzin,
                    Kubikaza = 1.3m,
                    Boja = "Crvena",
                    Kilometraza = 112000,
                    Cijena = 16900m,
                    Slika = "seed-volkswagen-golf8-2021.jpg",
                    Opis = "Pristupačno gradsko vozilo sa niskom potrošnjom."
                },
                new Vozilo
                {
                    Marka = "Renault",
                    Model = "Clio",
                    Godiste = 2018,
                    Gorivo = TipGoriva.Dizel,
                    Kubikaza = 1.5m,
                    Boja = "Bijela",
                    Kilometraza = 98500,
                    Cijena = 14500m,
                    Slika = "seed-audi-a4-2020.jpg",
                    Opis = "Ekonomično vozilo za svakodnevnu gradsku vožnju."
                },
                new Vozilo
                {
                    Marka = "Opel",
                    Model = "Astra",
                    Godiste = 2016,
                    Gorivo = TipGoriva.Benzin,
                    Kubikaza = 1.4m,
                    Boja = "Siva",
                    Kilometraza = 124000,
                    Cijena = 13500m,
                    Slika = "seed-mercedes-c220-2019.jpg",
                    Opis = "Pouzdan hatchback za kupce koji traže niži budžet."
                },
                new Vozilo
                {
                    Marka = "Ford",
                    Model = "Fiesta",
                    Godiste = 2017,
                    Gorivo = TipGoriva.Benzin,
                    Kubikaza = 1.25m,
                    Boja = "Plava",
                    Kilometraza = 108000,
                    Cijena = 11900m,
                    Slika = "seed-bmw-x5-2022.jpg",
                    Opis = "Kompaktno vozilo za gradsku vožnju i početnike."
                },
                new Vozilo
                {
                    Marka = "Hyundai",
                    Model = "i20",
                    Godiste = 2019,
                    Gorivo = TipGoriva.Benzin,
                    Kubikaza = 1.2m,
                    Boja = "Srebrna",
                    Kilometraza = 72000,
                    Cijena = 15500m,
                    Slika = "seed-tesla-model3-2023.jpg",
                    Opis = "Dobro očuvano vozilo sa jednostavnim održavanjem."
                },
                new Vozilo
                {
                    Marka = "Dacia",
                    Model = "Sandero",
                    Godiste = 2020,
                    Gorivo = TipGoriva.Plin,
                    Kubikaza = 1.0m,
                    Boja = "Bijela",
                    Kilometraza = 68000,
                    Cijena = 12900m,
                    Slika = "seed-mercedes-glc300-2020.jpg",
                    Opis = "Povoljan demo model sa plinskim pogonom."
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

            var vehicleByName = vehicles.ToDictionary(vehicle => $"{vehicle.Marka} {vehicle.Model}", StringComparer.OrdinalIgnoreCase);
            Vozilo PickVehicle(string name) => vehicleByName.TryGetValue(name, out var vehicle) ? vehicle : vehicles[0];

            var demoReviews = new[]
            {
                new { Vehicle = "Porsche Panamera 4 E-Hybrid", User = reviewUsers[0], Rating = 5, Comment = "Izuzetno udobno i brzo vozilo, cijeli proces kupovine je bio profesionalan." },
                new { Vehicle = "Volkswagen Golf 8", User = reviewUsers[^1], Rating = 4, Comment = "Golf je pregledan, uredan i dobar za svakodnevnu vožnju." },
                new { Vehicle = "Audi A4", User = reviewUsers[0], Rating = 5, Comment = "Odličan balans cijene, opreme i potrošnje. Preporuka za porodičnu vožnju." },
                new { Vehicle = "BMW X5", User = reviewUsers[^1], Rating = 4, Comment = "Prostran SUV, dobra oprema i veoma stabilan na otvorenoj cesti." },
                new { Vehicle = "Tesla Model 3", User = reviewUsers[0], Rating = 5, Comment = "Elektricni pogon je tih i brz, autonomija je odlicna za svakodnevnu upotrebu." },
                new { Vehicle = "Mercedes-Benz C 220", User = reviewUsers[^1], Rating = 4, Comment = "Pouzdana limuzina, udobna i ekonomična za duže relacije." },
                new { Vehicle = "Audi e-tron GT quattro", User = reviewUsers[0], Rating = 5, Comment = "Vrhunski izgled i performanse, auto ostavlja premium utisak." },
                new { Vehicle = "Toyota Yaris", User = reviewUsers[^1], Rating = 4, Comment = "Povoljan i praktičan izbor za gradsku vožnju." },
                new { Vehicle = "Renault Clio", User = reviewUsers[0], Rating = 3, Comment = "Dobar budžet auto, ekonomičan i jednostavan za održavanje." },
                new { Vehicle = "Dacia Sandero", User = reviewUsers[^1], Rating = 4, Comment = "Odnos cijene i koristi je jako dobar, posebno za lokalnu vožnju." }
            };

            var addedReviews = 0;
            foreach (var review in demoReviews)
            {
                if (await dbContext.Recenzije.AnyAsync(r => r.Komentar == review.Comment))
                {
                    continue;
                }

                dbContext.Recenzije.Add(new Recenzija
                {
                    VoziloID = PickVehicle(review.Vehicle).VoziloID,
                    KorisnikId = review.User.Id,
                    Ocjena = review.Rating,
                    Komentar = review.Comment,
                    DatumRecenzije = DateTime.UtcNow.AddDays(-(addedReviews + 1))
                });
                addedReviews++;
            }

            var supportUser = buyerUser ?? reviewUsers[0];
            var demoInquiries = new[]
            {
                new { Title = "Pitanje oko rezervacije vozila", Body = "Zanima me koliko dugo mogu rezervisati vozilo prije kupovine.", Status = StatusUpita.Poslat },
                new { Title = "Provjera dostupnosti testne vožnje", Body = "Da li je moguće zakazati testnu vožnju za vikend?", Status = StatusUpita.UObradi },
                new { Title = "Informacije o finansiranju", Body = "Molim vas za više informacija o opcijama plaćanja na rate.", Status = StatusUpita.Odgovoren }
            };

            var addedInquiries = 0;
            foreach (var inquiry in demoInquiries)
            {
                if (await dbContext.PodrskaUpiti.AnyAsync(p => p.Naslov == inquiry.Title && p.KorisnikId == supportUser.Id))
                {
                    continue;
                }

                dbContext.PodrskaUpiti.Add(new Podrska
                {
                    KorisnikId = supportUser.Id,
                    Naslov = inquiry.Title,
                    Sadrzaj = inquiry.Body,
                    Status = inquiry.Status,
                    DatumUpita = DateTime.UtcNow.AddDays(-(addedInquiries + 1))
                });
                addedInquiries++;
            }

            if (addedReviews == 0 && addedInquiries == 0)
            {
                return;
            }

            await dbContext.SaveChangesAsync();
            logger.LogInformation("Seeded {ReviewCount} demo reviews and {InquiryCount} support inquiries.", addedReviews, addedInquiries);
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
