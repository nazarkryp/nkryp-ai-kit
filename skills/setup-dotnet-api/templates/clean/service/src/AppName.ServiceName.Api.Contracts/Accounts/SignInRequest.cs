// @file-module:auth
namespace AppName.ServiceName.Api.Contracts.Accounts;

public class SignInRequest
{
    public string Username { get; set; } = null!;
    
    public string Password { get; set; } = null!;
}