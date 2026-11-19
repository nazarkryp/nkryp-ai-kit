// @file-module:auth
namespace AppName.ServiceName.Security.Abstractions;

public interface IPasswordManager
{
    string CreatePasswordHash(string password);

    bool VerifyPassword(string password, string passwordHash);
}
