using System.Management.Automation;
using PowerPlug.Base;
using PowerPlug.Internal;
using PowerPlug.Models;

namespace PowerPlug.Cmdlets.Data;

/// <summary>
/// <para type="synopsis">Decodes the header and claims of a JSON Web Token.</para>
/// <para type="description">Splits a compact JWT, Base64Url decodes the header and payload, and exposes the
/// standard claims (iss, sub, aud, iat, nbf, exp) as typed properties. A leading "Bearer " prefix is stripped.
/// The signature is not verified.</para>
/// <example>
/// <para>Inspect a token from an Authorization header</para>
/// <code>$response.Headers.Authorization | ConvertFrom-Jwt</code>
/// </example>
/// <example>
/// <para>Check whether a token has expired</para>
/// <code>(ConvertFrom-Jwt $token).IsExpired</code>
/// </example>
/// </summary>
[Cmdlet(VerbsData.ConvertFrom, "Jwt")]
[Alias("fromjwt")]
[OutputType(typeof(JwtToken))]
public sealed class ConvertFromJwtCmdlet : PowerPlugCmdlet
{
    /// <summary>
    /// <para type="description">The compact JWT string.</para>
    /// </summary>
    [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
    [ValidateNotNullOrEmpty]
    [Alias("Jwt")]
    public string Token { get; set; } = string.Empty;

    /// <inheritdoc />
    protected override void ProcessRecord()
    {
        try
        {
            WriteObject(JwtDecoder.Decode(Token, DateTimeOffset.UtcNow));
        }
        catch (FormatException ex)
        {
            WriteError(ex, "InvalidJwt", ErrorCategory.InvalidData, Token);
        }
    }
}
