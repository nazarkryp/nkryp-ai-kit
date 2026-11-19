// @file-module:permissions
using AppName.ServiceName.Domain.Accounts;

namespace AppName.ServiceName.Persistence.Abstractions;

public interface IAccountSetupCodeRepository
{
    /// <summary>Writes the setup code of the account. It replaces an older one, so only the newest code works.</summary>
    Task SaveAsync(AccountSetupCode code, CancellationToken cancellationToken = default);

    /// <summary>The code with this hash, when it is not expired at <paramref name="now"/>. Null otherwise.</summary>
    Task<AccountSetupCode?> GetValidAsync(string codeHash, DateTime now, CancellationToken cancellationToken = default);

    /// <summary>The expiry of each open code that is not expired at <paramref name="now"/>, by account id.</summary>
    Task<IReadOnlyDictionary<Guid, DateTime>> GetOpenExpiriesAsync(DateTime now, CancellationToken cancellationToken = default);

    /// <summary>
    /// In one transaction: deletes the code only when the account id and the hash match and the code is not
    /// expired at <paramref name="now"/>, sets the password hash, and deletes every refresh token of the account.
    /// </summary>
    /// <returns>
    /// False when the guarded delete removed no row. Then nothing is written. Of two requests with the same
    /// code, only one gets true.
    /// </returns>
    Task<bool> TryCompleteSetupAsync(
        Guid accountId,
        string codeHash,
        DateTime now,
        string passwordHash,
        CancellationToken cancellationToken = default);
}
