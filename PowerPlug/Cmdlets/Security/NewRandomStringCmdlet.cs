using System.Management.Automation;
using System.Security.Cryptography;
using System.Text;
using PowerPlug.Base;

namespace PowerPlug.Cmdlets.Security;

/// <summary>
/// <para type="synopsis">Generates cryptographically secure random strings.</para>
/// <para type="description">Produces random strings suitable for passwords, API keys and tokens using the system
/// CSPRNG. The default character set is letters, digits and common symbols. Use -AlphanumericOnly to drop symbols,
/// -ExcludeAmbiguous to avoid look-alike characters (0, O, l, 1, I), or -CharacterSet to supply your own pool.</para>
/// <example>
/// <para>A 24 character password</para>
/// <code>New-RandomString -Length 24</code>
/// </example>
/// <example>
/// <para>Five alphanumeric tokens without ambiguous characters</para>
/// <code>New-RandomString -Length 32 -Count 5 -AlphanumericOnly -ExcludeAmbiguous</code>
/// </example>
/// <example>
/// <para>A numeric PIN</para>
/// <code>New-RandomString -Length 6 -CharacterSet "0123456789"</code>
/// </example>
/// </summary>
[Cmdlet(VerbsCommon.New, "RandomString")]
[Alias("nrs", "randstr")]
[OutputType(typeof(string))]
public sealed class NewRandomStringCmdlet : PowerPlugCmdlet
{
    private const string Letters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string Digits = "0123456789";
    private const string Symbols = "!@#$%^&*()-_=+[]{}|;:,.<>?";
    private const string Ambiguous = "0OolI1";

    /// <summary>
    /// <para type="description">Length of each string. Defaults to 16.</para>
    /// </summary>
    [Parameter(Position = 0)]
    [ValidateRange(1, 4096)]
    public int Length { get; set; } = 16;

    /// <summary>
    /// <para type="description">Number of strings to generate. Defaults to 1.</para>
    /// </summary>
    [Parameter]
    [ValidateRange(1, 1000)]
    public int Count { get; set; } = 1;

    /// <summary>
    /// <para type="description">Only use letters and digits.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter AlphanumericOnly { get; set; }

    /// <summary>
    /// <para type="description">Leave out characters that are easy to confuse: 0 O o l I 1.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter ExcludeAmbiguous { get; set; }

    /// <summary>
    /// <para type="description">Use exactly these characters instead of the built in sets.</para>
    /// </summary>
    [Parameter]
    [ValidateNotNullOrEmpty]
    public string? CharacterSet { get; set; }

    /// <inheritdoc />
    protected override void ProcessRecord()
    {
        var pool = BuildPool();
        if (pool.Length == 0)
        {
            WriteError(new ArgumentException("The character pool is empty after applying the options."),
                "EmptyCharacterPool", ErrorCategory.InvalidArgument, CharacterSet);
            return;
        }

        for (var i = 0; i < Count; i++)
        {
            WriteObject(Generate(pool, Length));
        }
    }

    /// <summary>
    /// Generates a random string from the given pool. Exposed for testing.
    /// </summary>
    internal static string Generate(string pool, int length)
    {
        var builder = new StringBuilder(length);
        for (var i = 0; i < length; i++)
        {
            builder.Append(pool[RandomNumberGenerator.GetInt32(pool.Length)]);
        }

        return builder.ToString();
    }

    private string BuildPool()
    {
        var pool = CharacterSet ?? (AlphanumericOnly ? Letters + Digits : Letters + Digits + Symbols);
        if (ExcludeAmbiguous)
        {
            pool = new string(pool.Where(c => !Ambiguous.Contains(c, StringComparison.Ordinal)).ToArray());
        }

        return new string(pool.Distinct().ToArray());
    }
}
