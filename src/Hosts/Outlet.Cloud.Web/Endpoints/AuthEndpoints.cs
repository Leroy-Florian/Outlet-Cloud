using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Outlet.Cloud.Application.Subscriptions;
using Outlet.Identity.Infrastructure.Persistence;

namespace Outlet.Cloud.Web.Endpoints;

/// <summary>
/// Interactive (human) authentication + account/billing for the Outlet Cloud web UI,
/// backed by ASP.NET Core Identity with a session cookie. Machine access uses PATs.
/// Sign-up immediately starts a frictionless Pro trial (the account's <c>Subscription</c>);
/// billing is mocked (no payment processor): subscribing converts the trial to Active.
/// </summary>
public static class AuthEndpoints
{
    /// <summary>Default trial length (CLAUDE.md: 14 days, parameterizable).</summary>
    private const int TrialDays = 14;

    public static void MapOutletAuth(this WebApplication app)
    {
        app.MapPost("/auth/register", async (
            RegisterRequest request,
            UserManager<OutletIdentityUser> users,
            SignInManager<OutletIdentityUser> signIn,
            StartTrialUseCase startTrial) =>
        {
            var email = request.Email.Trim();
            var user = new OutletIdentityUser
            {
                Id = Guid.NewGuid(),
                Email = email,
                UserName = email,
                DisplayName = request.DisplayName.Trim(),
            };

            var result = await users.CreateAsync(user, request.Password);
            if (!result.Succeeded)
                return Results.BadRequest(new { error = string.Join("; ", result.Errors.Select(e => e.Description)) });

            // Frictionless trial: the account gets full Pro access immediately, no card.
            // Anti-abuse (disposable domains, etc.) is enforced inside the use case; if it
            // declines, the account exists but has no entitlements until it subscribes.
            await startTrial.HandleAsync(new StartTrialCommand(user.Id, email, TrialDays));

            await signIn.SignInAsync(user, isPersistent: true);
            return Results.Created($"/users/{user.Id}", new { userId = user.Id, displayName = user.DisplayName });
        });

        app.MapPost("/auth/login", async (
            LoginRequest request,
            UserManager<OutletIdentityUser> users,
            SignInManager<OutletIdentityUser> signIn) =>
        {
            var user = await users.FindByEmailAsync(request.Email.Trim());
            if (user is null)
                return Results.Unauthorized();

            var result = await signIn.PasswordSignInAsync(user, request.Password, isPersistent: true, lockoutOnFailure: false);
            return result.Succeeded
                ? Results.Ok(new { userId = user.Id, displayName = user.DisplayName })
                : Results.Unauthorized();
        });

        app.MapGet("/auth/me", async (ClaimsPrincipal principal, UserManager<OutletIdentityUser> users, GetEntitlementsUseCase entitlements) =>
        {
            var user = await users.GetUserAsync(principal);
            if (user is null)
                return Results.Unauthorized();

            // Plan/status come from the account subscription (what `outlet whoami` shows).
            var view = (await entitlements.HandleAsync(new GetEntitlementsQuery(user.Id))).Value!;
            return Results.Ok(new
            {
                userId = user.Id,
                email = user.Email,
                displayName = user.DisplayName,
                plan = view.Plan,
                status = view.Status,
                trialDaysRemaining = view.TrialDaysRemaining,
            });
        }).RequireAuthorization();

        app.MapPost("/auth/logout", async (SignInManager<OutletIdentityUser> signIn) =>
        {
            await signIn.SignOutAsync();
            return Results.NoContent();
        }).RequireAuthorization();

        // Password reset. No email infrastructure in this build, so /forgot-password
        // returns the reset token directly (DEV ONLY — in production it is emailed and
        // never returned in the response).
        app.MapPost("/auth/forgot-password", async (ForgotPasswordRequest request, UserManager<OutletIdentityUser> users) =>
        {
            var user = await users.FindByEmailAsync(request.Email.Trim());
            var token = user is null ? null : await users.GeneratePasswordResetTokenAsync(user);
            return Results.Ok(new { token });
        });

        app.MapPost("/auth/reset-password", async (ResetPasswordRequest request, UserManager<OutletIdentityUser> users) =>
        {
            var user = await users.FindByEmailAsync(request.Email.Trim());
            if (user is null)
                return Results.BadRequest(new { error = "Invalid reset request." });

            var result = await users.ResetPasswordAsync(user, request.Token, request.NewPassword);
            return result.Succeeded
                ? Results.NoContent()
                : Results.BadRequest(new { error = string.Join("; ", result.Errors.Select(e => e.Description)) });
        });

        // Mock billing: no payment processor. Subscribing converts the trial to a paying plan;
        // cancelling suspends it (read-only, data retained). Real checkout (Stripe…) plugs in
        // upstream of these use cases without touching the state machine.
        app.MapPost("/billing/subscribe", async (ClaimsPrincipal principal, UserManager<OutletIdentityUser> users, ConvertSubscriptionUseCase convert) =>
        {
            var user = await users.GetUserAsync(principal);
            if (user is null)
                return Results.Unauthorized();

            var result = await convert.HandleAsync(new ConvertSubscriptionCommand(user.Id));
            return result.IsSuccess ? Results.Ok(new { status = nameof(Outlet.Cloud.Domain.Subscriptions.SubscriptionStatus.Active) }) : result.ToHttp();
        }).RequireAuthorization();

        app.MapPost("/billing/cancel", async (ClaimsPrincipal principal, UserManager<OutletIdentityUser> users, CancelSubscriptionUseCase cancel) =>
        {
            var user = await users.GetUserAsync(principal);
            if (user is null)
                return Results.Unauthorized();

            var result = await cancel.HandleAsync(new CancelSubscriptionCommand(user.Id));
            return result.IsSuccess ? Results.Ok(new { status = nameof(Outlet.Cloud.Domain.Subscriptions.SubscriptionStatus.Suspended) }) : result.ToHttp();
        }).RequireAuthorization();
    }
}

/// <summary>Self-service registration with a password.</summary>
public sealed record RegisterRequest(string Email, string Password, string DisplayName);

/// <summary>Email + password login.</summary>
public sealed record LoginRequest(string Email, string Password);

/// <summary>Request a password-reset token for an email.</summary>
public sealed record ForgotPasswordRequest(string Email);

/// <summary>Reset a password using a token obtained from /auth/forgot-password.</summary>
public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);
