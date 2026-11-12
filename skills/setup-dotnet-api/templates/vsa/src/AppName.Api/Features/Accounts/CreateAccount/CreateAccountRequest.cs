// @file-module:auth
namespace AppName.Api.Features.Accounts.CreateAccount;

public sealed class CreateAccountRequest
{
    public string Username { get; set; } = null!;
    
    public string EmailAddress { get; set; } = null!;
    
    public string Password { get; set; } = null!;
}