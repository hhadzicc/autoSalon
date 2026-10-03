#nullable disable

using System.Reflection;
using Autosalon_OneZone.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutosalonOneZone.Tests;

public class AuthorizationContractTests
{
    [Fact]
    public void Admin_panel_controller_is_not_available_to_anonymous_or_buyer_users()
    {
        var authorize = typeof(AdminPanelController).GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(authorize);
        Assert.Equal("Administrator,Prodavac", authorize.Roles);
    }

    [Theory]
    [InlineData(nameof(AdminPanelController.GetProfiliSection))]
    [InlineData(nameof(AdminPanelController.GetProfiliJson))]
    [InlineData(nameof(AdminPanelController.GetAddProfilForm))]
    [InlineData(nameof(AdminPanelController.GetEditProfilForm))]
    [InlineData(nameof(AdminPanelController.SaveProfil))]
    [InlineData(nameof(AdminPanelController.DeleteProfil))]
    [InlineData(nameof(AdminPanelController.DeletePodrska))]
    public void Administrator_only_admin_actions_are_explicitly_restricted(string actionName)
    {
        var method = FindAction(actionName);
        var authorize = method.GetCustomAttributes<AuthorizeAttribute>().ToList();

        Assert.Contains(authorize, attribute => attribute.Roles == "Administrator");
    }

    [Theory]
    [InlineData(nameof(AdminPanelController.PreuzmiPodrsku))]
    [InlineData(nameof(AdminPanelController.OslobodiPodrsku))]
    [InlineData(nameof(AdminPanelController.OdgovoriNaPodrsku))]
    [InlineData(nameof(AdminPanelController.ZatvoriPodrsku))]
    public void Support_mutations_are_available_to_admin_and_seller_roles(string actionName)
    {
        var method = FindAction(actionName);

        Assert.NotNull(method.GetCustomAttribute<HttpPostAttribute>());
        Assert.NotNull(method.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
        var controllerAuthorize = typeof(AdminPanelController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.Equal("Administrator,Prodavac", controllerAuthorize?.Roles);
        Assert.Null(method.GetCustomAttribute<AllowAnonymousAttribute>());
    }

    [Theory]
    [InlineData(nameof(AdminPanelController.SaveVozilo))]
    [InlineData(nameof(AdminPanelController.DeleteVozilo))]
    [InlineData(nameof(AdminPanelController.DeleteRecenzija))]
    public void Admin_mutation_actions_require_post_and_antiforgery(string actionName)
    {
        var method = FindAction(actionName);

        Assert.NotNull(method.GetCustomAttribute<HttpPostAttribute>());
        Assert.NotNull(method.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
    }

    [Theory]
    [InlineData(nameof(AdminPanelController.SaveProfil))]
    [InlineData(nameof(AdminPanelController.DeleteProfil))]
    public void Sensitive_admin_only_mutations_require_post_antiforgery_and_admin_role(string actionName)
    {
        var method = FindAction(actionName);

        Assert.NotNull(method.GetCustomAttribute<HttpPostAttribute>());
        Assert.NotNull(method.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
        Assert.Contains(method.GetCustomAttributes<AuthorizeAttribute>(), attribute => attribute.Roles == "Administrator");
    }

    [Theory]
    [InlineData(nameof(AccountController.Login))]
    [InlineData(nameof(AccountController.Register))]
    public void Account_login_and_register_allow_anonymous_users(string actionName)
    {
        var methods = typeof(AccountController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(method => method.Name == actionName)
            .ToList();

        Assert.NotEmpty(methods);
        Assert.All(methods, method => Assert.NotNull(method.GetCustomAttribute<AllowAnonymousAttribute>()));
    }

    private static MethodInfo FindAction(string name)
    {
        var methods = typeof(AdminPanelController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(method => method.Name == name)
            .ToList();

        Assert.NotEmpty(methods);
        return methods.Count == 1
            ? methods[0]
            : methods.Single(method => method.GetCustomAttributes().Any(attribute =>
                attribute is HttpGetAttribute or HttpPostAttribute));
    }
}
