using System.Security.Claims;

using FubarDev.FtpServer.AccountManagement;

namespace Iec61850Sim.Web.Services;

public sealed class SingleUserMembershipProvider : IMembershipProvider
{
    private const string Username = "iec61850sim";
    private const string Password = "iec61850sim";

    public Task<MemberValidationResult> ValidateUserAsync(string username, string password)
    {
        if (!string.Equals(username, Username, StringComparison.Ordinal) ||
            !string.Equals(password, Password, StringComparison.Ordinal))
            return Task.FromResult(new MemberValidationResult(MemberValidationStatus.InvalidLogin));

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, username)],
            authenticationType: "password");
        var principal = new ClaimsPrincipal(identity);

        return Task.FromResult(new MemberValidationResult(MemberValidationStatus.AuthenticatedUser, principal));
    }
}
