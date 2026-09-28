using AwesomeAssertions;
using PanoramicData.Blazor.Models.Monaco;

namespace PanoramicData.Blazor.Test.Models.Monaco;

/// <summary>Tests for <see cref="Language"/>, <see cref="SignatureInformation"/> and <see cref="ParameterInformation"/>.</summary>
public class LanguageTests
{
	/// <summary>A new language uses brackets for calls and a colon for optional parameters, and shows no completions.</summary>
	[Fact]
	public void Language_DefaultsAndRoundTrip()
	{
		var language = new Language();
		language.FunctionDelimiter.Should().Be('(');
		language.Id.Should().BeEmpty();
		language.OptionalParameterPostfix.Should().Be(':');
		language.ShowCompletions.Should().BeFalse();
		language.SignatureHelpTriggers.Should().BeEmpty();

		language.FunctionDelimiter = '[';
		language.Id = "ncalc";
		language.OptionalParameterPostfix = '?';
		language.ShowCompletions = true;
		language.SignatureHelpTriggers = ['(', ','];

		language.FunctionDelimiter.Should().Be('[');
		language.Id.Should().Be("ncalc");
		language.OptionalParameterPostfix.Should().Be('?');
		language.ShowCompletions.Should().BeTrue();
		language.SignatureHelpTriggers.Should().Equal('(', ',');
	}

	/// <summary>Signature and parameter information start empty and round-trip.</summary>
	[Fact]
	public void SignatureInformation_DefaultsAndRoundTrip()
	{
		var signature = new SignatureInformation();
		signature.ActiveParameter.Should().BeNull();
		signature.Documentation.Should().BeEmpty();
		signature.Label.Should().BeEmpty();
		signature.Parameters.Should().BeEmpty();
		var parameter = new ParameterInformation();
		parameter.Label.Should().BeEmpty();
		parameter.Documentation.Should().BeEmpty();

		parameter.Label = "int a";
		parameter.Documentation = "first";
		signature.ActiveParameter = 0;
		signature.Documentation = "Adds";
		signature.Label = "Add(int a)";
		signature.Parameters = [parameter];

		signature.ActiveParameter.Should().Be(0);
		signature.Documentation.Should().Be("Adds");
		signature.Label.Should().Be("Add(int a)");
		signature.Parameters.Should().ContainSingle().Which.Documentation.Should().Be("first");
	}
}
