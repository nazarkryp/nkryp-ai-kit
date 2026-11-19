// @file-module:auth
using AppName.ServiceName.Security.Tokens;

namespace AppName.ServiceName.Security.Abstractions;

public interface ITokenProvider
{
    public Token GenerateToken(CreateTokenParameters parameters);
}