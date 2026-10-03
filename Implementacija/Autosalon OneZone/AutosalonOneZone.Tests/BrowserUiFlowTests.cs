#nullable disable

using Microsoft.Playwright;

namespace AutosalonOneZone.Tests;

public class BrowserUiFlowTests
{
    private static string AdminLogin => Environment.GetEnvironmentVariable("AUTOSALON_E2E_ADMIN_LOGIN") ?? "admin";
    private static string AdminPassword => Environment.GetEnvironmentVariable("AUTOSALON_E2E_ADMIN_PASSWORD") ?? "Admin123!";

    [Fact]
    public async Task Admin_can_take_support_ticket_reply_and_wait_for_customer()
    {
        if (!TryGetBaseUrl(out var baseUrl))
        {
            return;
        }

        await using var session = await BrowserSession.StartAsync();
        var page = await session.NewPageAsync(baseUrl, width: 1366, height: 900);
        await LoginAsAdminAsync(page, baseUrl);

        var title = "E2E status " + Guid.NewGuid().ToString("N")[..8];
        await CreateSupportInquiryAsync(page, baseUrl, title);
        Assert.Contains("/Profil/PodrskaDetalji/", page.Url, StringComparison.OrdinalIgnoreCase);
        await page.WaitForSelectorAsync(".support-thread");
        await page.WaitForSelectorAsync(".support-reply-form");

        await page.GotoAsync(Url(baseUrl, "/AdminPanel?section=Podrska"));
        await page.WaitForSelectorAsync("#podrska-search-input");
        await page.FillAsync("#podrska-search-input", title);
        await page.ClickAsync("#podrska-search-button");
        await page.WaitForFunctionAsync(
            "expected => document.querySelector('#podrska-table-body')?.innerText.includes(expected)",
            title);

        var row = page.Locator("#podrska-table-body tr").Filter(new() { HasTextString = title }).First;
        await row.Locator(".view-message-button").ClickAsync();
        await page.WaitForSelectorAsync("#messageModal.show");

        var takeResponseTask = page.WaitForResponseAsync(response =>
            response.Url.Contains("PreuzmiPodrsku", StringComparison.OrdinalIgnoreCase));
        await page.ClickAsync("#messageModalTake");
        Assert.True((await takeResponseTask).Ok);

        await page.FillAsync("#messageModalReplyText", "Automated support workflow response.");
        var replyResponseTask = page.WaitForResponseAsync(response =>
            response.Url.Contains("OdgovoriNaPodrsku", StringComparison.OrdinalIgnoreCase));
        await page.ClickAsync("#messageModalSendWaiting");
        Assert.True((await replyResponseTask).Ok);

        await page.WaitForFunctionAsync(
            "expectedTitle => document.querySelector('#podrska-table-body')?.innerText.includes(expectedTitle) && document.querySelector('#podrska-table-body')?.innerText.includes('Čeka kupca')",
            title);

        var persistedStatus = await page.EvaluateAsync<string>(
            @"async expectedTitle => {
                const response = await fetch('/AdminPanel/GetPodrskaJson?searchQuery=' + encodeURIComponent(expectedTitle));
                const payload = await response.json();
                return payload.upiti?.[0]?.status || '';
            }",
            title);

        Assert.Equal("CekaKorisnika", persistedStatus);
    }

    [Fact]
    public async Task Admin_users_role_badge_and_actions_do_not_overlap_and_actions_are_clickable()
    {
        if (!TryGetBaseUrl(out var baseUrl))
        {
            return;
        }

        await using var session = await BrowserSession.StartAsync();
        var page = await session.NewPageAsync(baseUrl, width: 1366, height: 900);
        await LoginAsAdminAsync(page, baseUrl);

        await page.GotoAsync(Url(baseUrl, "/AdminPanel?section=Profili"));
        await page.WaitForSelectorAsync("#profili-table-body tr");
        await page.WaitForFunctionAsync(
            "() => !document.querySelector('#profili-table-body')?.innerText.includes('Ucitavanje')");

        var row = page.Locator("#profili-table-body tr").First;
        var layoutIsSeparated = await row.EvaluateAsync<bool>(
            @"row => {
                const role = row.querySelector('.admin-badge');
                const actions = row.querySelector('.table-actions');
                const edit = row.querySelector('.edit-profil-button');
                const del = row.querySelector('.delete-profil-button');
                if (!role || !actions || !edit || !del) return false;

                const roleBox = role.getBoundingClientRect();
                const actionsBox = actions.getBoundingClientRect();
                const editBox = edit.getBoundingClientRect();
                const deleteBox = del.getBoundingClientRect();
                const editStyle = window.getComputedStyle(edit);
                const deleteStyle = window.getComputedStyle(del);

                const separated = roleBox.right <= actionsBox.left || roleBox.bottom <= actionsBox.top || actionsBox.bottom <= roleBox.top;
                const buttonsCenterContent =
                    editStyle.justifyContent === 'center' &&
                    editStyle.alignItems === 'center' &&
                    deleteStyle.justifyContent === 'center' &&
                    deleteStyle.alignItems === 'center';

                return separated && editBox.width > 0 && deleteBox.width > 0 && buttonsCenterContent;
            }");

        Assert.True(layoutIsSeparated, "Role badge and profile actions should not overlap, and action button content should be centered.");

        await row.Locator(".edit-profil-button").ClickAsync();
        await page.WaitForSelectorAsync("#add-profil-form");
        Assert.True(await page.Locator("#add-profil-form").IsVisibleAsync());

        await page.SetViewportSizeAsync(430, 820);
        await page.GotoAsync(Url(baseUrl, "/AdminPanel?section=Profili"));
        await page.WaitForSelectorAsync("#profili-mobile-list article");

        var mobileEdit = page.Locator("#profili-mobile-list .edit-profil-button").First;
        var mobileDelete = page.Locator("#profili-mobile-list .delete-profil-button").First;

        Assert.True(await mobileEdit.IsVisibleAsync());
        Assert.True(await mobileDelete.IsVisibleAsync());
    }

    private static async Task LoginAsAdminAsync(IPage page, string baseUrl)
    {
        await page.GotoAsync(Url(baseUrl, "/Account/Login"));
        await page.FillAsync("input[name='LoginIdentifier']", Environment.GetEnvironmentVariable("AUTOSALON_E2E_ADMIN_LOGIN") ?? AdminLogin);
        await page.FillAsync("input[name='Password']", Environment.GetEnvironmentVariable("AUTOSALON_E2E_ADMIN_PASSWORD") ?? AdminPassword);
        await page.ClickAsync("button[type='submit']");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var currentUrl = page.Url;
        Assert.DoesNotContain("/Account/Login", currentUrl, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task CreateSupportInquiryAsync(IPage page, string baseUrl, string title)
    {
        await page.GotoAsync(Url(baseUrl, "/Home/Kontakt"));
        await page.WaitForSelectorAsync("#contact-form");
        await page.FillAsync("input[name='Naslov']", title);
        await page.FillAsync("textarea[name='Sadrzaj']", "Automated support conversation workflow test.");
        await page.ClickAsync("#send-message");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    private static bool TryGetBaseUrl(out string baseUrl)
    {
        baseUrl = Environment.GetEnvironmentVariable("AUTOSALON_E2E_BASE_URL");
        return !string.IsNullOrWhiteSpace(baseUrl);
    }

    private static string Url(string baseUrl, string path)
    {
        return baseUrl.TrimEnd('/') + path;
    }

    private sealed class BrowserSession : IAsyncDisposable
    {
        private readonly IPlaywright _playwright;
        private readonly IBrowser _browser;

        private BrowserSession(IPlaywright playwright, IBrowser browser)
        {
            _playwright = playwright;
            _browser = browser;
        }

        public static async Task<BrowserSession> StartAsync()
        {
            var playwright = await Playwright.CreateAsync();
            var options = new BrowserTypeLaunchOptions
            {
                Headless = true
            };

            var executablePath = Environment.GetEnvironmentVariable("AUTOSALON_E2E_CHROME_PATH") ?? FindChromeExecutable();
            if (!string.IsNullOrWhiteSpace(executablePath))
            {
                options.ExecutablePath = executablePath;
            }
            else
            {
                options.Channel = "chrome";
            }

            var browser = await playwright.Chromium.LaunchAsync(options);
            return new BrowserSession(playwright, browser);
        }

        public async Task<IPage> NewPageAsync(string baseUrl, int width, int height)
        {
            var context = await _browser.NewContextAsync(new()
            {
                BaseURL = baseUrl,
                ViewportSize = new ViewportSize
                {
                    Width = width,
                    Height = height
                }
            });

            return await context.NewPageAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await _browser.DisposeAsync();
            _playwright.Dispose();
        }

        private static string FindChromeExecutable()
        {
            var candidates = new[]
            {
                @"C:\Program Files\Google\Chrome\Application\chrome.exe",
                @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
                @"C:\Program Files\Microsoft\Edge\Application\msedge.exe",
                @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"
            };

            return candidates.FirstOrDefault(File.Exists);
        }
    }
}
