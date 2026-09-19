using PowerPlug.Internal;

namespace PowerPlug.Tests.Internal;

public class RandomTextTests
{
    private const string Letters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string Digits = "0123456789";
    private const string Symbols = "!@#$%^&*()-_=+[]{}|;:,.<>?";

    [Fact]
    public void BuildPool_DefaultsToLettersDigitsAndSymbols()
    {
        var pool = RandomText.BuildPool(null, alphanumericOnly: false, excludeAmbiguous: false);
        Assert.All(Letters + Digits + Symbols, c => Assert.Contains(c, pool));
        Assert.Equal((Letters + Digits + Symbols).Length, pool.Length);
    }

    [Fact]
    public void BuildPool_DeduplicatesCustomSet()
    {
        var pool = RandomText.BuildPool("aab", alphanumericOnly: false, excludeAmbiguous: false);
        Assert.Equal(2, pool.Length);
        Assert.Contains('a', pool);
        Assert.Contains('b', pool);
    }

    [Fact]
    public void BuildPool_ExcludeAmbiguousDropsLookAlikes()
    {
        var pool = RandomText.BuildPool(null, alphanumericOnly: true, excludeAmbiguous: true);
        Assert.DoesNotContain(pool, c => "0OolI1".Contains(c, StringComparison.Ordinal));
    }

    [Fact]
    public void GenerateUniform_UsesEveryCharacterEventually()
    {
        var text = RandomText.GenerateUniform("abc", 3000);
        Assert.Contains('a', text);
        Assert.Contains('b', text);
        Assert.Contains('c', text);
    }

    [Fact]
    public void GenerateUniform_ProducesRequestedLength()
    {
        Assert.Equal(500, RandomText.GenerateUniform("xyz", 500).Length);
    }

    [Fact]
    public void GenerateWithRequirements_HonorsEachMinimum()
    {
        var pool = RandomText.BuildPool(null, alphanumericOnly: false, excludeAmbiguous: false);
        var chars = RandomText.GenerateWithRequirements(pool, 40, minUppercase: 5, minLowercase: 5, minDigits: 5, minSymbols: 5);

        Assert.Equal(40, chars.Length);
        Assert.True(chars.Count(char.IsUpper) >= 5);
        Assert.True(chars.Count(char.IsLower) >= 5);
        Assert.True(chars.Count(char.IsDigit) >= 5);
        Assert.True(chars.Count(c => Symbols.Contains(c, StringComparison.Ordinal)) >= 5);
    }

    [Fact]
    public void GenerateWithRequirements_NoMinimumsIsPlainUniformSampling()
    {
        var chars = RandomText.GenerateWithRequirements("ab", 2000, 0, 0, 0, 0);
        Assert.Contains('a', chars);
        Assert.Contains('b', chars);
    }

    [Fact]
    public void GenerateWithRequirements_RequiredCharactersAreNotPinnedToTheFront()
    {
        // A required character is always drawn first internally; the shuffle must scatter it across positions.
        var positions = new HashSet<int>();
        for (var i = 0; i < 200; i++)
        {
            var chars = RandomText.GenerateWithRequirements("Abcdefghij", 10, minUppercase: 1, minLowercase: 0, minDigits: 0, minSymbols: 0);
            positions.Add(Array.IndexOf(chars, 'A'));
        }

        Assert.True(positions.Count > 1, "The required character should not always land at the same position.");
    }

    [Fact]
    public void GenerateWithRequirements_ThrowsWhenMinimumsExceedLength()
    {
        Assert.Throws<ArgumentException>(() => RandomText.GenerateWithRequirements("abcABC123!@#", 4, 2, 2, 2, 2));
    }

    [Fact]
    public void GenerateWithRequirements_ThrowsWhenClassIsMissingFromPool()
    {
        Assert.Throws<ArgumentException>(() => RandomText.GenerateWithRequirements("abc", 5, minUppercase: 1, minLowercase: 0, minDigits: 0, minSymbols: 0));
    }

    [Fact]
    public void ShuffleInPlace_KeepsTheSameMultisetOfCharacters()
    {
        var original = "abcdefghij".ToCharArray();
        var shuffled = (char[])original.Clone();
        RandomText.ShuffleInPlace(shuffled);

        Assert.Equal(original.OrderBy(c => c), shuffled.OrderBy(c => c));
    }
}
