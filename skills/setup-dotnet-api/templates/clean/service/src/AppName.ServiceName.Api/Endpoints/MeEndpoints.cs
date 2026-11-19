// @file-module:trust
using AppName.Api.Authentication.Extensions;
using AppName.Api.Infrastructure;
using AppName.Api.Infrastructure.Endpoints;
using AppName.ServiceName.Api.Contracts.Me;
using AppName.ServiceName.Api.Infrastructure.Extensions;

namespace AppName.ServiceName.Api.Endpoints;

/// <summary>
/// An example route of a service that trusts the access tokens of the auth service. It reads the claims of
/// the caller, which is an API concern, so it sends no request to the Application layer.
/// </summary>
public sealed class MeEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("v{v:apiVersion}/me")
            .WithApiVersionSet(builder.CreateDefaultApiVersionSet())
            .WithTags("Me");

        group.MapGet("", GetMe)
            .AllowAnySignedInUser()
            .Produces<MeResponse>()
            .WithSummary("Get the signed-in user")
            .WithDescription("Returns the user that the access token names.");
    }

    private static IResult GetMe(HttpContext context)
    {
        var user = context.User.GetUser();

        return Results.Ok(new MeResponse
        {
            UserId = user.UserId,
            Username = user.Username,
            EmailAddress = user.EmailAddress
        });
    }
}
