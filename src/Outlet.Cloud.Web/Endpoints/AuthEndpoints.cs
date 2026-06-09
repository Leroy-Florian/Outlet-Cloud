using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Outlet.Identity.Infrastructure.Persistence;

namespace Outlet.Cloud.Web.Endpoints;

/// <summary>
/// Interactive (human) authentication + account/billing for the Outlet Cloud web UI,
/// backed by ASP.NET Core Identity with a session cookie. Machine access uses PATs.
/// Billing is mocked (no payment processor): subscribing flips the account to Pro.
/// </summary>
public static class AuthEndpoints
{
    public static void MapOutletAuth(this WebApplication app)
    {
        app.MapPost("/auth/register", async (
            RegisterRequest request,
            UserManager<OutletIdentityUser> users,
            SignInManager<OutletIdentityUser> signIn) =>
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

        app.MapGet("/auth/me", async (ClaimsPrincipal principal, UserManager<OutletIdentityUser> users) =>
        {
            var user = await users.GetUserAsync(principal);
            return user is null
                ? Results.Unauthorized()
                : Results.Ok(new { userId = user.Id, email = user.Email, displayName = user.DisplayName, plan = user.Plan.ToString() });
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

        // Mock billing: the web UI is a paid offering; subscribing flips the account to Pro.
        app.MapPost("/billing/subscribe", async (ClaimsPrincipal principal, UserManager<OutletIdentityUser> users) =>
            await SetPlanAsync(principal, users, UserPlan.Pro)).RequireAuthorization();

        app.MapPost("/billing/cancel", async (ClaimsPrincipal principal, UserManager<OutletIdentityUser> users) =>
            await SetPlanAsync(principal, users, UserPlan.Free)).RequireAuthorization();
    }

    private static async Task<IResult> SetPlanAsync(ClaimsPrincipal principal, UserManager<OutletIdentityUser> users, UserPlan plan)
    {
        var user = await users.GetUserAsync(principal);
        if (user is null)
            return Results.Unauthorized();

        user.Plan = plan;
        await users.UpdateAsync(user);
        return Results.Ok(new { plan = plan.ToString() });
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
