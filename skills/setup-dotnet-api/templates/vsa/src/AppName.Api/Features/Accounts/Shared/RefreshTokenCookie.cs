// @file-module:auth
namespace AppName.Api.Features.Accounts.Shared;

/// <summary>
/// The HttpOnly cookie that holds the refresh token. It is SameSite=Strict, so the browser sends it
/// only from a page on the same site as the API.
/// </summary>
internal static class RefreshTokenCookie
{
    public const string Name = "refresh_token";

    public static void Append(HttpContext context, string refreshToken, DateTime expiresOn)
    {
        context.Response.Cookies.Append(Name, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Expires = expiresOn
        });
    }

    public static void Delete(HttpContext context)
    {
        context.Response.Cookies.Delete(Name, new CookieOptions
        {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Strict
        });
    }
}
