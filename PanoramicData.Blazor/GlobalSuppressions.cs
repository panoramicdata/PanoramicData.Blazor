// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
	"Naming",
	"CA1711:Identifiers should not have incorrect suffix",
	Justification = "Project policy",
	Scope = "namespaceanddescendants",
	Target = "~N:PanoramicData.Blazor"
)]
[assembly: SuppressMessage(
	"Performance",
	"CA1848:Use the LoggerMessage delegates",
	Justification = "Performance gain not worth the effort",
	Scope = "namespaceanddescendants",
	Target = "~N:PanoramicData.Blazor"
)]
