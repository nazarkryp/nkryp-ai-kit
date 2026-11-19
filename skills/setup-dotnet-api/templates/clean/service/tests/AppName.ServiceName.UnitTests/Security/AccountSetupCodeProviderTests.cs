// @file-module:permissions
using AppName.ServiceName.Security.Impl;

namespace AppName.ServiceName.UnitTests.Security;

public sealed class AccountSetupCodeProviderTests
{
    private readonly AccountSetupCodeProvider _provider = new();

    [Fact]
    public void Generate_ReturnsABase64UrlCodeAndItsHash()
    {
        var code = _provider.Generate();

        code.Value.Should().MatchRegex("^[A-Za-z0-9_-]{22}$");
        code.Hash.Should().MatchRegex("^[0-9a-f]{64}$");
        code.Hash.Should().Be(_provider.Hash(code.Value));
    }

    [Fact]
    public void Generate_ReturnsADifferentCodeEachTime()
    {
        var first = _provider.Generate();
        var second = _provider.Generate();

        second.Value.Should().NotBe(first.Value);
        second.Hash.Should().NotBe(first.Hash);
    }

    [Fact]
    public void IsFormat_AcceptsAGeneratedCode()
    {
        _provider.IsFormat(_provider.Generate().Value).Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAA")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAA")]
    [InlineData("AAAAAAAAAAAAAAAAAAAA+/")]
    [InlineData("AAAAAAAAAAAAAAAAAAAA==")]
    [InlineData("AAAAAAAAAAAAAAAAAAAA A")]
    public void IsFormat_RejectsTextThatIsNotACode(string code)
    {
        _provider.IsFormat(code).Should().BeFalse();
    }
}
