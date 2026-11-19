// @file-module:auth
using AppName.ServiceName.Api.Infrastructure.Security.Abstractions;
using Microsoft.Extensions.Caching.Distributed;

namespace AppName.ServiceName.Api.Infrastructure.Security.Impl;

public sealed class TokenManager(IDistributedCache distributedCache) : ITokenManager
{
    private readonly IDistributedCache _distributedCache = distributedCache;

    public async ValueTask RevokeTokenAsync(string tokenId, DateTime expiresOn, CancellationToken cancellationToken = default)
    {
        await _distributedCache.SetStringAsync(tokenId, "revoked", new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = expiresOn
        }, cancellationToken);
    }

    public async ValueTask<bool> ValidateTokenAsync(string tokenId, CancellationToken cancellationToken = default)
    {
        var value = await _distributedCache.GetStringAsync(tokenId, cancellationToken);
        
        return value is null;
    }
}