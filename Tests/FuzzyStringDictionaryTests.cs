using FluentAssertions;
using NickStrupat;

namespace Tests;

public class FuzzyStringDictionaryTests
{
	[Fact]
	public void Lookup_ReturnsMatch_WhenMatchIsWithinMaxEditDistance()
	{
		FuzzyStringDictionary fsd = new(maxEditDistance: 1);
		fsd.Add("test");

		var found = fsd.Lookup("tes");

		found.Should().BeEquivalentTo("test");
	}
}
