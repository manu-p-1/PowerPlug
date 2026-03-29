using System;
using System.Collections;
using System.Collections.Specialized;
using System.Management.Automation;
using PowerPlug.Attributes;
using PowerPlug.Base;

namespace PowerPlug.Cmdlets
{
    /// <summary>
    /// <para type="synopsis">Converts a PSObject or PSCustomObject to a hashtable</para>
    /// <para type="description">ConvertTo-HashTable recursively converts a PSObject, PSCustomObject, or any object
    /// with NoteProperties into an ordered hashtable. This is essential for splatting, JSON round-tripping,
    /// and interop with APIs that require hashtables.</para>
    /// <example>
    /// <para>Convert a PSCustomObject to a hashtable</para>
    /// <code>[pscustomobject]@{ Name = "test"; Value = 42 } | ConvertTo-HashTable</code>
    /// </example>
    /// <example>
    /// <para>Convert JSON data to a hashtable for splatting</para>
    /// <code>Get-Content config.json | ConvertFrom-Json | ConvertTo-HashTable</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsData.ConvertTo, "HashTable")]
    [Alias("toht")]
    [OutputType(typeof(Hashtable))]
    [BetaCmdlet(BetaCmdlet.WarningMessage)]
    public sealed class ConvertToHashTableCmdlet : PowerPlugCmdletBase
    {
        /// <summary>
        /// <para type="description">The input object to convert</para>
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
        [ValidateNotNull]
        public PSObject InputObject { get; set; } = null!;

        /// <summary>
        /// <para type="description">Recursively convert nested objects to hashtables</para>
        /// </summary>
        [Parameter]
        public SwitchParameter Recurse { get; set; }

        /// <summary>
        /// Processes the ConvertTo-HashTable PSCmdlet.
        /// </summary>
        protected override void ProcessRecord()
        {
            WriteObject(ConvertToHashtable(InputObject, Recurse, 0));
        }

        private static object ConvertToHashtable(PSObject obj, bool recurse, int depth)
        {
            if (depth > 64)
            {
                return obj;
            }

            var ht = new OrderedDictionary();

            foreach (var prop in obj.Properties)
            {
                if (prop.MemberType is not (PSMemberTypes.NoteProperty or PSMemberTypes.Property))
                {
                    continue;
                }

                try
                {
                    var value = prop.Value;

                    if (recurse && value != null)
                    {
                        value = ConvertValue(value, depth + 1);
                    }

                    ht[prop.Name] = value;
                }
                catch (GetValueInvocationException)
                {
                    // Skip properties that throw on access (e.g., broken ScriptProperties)
                }
            }

            return ht;
        }

        private static object? ConvertValue(object value, int depth)
        {
            if (depth > 64)
            {
                return value;
            }

            if (value is PSObject pso && pso.BaseObject is not string && pso.BaseObject is not ValueType)
            {
                return ConvertToHashtable(pso, true, depth);
            }

            if (value is IList list)
            {
                var result = new object?[list.Count];
                for (var i = 0; i < list.Count; i++)
                {
                    var item = list[i];
                    if (item is PSObject innerPso && innerPso.BaseObject is not string && innerPso.BaseObject is not ValueType)
                    {
                        result[i] = ConvertToHashtable(innerPso, true, depth);
                    }
                    else
                    {
                        result[i] = item;
                    }
                }
                return result;
            }

            return value;
        }
    }
}
