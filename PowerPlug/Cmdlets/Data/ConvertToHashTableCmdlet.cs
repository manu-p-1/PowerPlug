using System.Collections;
using System.Collections.Specialized;
using System.Management.Automation;
using PowerPlug.Base;

namespace PowerPlug.Cmdlets.Data;

/// <summary>
/// <para type="synopsis">Converts a PSObject or PSCustomObject to an ordered hashtable.</para>
/// <para type="description">Turns objects with note properties (for example the output of ConvertFrom-Json) into
/// ordered hashtables that can be splatted or passed to APIs expecting IDictionary. Nested objects and arrays
/// are converted too when -Recurse is set. Property order is preserved.</para>
/// <example>
/// <para>Convert a custom object</para>
/// <code>[pscustomobject]@{ Name = "test"; Value = 42 } | ConvertTo-HashTable</code>
/// </example>
/// <example>
/// <para>Load JSON config for splatting</para>
/// <code>$params = Get-Content config.json | ConvertFrom-Json | ConvertTo-HashTable -Recurse
/// Invoke-Something @params</code>
/// </example>
/// </summary>
[Cmdlet(VerbsData.ConvertTo, "HashTable")]
[Alias("toht")]
[OutputType(typeof(OrderedDictionary))]
public sealed class ConvertToHashTableCmdlet : PowerPlugCmdlet
{
    private const int MaxDepth = 64;

    /// <summary>
    /// <para type="description">The object to convert.</para>
    /// </summary>
    [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
    [ValidateNotNull]
    public PSObject InputObject { get; set; } = null!;

    /// <summary>
    /// <para type="description">Convert nested objects and collections as well.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter Recurse { get; set; }

    /// <inheritdoc />
    protected override void ProcessRecord()
    {
        if (InputObject.BaseObject is IDictionary existing)
        {
            WriteObject(existing is OrderedDictionary && !Recurse ? existing : ConvertDictionary(existing, 0));
            return;
        }

        WriteObject(ConvertObject(InputObject, 0));
    }

    private OrderedDictionary ConvertObject(PSObject obj, int depth)
    {
        var result = new OrderedDictionary();
        foreach (var property in obj.Properties)
        {
            if (property.MemberType is not (PSMemberTypes.NoteProperty or PSMemberTypes.Property or PSMemberTypes.AliasProperty))
            {
                continue;
            }

            object? value;
            try
            {
                value = property.Value;
            }
            catch (GetValueException)
            {
                // Script and code properties can throw on access. They are not data, so skip them.
                continue;
            }

            result[property.Name] = Recurse ? ConvertValue(value, depth + 1) : value;
        }

        return result;
    }

    private object? ConvertValue(object? value, int depth)
    {
        if (value is null || depth > MaxDepth)
        {
            return value;
        }

        var unwrapped = value is PSObject pso ? pso.BaseObject : value;

        switch (unwrapped)
        {
            case string or ValueType:
                return unwrapped;
            case IDictionary dictionary:
                return ConvertDictionary(dictionary, depth);
            case PSCustomObject when value is PSObject custom:
                return ConvertObject(custom, depth);
            case IEnumerable enumerable:
                return enumerable.Cast<object?>().Select(item => ConvertValue(item, depth + 1)).ToArray();
            default:
                return value is PSObject wrapped && wrapped.Properties.Any(p => p.MemberType == PSMemberTypes.NoteProperty)
                    ? ConvertObject(wrapped, depth)
                    : unwrapped;
        }
    }

    private OrderedDictionary ConvertDictionary(IDictionary dictionary, int depth)
    {
        var result = new OrderedDictionary();
        foreach (DictionaryEntry entry in dictionary)
        {
            result[entry.Key] = Recurse ? ConvertValue(entry.Value, depth + 1) : entry.Value;
        }

        return result;
    }
}
