using AutoFixture;
using AutoFixture.AutoMoq;

namespace RecipeFetcher.Tests;

public sealed class TestDataFixture
{
    public IFixture Fixture { get; } = new Fixture().Customize(new AutoMoqCustomization());
}
