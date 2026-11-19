// @file-module:auth
using AppName.ServiceName.Api.Infrastructure.Security.Tokens;

namespace AppName.ServiceName.Api.Infrastructure.Security.Abstractions;

public interface ITokenProvider
{
    public Token GenerateToken(CreateTokenParameters parameters);
}