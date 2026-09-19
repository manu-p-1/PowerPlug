namespace PowerPlug.Tests.Infrastructure;

/// <summary>
/// Every integration test class joins this collection so they share one runspace and run sequentially.
/// Several PowerShell runspaces starting at once in one process is slow and has deadlocked in practice.
/// </summary>
[CollectionDefinition(Name)]
public sealed class PowerShellTestGroup : ICollectionFixture<PowerShellFixture>
{
    public const string Name = "PowerShell";
}
