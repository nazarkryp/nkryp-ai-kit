// @file-module:auth
using AppName.Api.Infrastructure.Security.Tokens;

namespace AppName.Api.Infrastructure.Security.Abstractions;

public interface ITokenProvider
{
    public Token GenerateToken(CreateTokenParameters parameters);
}