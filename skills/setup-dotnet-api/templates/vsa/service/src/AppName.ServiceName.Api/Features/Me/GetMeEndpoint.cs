// @file-module:trust
using AppName.Api.Authentication.Extensions;
using AppName.Api.Infrastructure;
using AppName.Api.Infrastructure.Endpoints;
using AppName.ServiceName.Api.Infrastructure.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace AppName.ServiceName.Api.Features.Me;

/// <summary>
/// An example route of a trust service. It reads the user from the claims of the access token, so it needs no
/// handler and no database.
/// </summary>
public sealed class GetMeEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapGroup("v{v:apiVersion}")
            .WithApiVersionSet(builder.CreateDefaultApiVersionSet())
            .WithTags("Me")
            .MapGet("me", GetMe)
            .AllowAnySignedInUser()
            .Produces<MeResponse>()
            .WithSummary("Get the signed-in user")
            .WithDescription("Returns the user of the access token that the auth service issued.");
    }

    private static IResult GetMe(HttpContext context)
    {
        var user = context.User.GetUser();

        return Results.Ok(new MeResponse(user.UserId, user.Username, user.EmailAddress));
    }
}
