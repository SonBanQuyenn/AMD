using PollService.Api;
using Xunit;

namespace PollService.Api.Tests;

public class ShortCodeGeneratorTests
{
    [Fact]
    public void Generate_DefaultLength_Returns6Characters()
    {
        var code = ShortCodeGenerator.Generate();

        Assert.Equal(6, code.Length);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(10)]
    public void Generate_CustomLength_ReturnsRequestedLength(int length)
    {
        var code = ShortCodeGenerator.Generate(length);

        Assert.Equal(length, code.Length);
    }

    [Fact]
    public void Generate_OnlyUsesAlphanumericCharacters()
    {
        const string allowed = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        var code = ShortCodeGenerator.Generate(50);

        Assert.All(code, c => Assert.Contains(c, allowed));
    }

    [Fact]
    public void Generate_CalledManyTimes_ProducesDistinctCodes()
    {
        // Not a proof of uniqueness (it's still a random alphabet, not a UUID),
        // but if this ever starts failing it means the RNG is broken, not unlucky.
        var codes = Enumerable.Range(0, 200)
            .Select(_ => ShortCodeGenerator.Generate())
            .ToHashSet();

        Assert.True(codes.Count > 195, "Expected close to 200 distinct 6-character codes out of 200 draws.");
    }
}
