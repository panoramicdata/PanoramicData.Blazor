using System.Text.Json;
using System.Xml.Linq;
using AwesomeAssertions;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Guards the two build settings that automated governance sweeps have repeatedly reset, each time
/// breaking CI in a way nobody noticed for weeks.
/// </summary>
/// <remarks>
/// These are ordinary asserts over the repository's own build files. They exist because a comment
/// saying "deliberately false" is not enforcement: issue #142 was a sweep setting
/// <c>GeneratePackageOnBuild</c> back to <c>true</c> and leaving the comment that said it was false,
/// and issue #141 was a sweep re-declaring a test runner the suite could not use. A failing test is
/// the only form of "do not change this" that a sweep cannot quietly talk its way past.
/// </remarks>
public class PackagingGuardTests
{
	/// <summary>
	/// The demo app must not be packable. version.json makes only <c>main</c> a public release ref, so
	/// Nerdbank.GitVersioning stamps a prerelease version on every other branch; a packable demo then
	/// fails NU5104 on its prerelease project reference, and no pull request can go green (issue #142).
	/// </summary>
	[Fact]
	public void The_demo_project_does_not_pack()
	{
		var project = XDocument.Load(Path.Combine(RepositoryRoot, "PanoramicData.Blazor.Demo", "PanoramicData.Blazor.Demo.csproj"));

		Property(project, "IsPackable").Should().Be("false");
		Property(project, "GeneratePackageOnBuild").Should().Be("false");
	}

	/// <summary>
	/// The test runner declared in global.json must be the one this project actually uses. The suite is
	/// xunit.v3, which runs on Microsoft.Testing.Platform; declaring anything else - or previously,
	/// declaring this while the suite was xunit 2.x and VSTest-only - produces "Zero tests ran" and a
	/// non-zero exit rather than a test failure, which reads like a configuration hiccup and can sit in
	/// CI for weeks (issue #141).
	/// </summary>
	[Fact]
	public void The_declared_test_runner_matches_the_suite()
	{
		using var globalJson = JsonDocument.Parse(File.ReadAllText(Path.Combine(RepositoryRoot, "global.json")));

		globalJson.RootElement.TryGetProperty("test", out var test).Should().BeTrue(
			"global.json must declare the test runner, so the choice is explicit rather than inherited");
		test.GetProperty("runner").GetString().Should().Be("Microsoft.Testing.Platform");
	}

	private static string Property(XDocument project, string name)
		=> project.Descendants(name).LastOrDefault()?.Value.Trim()
			?? throw new InvalidOperationException($"<{name}> is not set. It must be present and false.");

	/// <summary>
	/// Walks up from the test binaries to the directory holding global.json.
	/// </summary>
	private static string RepositoryRoot
	{
		get
		{
			var directory = new DirectoryInfo(AppContext.BaseDirectory);
			while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "global.json")))
			{
				directory = directory.Parent;
			}

			return directory?.FullName
				?? throw new InvalidOperationException("Could not locate the repository root from " + AppContext.BaseDirectory);
		}
	}
}
