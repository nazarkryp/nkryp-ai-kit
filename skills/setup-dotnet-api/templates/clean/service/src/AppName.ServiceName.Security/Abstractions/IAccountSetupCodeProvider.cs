// @file-module:permissions
using AppName.ServiceName.Security.Tokens;

namespace AppName.ServiceName.Security.Abstractions;

public interface IAccountSetupCodeProvider
{
    /// <summary>A new random code of 128 bits in base64url, and its hash.</summary>
    AccountSetupCodeValue Generate();

    string Hash(string code);

    /// <summary>Whether the text has the form of a code. A wrong form needs no database lookup.</summary>
    bool IsFormat(string code);
}
