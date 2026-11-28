// PDStudioDemo Monaco Editor Enhancement Script

// Function to detect system dark mode preference
export function isSystemDarkMode() {
	return (
		window.matchMedia &&
		window.matchMedia("(prefers-color-scheme: dark)").matches
	);
}

// NCalc syntax highlighting themes
const NCALC_LIGHT_THEME = {
	base: "vs",
	inherit: true,
	rules: [
		{ token: "keyword", foreground: "0000ff" },
		{ token: "number", foreground: "098658" },
		{ token: "string", foreground: "a31515" },
		{ token: "function", foreground: "795e26" },
		{ token: "variable", foreground: "001080" },
		{ token: "operator", foreground: "0000ff" },
		{ token: "date", foreground: "d99c13" },
		{ token: "comment", foreground: "008000" },
	],
	colors: {},
};

const NCALC_DARK_THEME = {
	base: "vs-dark",
	inherit: true,
	rules: [
		{ token: "keyword", foreground: "569cd6" },
		{ token: "number", foreground: "b5cea8" },
		{ token: "string", foreground: "ce9178" },
		{ token: "function", foreground: "dcdcaa" },
		{ token: "variable", foreground: "9cdcfe" },
		{ token: "operator", foreground: "569cd6" },
		{ token: "date", foreground: "d99c13" },
		{ token: "comment", foreground: "6a9955" },
	],
	colors: {},
};

// NCalc language tokenizer for syntax highlighting
const NCALC_LANGUAGE = {
	defaultToken: "invalid",
	keywords: ["true", "false", "and", "or", "not", "in", "if", "null"],
	operators: [
		"and",
		"&&",
		"or",
		"||",
		"=",
		"==",
		"!=",
		"<>",
		"<",
		"<=",
		">",
		">=",
		"in",
		"not in",
		"+",
		"-",
		"*",
		"/",
		"%",
		"&",
		"|",
		"^",
		"<<",
		">>",
		"!",
		"not",
		"~",
		"**",
	],
	functions: [
		// Math functions
		"Abs",
		"Acos",
		"Asin",
		"Atan",
		"Atan2",
		"Ceiling",
		"Cos",
		"Cosh",
		"Exp",
		"Floor",
		"IEEERemainder",
		"Log",
		"Log10",
		"Max",
		"Min",
		"Pow",
		"Round",
		"Sign",
		"Sin",
		"Sinh",
		"Sqrt",
		"Tan",
		"Tanh",
		"Truncate",
		// Custom functions
		"if",
		"in",
		"isnull",
		"AddDays",
		"AddHours",
		"Hour",
	],
	variables: ["Pi", "E", "Now", "Today", "x", "y"],
	symbols: /[=><!~?:&|+\-*/^%]+/,
	escapes:
		/\\(?:[abfnrtv\\"']|x[0-9A-Fa-f]{1,4}|u[0-9A-Fa-f]{4}|U[0-9A-Fa-f]{8})/,
	brackets: [{ open: "(", close: ")", token: "delimiter.parenthesis" }],
	tokenizer: {
		root: [
			// Functions
			[
				/[a-zA-Z]\w*(?=\()/,
				{
					cases: {
						"@functions": "function",
						"@default": "identifier",
					},
				},
			],

			// Variables and identifiers
			[
				/[a-zA-Z]\w*/,
				{
					cases: {
						"@keywords": "keyword",
						"@variables": "variable",
						"@default": "identifier",
					},
				},
			],

			// Whitespace
			{ include: "@whitespace" },

			// Delimiters and operators
			[/[()]/, "@brackets"],
			[
				/@symbols/,
				{
					cases: {
						"@operators": "operator",
						"@default": "",
					},
				},
			],

			// Numbers
			[/[-+]?\d*\.\d+([eE][-+]?\d+)?/, "number.float"],
			[/0[xX][0-9a-fA-F]+/, "number.hex"],
			[/\d+/, "number"],

			// Dates (between # symbols)
			[/#.*#/, "date"],

			// Delimiter
			[/[;,.]/, "delimiter"],

			// Strings
			[/'([^'\\]|\\.)*$/, "string.invalid"],
			[/'/, "string", "@string_single"],
			[/\x22([^\x22\\]|\\.)*$/, "string.invalid"],
			[/\x22/, "string", "@string_double"],
		],
		string_single: [
			[/[^\\']+/, "string"],
			[/@escapes/, "string.escape"],
			[/\\./, "string.escape.invalid"],
			[/'/, { token: "string.quote", bracket: "@close", next: "@pop" }],
		],
		string_double: [
			[/[^\\\x22]+/, "string"],
			[/@escapes/, "string.escape"],
			[/\\./, "string.escape.invalid"],
			[/\x22/, { token: "string.quote", bracket: "@close", next: "@pop" }],
		],
		whitespace: [
			[/[ \t\r\n]+/, "white"],
			[/\/\*/, "comment", "@comment"],
			[/\/\/.*$/, "comment"],
		],
		comment: [
			[/[^/*]+/, "comment"],
			[/\/\*/, "comment", "@push"],
			["\\*/", "comment", "@pop"],
			[/[/*]/, "comment"],
		],
	},
};

// Basic SQL syntax highlighting themes (Monaco has built-in SQL, but we can enhance it)
const SQL_LIGHT_THEME = {
	base: "vs",
	inherit: true,
	rules: [
		{ token: "keyword.sql", foreground: "0000ff", fontStyle: "bold" },
		{ token: "string.sql", foreground: "a31515" },
		{ token: "number", foreground: "098658" },
		{ token: "operator.sql", foreground: "0000ff" },
	],
	colors: {},
};

const SQL_DARK_THEME = {
	base: "vs-dark",
	inherit: true,
	rules: [
		{ token: "keyword.sql", foreground: "569cd6", fontStyle: "bold" },
		{ token: "string.sql", foreground: "ce9178" },
		{ token: "number", foreground: "b5cea8" },
		{ token: "operator.sql", foreground: "569cd6" },
	],
	colors: {},
};

export function configurePDStudioMonaco() {
	// Monaco is loaded by the host page as a global script
	const monaco = window.monaco;
	if (!monaco || !monaco.editor) {
		console.warn("Monaco Editor not available for PDStudio configuration");
		return;
	}

	monaco.editor.defineTheme("ncalc-light", NCALC_LIGHT_THEME);
	monaco.editor.defineTheme("ncalc-dark", NCALC_DARK_THEME);
	monaco.languages.setMonarchTokensProvider("ncalc", NCALC_LANGUAGE);
	monaco.editor.defineTheme("sql-light", SQL_LIGHT_THEME);
	monaco.editor.defineTheme("sql-dark", SQL_DARK_THEME);

	console.log("PDStudio Monaco language configurations loaded successfully");
}

// Auto-configure when module loads
if (typeof window !== "undefined" && window.monaco) {
	configurePDStudioMonaco();
} else if (typeof window !== "undefined") {
	// Wait for Monaco to load
	window.addEventListener("load", () => {
		setTimeout(configurePDStudioMonaco, 100);
	});
}
