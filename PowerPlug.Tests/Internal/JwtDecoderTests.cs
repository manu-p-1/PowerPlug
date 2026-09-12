using System.Text;
using PowerPlug.Internal;

namespace PowerPlug.Tests.Internal;

public class JwtDecoderTests
{
    // Header {"alg":"HS256","typ":"JWT"}, payload {"sub":"1234567890","name":"John Doe","iat":1516239022}
    private const string SampleToken =
        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";

    private static string Build(string headerJson, string payloadJson) =>
        $"{Base64Url.Encode(Encoding.UTF8.GetBytes(headerJson))}.{Base64Url.Encode(Encoding.UTF8.GetBytes(payloadJson))}.sig";

    [Fact]
    public void Decode_ReadsStandardClaims()
    {
        var token = JwtDecoder.Decode(SampleToken, DateTimeOffset.UtcNow);

        Assert.Equal("HS256", token.Algorithm);
        Assert.Equal("1234567890", token.Subject);
        Assert.Equal("John Doe", token.Payload["name"]);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1516239022), token.IssuedAt);
        Assert.Null(token.ExpiresAt);
        Assert.False(token.IsExpired);
        Assert.Equal("SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c", token.Signature);
    }

    [Fact]
    public void Decode_StripsBearerPrefix()
    {
        var token = JwtDecoder.Decode("Bearer " + SampleToken, DateTimeOffset.UtcNow);
        Assert.Equal("1234567890", token.Subject);
    }

    [Fact]
    public void Decode_ExpiryIsEvaluatedAgainstProvidedClock()
    {
        var token = Build("{\"alg\":\"none\"}", "{\"exp\":1000,\"nbf\":500,\"iss\":\"me\",\"aud\":\"you\"}");

        var before = JwtDecoder.Decode(token, DateTimeOffset.FromUnixTimeSeconds(999));
        var after = JwtDecoder.Decode(token, DateTimeOffset.FromUnixTimeSeconds(1000));

        Assert.False(before.IsExpired);
        Assert.True(after.IsExpired);
        Assert.Equal("me", after.Issuer);
        Assert.Equal("you", after.Audience);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(500), after.NotBefore);
    }

    [Fact]
    public void Decode_AudienceArrayIsJoined()
    {
        var token = Build("{}", "{\"aud\":[\"a\",\"b\"]}");
        Assert.Equal("a, b", JwtDecoder.Decode(token, DateTimeOffset.UtcNow).Audience);
    }

    [Fact]
    public void Decode_NestedObjectsBecomeOrderedDictionaries()
    {
        var token = Build("{}", "{\"roles\":[\"admin\",\"user\"],\"meta\":{\"n\":1.5,\"ok\":true,\"none\":null}}");
        var decoded = JwtDecoder.Decode(token, DateTimeOffset.UtcNow);

        var roles = Assert.IsType<object?[]>(decoded.Payload["roles"]);
        Assert.Equal(["admin", "user"], roles);

        var meta = Assert.IsType<System.Collections.Specialized.OrderedDictionary>(decoded.Payload["meta"]);
        Assert.Equal(1.5, meta["n"]);
        Assert.Equal(true, meta["ok"]);
        Assert.Null(meta["none"]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("only.two")]
    [InlineData("a.b.c.d")]
    [InlineData("!!!.!!!.!!!")]
    public void Decode_RejectsMalformedTokens(string token)
    {
        Assert.Throws<FormatException>(() => JwtDecoder.Decode(token, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Decode_RejectsNonObjectPayload()
    {
        var token = $"{Base64Url.Encode(Encoding.UTF8.GetBytes("{}"))}.{Base64Url.Encode(Encoding.UTF8.GetBytes("[1,2]"))}.sig";
        Assert.Throws<FormatException>(() => JwtDecoder.Decode(token, DateTimeOffset.UtcNow));
    }
}
