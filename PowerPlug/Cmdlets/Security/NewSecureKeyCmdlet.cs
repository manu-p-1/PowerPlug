using System.Management.Automation;
using System.Security;
using System.Security.Cryptography;
using PowerPlug.Base;
using PowerPlug.Internal;

namespace PowerPlug.Cmdlets.Security;

/// <summary>
/// <para type="synopsis">Generates cryptographically secure keys, passwords and secrets.</para>
/// <para type="description">Produces secrets from the system CSPRNG (<see cref="RandomNumberGenerator"/>), never
/// from <see cref="Random"/>. In the default password mode a character pool (letters, digits and symbols, or a
/// custom set) is sampled uniformly, with optional -Min* parameters that guarantee character class coverage
/// without weakening randomness: required characters are drawn from the intersection of the pool and the class,
/// the remainder is filled from the whole pool, and the buffer is then shuffled with a CSPRNG-driven Fisher-Yates
/// pass so the guaranteed characters do not land at predictable positions. Use -Encoding to switch to raw key
/// material instead, sized in bytes and returned as Base64, Base64Url or hex. Use -AsSecureString or -AsCredential
/// to avoid returning the secret as a plain string.</para>
/// <example>
/// <para>A 24 character password with at least one of each character class</para>
/// <code>New-SecureKey -Length 24 -MinUppercase 1 -MinLowercase 1 -MinDigits 1 -MinSymbols 1</code>
/// </example>
/// <example>
/// <para>A 256-bit key encoded as Base64Url, suitable for a JWT signing secret</para>
/// <code>New-SecureKey -ByteLength 32 -Encoding Base64Url</code>
/// </example>
/// <example>
/// <para>A credential object for use with -Credential parameters, without ever exposing the plain secret</para>
/// <code>New-SecureKey -Length 32 -AsCredential -UserName api-service</code>
/// </example>
/// </summary>
[Cmdlet(VerbsCommon.New, "SecureKey", DefaultParameterSetName = PasswordSet)]
[Alias("nsk")]
[OutputType(typeof(string))]
[OutputType(typeof(SecureString))]
[OutputType(typeof(PSCredential))]
public sealed class NewSecureKeyCmdlet : PowerPlugCmdlet
{
    private const string PasswordSet = "Password";
    private const string KeySet = "Key";

    private const string Base64 = "Base64";
    private const string Base64UrlEncoding = "Base64Url";
    private const string Hex = "Hex";

    /// <summary>
    /// <para type="description">Length of the password in characters. Defaults to 24. Only used in password mode.</para>
    /// </summary>
    [Parameter(Position = 0, ParameterSetName = PasswordSet)]
    [ValidateRange(1, 4096)]
    public int Length { get; set; } = 24;

    /// <summary>
    /// <para type="description">Length of the raw key in bytes. Defaults to 32 (256 bits). Only used with -Encoding.</para>
    /// </summary>
    [Parameter(Position = 0, ParameterSetName = KeySet)]
    [Alias("KeyLength")]
    [ValidateRange(1, 1024)]
    public int ByteLength { get; set; } = 32;

    /// <summary>
    /// <para type="description">Switches to raw key material of -ByteLength bytes, returned in this encoding instead
    /// of a character based password.</para>
    /// </summary>
    [Parameter(ParameterSetName = KeySet, Mandatory = true)]
    [ValidateSet(Base64, Base64UrlEncoding, Hex, IgnoreCase = true)]
    public string Encoding { get; set; } = Base64;

    /// <summary>
    /// <para type="description">Only use letters and digits.</para>
    /// </summary>
    [Parameter(ParameterSetName = PasswordSet)]
    public SwitchParameter AlphanumericOnly { get; set; }

    /// <summary>
    /// <para type="description">Leave out characters that are easy to confuse: 0 O o l I 1.</para>
    /// </summary>
    [Parameter(ParameterSetName = PasswordSet)]
    public SwitchParameter ExcludeAmbiguous { get; set; }

    /// <summary>
    /// <para type="description">Use exactly these characters instead of the built in sets.</para>
    /// </summary>
    [Parameter(ParameterSetName = PasswordSet)]
    [ValidateNotNullOrEmpty]
    public string? CharacterSet { get; set; }

    /// <summary>
    /// <para type="description">Minimum number of uppercase letters guaranteed to appear.</para>
    /// </summary>
    [Parameter(ParameterSetName = PasswordSet)]
    [ValidateRange(0, 4096)]
    public int MinUppercase { get; set; }

    /// <summary>
    /// <para type="description">Minimum number of lowercase letters guaranteed to appear.</para>
    /// </summary>
    [Parameter(ParameterSetName = PasswordSet)]
    [ValidateRange(0, 4096)]
    public int MinLowercase { get; set; }

    /// <summary>
    /// <para type="description">Minimum number of digits guaranteed to appear.</para>
    /// </summary>
    [Parameter(ParameterSetName = PasswordSet)]
    [ValidateRange(0, 4096)]
    public int MinDigits { get; set; }

    /// <summary>
    /// <para type="description">Minimum number of symbols guaranteed to appear.</para>
    /// </summary>
    [Parameter(ParameterSetName = PasswordSet)]
    [ValidateRange(0, 4096)]
    public int MinSymbols { get; set; }

    /// <summary>
    /// <para type="description">Number of secrets to generate. Defaults to 1.</para>
    /// </summary>
    [Parameter]
    [ValidateRange(1, 1000)]
    public int Count { get; set; } = 1;

    /// <summary>
    /// <para type="description">Return a System.Security.SecureString instead of a plain string.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter AsSecureString { get; set; }

    /// <summary>
    /// <para type="description">Return a PSCredential (secret as the password) instead of a plain string. Implies
    /// -AsSecureString.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter AsCredential { get; set; }

    /// <summary>
    /// <para type="description">User name to pair with -AsCredential. Defaults to "key".</para>
    /// </summary>
    [Parameter]
    [ValidateNotNullOrEmpty]
    public string UserName { get; set; } = "key";

    /// <inheritdoc />
    protected override void ProcessRecord()
    {
        if (ParameterSetName == KeySet)
        {
            for (var i = 0; i < Count; i++)
            {
                EmitBytes();
            }

            return;
        }

        var pool = RandomText.BuildPool(CharacterSet, AlphanumericOnly, ExcludeAmbiguous);
        if (pool.Length == 0)
        {
            WriteError(new ArgumentException("The character pool is empty after applying the options."),
                "EmptyCharacterPool", ErrorCategory.InvalidArgument, CharacterSet);
            return;
        }

        for (var i = 0; i < Count; i++)
        {
            if (!EmitChars(pool))
            {
                return; // Error already written; the requirements will not change on the next iteration.
            }
        }
    }

    private bool EmitChars(string pool)
    {
        char[] chars;
        try
        {
            chars = RandomText.GenerateWithRequirements(pool, Length, MinUppercase, MinLowercase, MinDigits, MinSymbols);
        }
        catch (ArgumentException ex)
        {
            WriteError(ex, "UnsatisfiableRequirement", ErrorCategory.InvalidArgument, pool);
            return false;
        }

        try
        {
            if (AsCredential)
            {
                WriteObject(new PSCredential(UserName, ToSecureString(chars)));
            }
            else if (AsSecureString)
            {
                WriteObject(ToSecureString(chars));
            }
            else
            {
                WriteObject(new string(chars));
            }
        }
        finally
        {
            Array.Clear(chars);
        }

        return true;
    }

    private void EmitBytes()
    {
        var bytes = RandomNumberGenerator.GetBytes(ByteLength);
        try
        {
            // Convert/ToHexString allocate immutable strings that cannot be wiped; this is a known .NET
            // limitation. SecureString/PSCredential still avoid the value ever reaching the pipeline as plain text.
            var encoded = EncodeBytes(bytes, Encoding);
            if (AsCredential)
            {
                WriteObject(new PSCredential(UserName, ToSecureString(encoded)));
            }
            else if (AsSecureString)
            {
                WriteObject(ToSecureString(encoded));
            }
            else
            {
                WriteObject(encoded);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(bytes);
        }
    }

    private static string EncodeBytes(byte[] bytes, string encoding) => encoding.ToUpperInvariant() switch
    {
        "BASE64" => Convert.ToBase64String(bytes),
        "BASE64URL" => Base64Url.Encode(bytes),
        "HEX" => Convert.ToHexString(bytes),
        _ => throw new ArgumentException($"Unsupported encoding: {encoding}", nameof(encoding)),
    };

    private static SecureString ToSecureString(char[] chars)
    {
        var secure = new SecureString();
        foreach (var c in chars)
        {
            secure.AppendChar(c);
        }

        secure.MakeReadOnly();
        return secure;
    }

    private static SecureString ToSecureString(string text)
    {
        var secure = new SecureString();
        foreach (var c in text)
        {
            secure.AppendChar(c);
        }

        secure.MakeReadOnly();
        return secure;
    }
}
