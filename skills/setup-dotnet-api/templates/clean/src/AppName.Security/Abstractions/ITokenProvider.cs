// @file-module:auth
using AppName.Security.Tokens;

namespace AppName.Security.Abstractions;

public interface ITokenProvider
{
    public Token GenerateToken(CreateTokenParameters parameters);
}