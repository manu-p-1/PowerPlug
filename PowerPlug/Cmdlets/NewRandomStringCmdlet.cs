using System;
using System.Management.Automation;
using System.Text;
using PowerPlug.Attributes;
using PowerPlug.Base;

namespace PowerPlug.Cmdlets
{
    /// <summary>
    /// <para type="synopsis">Generates a random password or string</para>
    /// <para type="description">Generates a cryptographically secure random string that can be used as a password, API key,
    /// or token. Supports configurable length and character sets.</para>
    /// <example>
    /// <para>Generate a 16-character password</para>
    /// <code>New-RandomString -Length 16</code>
    /// </example>
    /// <example>
    /// <para>Generate an alphanumeric-only string</para>
    /// <code>New-RandomString -Length 32 -AlphanumericOnly</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "RandomString")]
    [Alias("nrs", "randstr")]
    [OutputType(typeof(string))]
    [BetaCmdlet(BetaCmdlet.WarningMessage)]
    public sealed class NewRandomStringCmdlet : PowerPlugCmdletBase
    {
        private const string AlphanumericChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private const string SpecialChars = "!@#$%^&*()-_=+[]{}|;:,.<>?";

        /// <summary>
        /// <para type="description">The length of the random string (default: 16, min: 1, max: 1024)</para>
        /// </summary>
        [Parameter(Position = 0)]
        [ValidateRange(1, 1024)]
        public int Length { get; set; } = 16;

        /// <summary>
        /// <para type="description">If set, only alphanumeric characters are used (no special characters)</para>
        /// </summary>
        [Parameter]
        public SwitchParameter AlphanumericOnly { get; set; }

        /// <summary>
        /// <para type="description">Number of random strings to generate</para>
        /// </summary>
        [Parameter]
        [ValidateRange(1, 100)]
        public int Count { get; set; } = 1;

        /// <summary>
        /// Processes the PSCmdlet.
        /// </summary>
        protected override void ProcessRecord()
        {
            var charPool = AlphanumericOnly ? AlphanumericChars : AlphanumericChars + SpecialChars;

            for (var i = 0; i < Count; i++)
            {
                var sb = new StringBuilder(Length);
                for (var j = 0; j < Length; j++)
                {
                    var index = System.Security.Cryptography.RandomNumberGenerator.GetInt32(charPool.Length);
                    sb.Append(charPool[index]);
                }
                WriteObject(sb.ToString());
            }
        }
    }
}
