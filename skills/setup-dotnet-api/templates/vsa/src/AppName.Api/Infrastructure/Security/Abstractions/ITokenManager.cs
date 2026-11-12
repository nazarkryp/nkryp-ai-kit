// @file-module:auth
using AppName.Api.Infrastructure.Security.Tokens;

namespace AppName.Api.Infrastructure.Security.Abstractions;

public interface ITokenManager
{
    ValueTask RevokeTokenAsync(string tokenId, DateTime expiresOn, CancellationToken cancellationToken = default);

    ValueTask<bool> ValidateTokenAsync(string tokenId, CancellationToken cancellationToken = default);
}