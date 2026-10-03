using PanoramicData.Blazor.Models.Monaco;

namespace PanoramicData.Blazor.Demo.Pages;

/// <summary>
/// Parameter definitions of the example rmscript macros.
/// </summary>
internal static class RmscriptParameters
{
	public static MethodCache.Parameter[] GetListAddParameters()
	{
		return
		[
			new MethodCache.Parameter {
				Name = "concat",
				Description = "When adding lists to a list, this will add each individual item onto the end of the list, rather than adding the list itself onto the existing list."
			},
			new MethodCache.Parameter {
				Name = "listDelimiter",
				Description = "In Legacy Mode only, the delimiter to use between multiple items in the output."
			},
			new MethodCache.Parameter {
				Name = "value",
				Description = "The value to add."
			}
		];
	}

	public static MethodCache.Parameter[] GetColorParameters()
	{
		return
		[
			new MethodCache.Parameter {
				Name = "value",
				Description = "The colour to use to increase intensity of the input colour.",
				Type = typeof(string),
			},
			new MethodCache.Parameter {
				Name = "intensifyColor",
				Description = "The colour to use to increase intensity of the input colour.",
				Type = typeof(string),
			},
			new MethodCache.Parameter {
				Name = "intensifyPercent",
				Description = "The percentage intensity to apply.",
				Type = typeof(double),
			}
		];
	}

	public static MethodCache.Parameter[] GetStringParameters()
	{
		return
		[
			new MethodCache.Parameter {
				Name = "value",
				Description = "The string value.",
				Type = typeof(string),
			},
			new MethodCache.Parameter {
				Name = "selectDistinct",
				Description = "Whether to select distinct values in a string list.",
				IsOptional = true
			},
			new MethodCache.Parameter {
				Name = "find",
				Description = "The string value(s) to find in the value."
			},
			new MethodCache.Parameter {
				Name = "replaceWith",
				Description = "The string value(s) to use to replace the string specified in the find parameter."
			},
			new MethodCache.Parameter {
				Name = "regexFind",
				Description = "The Regex pattern(s) to find in the value."
			},
			new MethodCache.Parameter {
				Name = "regexReplaceWith",
				Description = "The Regex string value(s) to use to replace the string specified in the regexFind parameter."
			}
		];
	}

	public static MethodCache.Parameter[] GetCommonRmscriptParameters()
	{
		return
		[
			new MethodCache.Parameter {
				Name = "comment",
				Description = "Add a comment to make your document template more readable. The comment is discarded in the output document",
				IsOptional = true,
				Type = typeof(string),
			},
			new MethodCache.Parameter {
				Name = "failureText",
				Description = "The text to display should the macro fail to execute. Note that a poorly-specified macro (e.g. omitting mandatory parameters) will still result in an error message.",
				IsOptional = true,
				Type = typeof(string),
			},
			new MethodCache.Parameter {
				Name = "warning",
				Description = "If specified, adds a warning message for this macro. This is processed as an NCalc, and the warning message will ALWAYS be present and will be the value of the evaluated NCalc expression.",
				IsOptional = true,
				Type = typeof(string),
			},
			new MethodCache.Parameter {
				Name = "obfuscation",
				Description = "Obfuscation type. Use obfuscation to write reports where sensitive data is hidden.",
				IsOptional = true,
				Type = typeof(string),
			}
		];
	}

	public static MethodCache.Parameter[] GetCommonRmscriptEvaluationParameters()
	{
		return
		[
			new MethodCache.Parameter {
				Name = "mode",
				Description = "The mode in which variables are stored. In the legacy mode (default for Schedules), the variable created is a string and formatted.",
				IsOptional = true,
				Type = typeof(string),
			},
			new MethodCache.Parameter {
				Name = "errorOnOverflow",
				Description = "Should NCalc expression evaluation throw error on Overflow.",
				IsOptional = true,
				Type = typeof(bool),
			},
			new MethodCache.Parameter {
				Name = "storeAs",
				Description = "Name of variable to store value in.",
				IsOptional = true,
				Type = typeof(bool),
			},
			new MethodCache.Parameter {
				Name = "if",
				Description = "The condition that must be true in order for the macro to be executed/evaluated.",
				IsOptional = true,
				Type = typeof(bool),
			}
		];
	}
}
