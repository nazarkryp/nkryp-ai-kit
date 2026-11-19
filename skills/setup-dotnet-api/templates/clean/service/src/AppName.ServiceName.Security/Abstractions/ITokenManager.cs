// @file-module:auth
using AppName.ServiceName.Security.Tokens;

namespace AppName.ServiceName.Security.Abstractions;

public interface ITokenManager
{
    ValueTask RevokeTokenAsync(string tokenId, DateTime expiresOn, CancellationToken cancellationToken = default);

    ValueTask<bool> ValidateTokenAsync(string tokenId, CancellationToken cancellationToken = default);
}