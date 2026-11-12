// @file-module:auth
namespace AppName.Security.Abstractions;

public interface IPasswordManager
{
    string CreatePasswordHash(string password);

    bool VerifyPassword(string password, string passwordHash);
}
