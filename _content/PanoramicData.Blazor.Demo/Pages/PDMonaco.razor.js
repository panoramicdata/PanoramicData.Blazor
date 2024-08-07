export function configureMonaco() {

	// check and quit if rmscript already registered
	if (monaco.languages.getEncodedLanguageId('rmscript') == 0) {

		// register themes
		monaco.editor.defineTheme('rm-light', {
			base: 'vs',
			inherit: true,
			rules: [
				{ token: 'macro-name', foreground: '1000ff' },
				{ token: 'variable', foreground: '043775' },
				{ token: 'comment', foreground: '808080', background: 'ffffff' },
				{ token: 'parameter-name', foreground: '008600' },
				{ token: 'variable-assignment', foreground: 'ff00bf' },
				{ token: 'trailing-white', background: 'ffffe6' }
			],
			colors: {
			}
		});

		monaco.editor.defineTheme('rm-dark', {
			base: 'vs-dark',
			inherit: true,
			rules: [
				{ token: 'macro-name', foreground: '3dc9b0' },
				{ token: 'variable', foreground: 'f3a81e' },
				{ token: 'comment', foreground: '808080', background: 'ffffff' },
				{ token: 'parameter-name', foreground: '008600' },
				{ token: 'variable-assignment', foreground: 'ff00bf' },
				{ token: 'trailing-white', background: 'ffffe6' }
			],
			colors: {
			}
		});

		// Register language
		monaco.languages.register({ id: 'rmscript' });

		// Register a tokens provider for the language
		monaco.languages.setMonarchTokensProvider('rmscript', getRMScriptLanguage());
	}

	// check and quit if rmscript already registered
	if (monaco.languages.getEncodedLanguageId('ncalc') == 0) {

		// register themes
		monaco.editor.defineTheme('ncalc-light', {
			base: 'vs',
			inherit: true,
			rules: [
				{ token: 'date', foreground: 'd99c13' },
			],
			colors: {
			}
		});

		monaco.editor.defineTheme('ncalc-dark', {
			base: 'vs-dark',
			inherit: true,
			rules: [
				{ token: 'date', foreground: 'd99c13' },
			],
			colors: {
			}
		});

		// Register language
		monaco.languages.register({ id: 'ncalc' });

		// Register a tokens provider
		monaco.languages.setMonarchTokensProvider('ncalc', getNCalcLanguage());

		// Register a completion provider
		monaco.languages.registerCompletionItemProvider("ncalc", {
			provideCompletionItems: getNCalcCompletions
		});
	}
}

function getNCalcLanguage() {
	return {
		// Set defaultToken to invalid to see what you do not tokenize yet
		defaultToken: 'invalid',

		keywords: [
			'true', 'false'
		],

		operators: [
			'and', '&&', 'or', '||', '=', '==', '!=', '<>', '<', '<=', '>', '>=', 'in', 'not in', '+', '-', '*', '/', '%', '&', '|', '^', '<<', '>>', '!', 'not', '~', '**'
		],

		// we include these common regular expressions
		symbols: /[=><!~?:&|+\-*\/\^%]+/,

		// C# style strings
		escapes: /\\(?:[abfnrtv\\"']|x[0-9A-Fa-f]{1,4}|u[0-9A-Fa-f]{4}|U[0-9A-Fa-f]{8})/,

		brackets: [
			{ open: '(', close: ')', token: 'delimiter.parenthesis' }
		],

		// The main tokenizer for our languages
		tokenizer: {
			root: [
				// identifiers and keywords
				[/[a-zA-Z]\w*/, {
					cases: {
						'@keywords': 'keyword',
						'@default': 'identifier'
					}
				}],
				[/\[\w+\]/, 'variable'],

				// whitespace
				{ include: '@whitespace' },

				// delimiters and operators
				[/[()]/, '@brackets'],
				[/@symbols/, {
					cases: {
						'@operators': 'operator',
						'@default': ''
					}
				}],

				// dates
				[/#.*#/, 'date'],

				// numbers
				[/[\-+]?\d*\.\d+([eE][\-+]?\d+)?/, 'number.float'],
				[/0[xX][0-9a-fA-F]+/, 'number.hex'],
				[/\d+/, 'number'],

				// delimiter: after number because of .\d floats
				[/[;,.]/, 'delimiter'],

				// strings
				[/'([^'\\]|\\.)*$/, 'string.invalid'],  // non-teminated string
				[/'/, 'string', '@string_single']

			],

			comment: [
				[/[^\/*]+/, 'comment'],
				[/\/\*/, 'comment', '@push'],    // nested comment
				["\\*/", 'comment', '@pop'],
				[/[\/*]/, 'comment']
			],

			string_single: [
				[/[^\\']+/, 'string'],
				[/@escapes/, 'string.escape'],
				[/\\./, 'string.escape.invalid'],
				[/'/, { token: 'string.quote', bracket: '@close', next: '@pop' }]
			],

			whitespace: [
				[/[ \t\r\n]+/, 'white'],
				[/\/\*/, 'comment', '@comment'],
				[/\/\/.*$/, 'comment'],
			],
		}
	}
}

function getNCalcCompletions(model, position) {

	// find out if we are completing a property in the 'dependencies' object.
	var textUntilPosition = model.getValueInRange({
		startLineNumber: 1,
		startColumn: 1,
		endLineNumber: position.lineNumber,
		endColumn: position.column,
	});

	//var match = textUntilPosition.match(
	//	/"dependencies"\s*:\s*\{\s*("[^"]*"\s*:\s*"[^"]*"\s*,\s*)*([^"]*)?$/
	//);
	//if (!match) {
	//	return { suggestions: [] };
	//}
	var word = model.getWordUntilPosition(position);
	var range = {
		startLineNumber: position.lineNumber,
		endLineNumber: position.lineNumber,
		startColumn: word.startColumn,
		endColumn: word.endColumn,
	};

	return {
		suggestions: [
			{
				label: 'Abs',
				kind: monaco.languages.CompletionItemKind.Function,
				documentation: "Returns the absolute value of a number.",
				insertText: 'Abs(',
				range: range,
			},
			{
				label: 'Acos',
				kind: monaco.languages.CompletionItemKind.Function,
				documentation: "Returns the angle whose cosine is the specified number.",
				insertText: 'Acos(',
				range: range,
			},
			{
				label: 'Acosh(',
				kind: monaco.languages.CompletionItemKind.Function,
				documentation: "Returns the angle whose hyperbolic cosine is the specified number.",
				insertText: 'Acosh(',
				range: range,
			},
			{
				label: 'Asin',
				kind: monaco.languages.CompletionItemKind.Function,
				documentation: "Returns the angle whose sine is the specified number.",
				insertText: 'Asin(',
				range: range,
			},
			{
				label: 'Asinh(',
				kind: monaco.languages.CompletionItemKind.Function,
				documentation: "Returns the angle whose hyperbolic sine is the specified number.",
				insertText: 'Asinh(',
				range: range,
			},
			{
				label: 'Atan',
				kind: monaco.languages.CompletionItemKind.Function,
				documentation: "Returns the angle whose tangent is the specified number.",
				insertText: 'Atan(',
				range: range,
			},
			{
				label: 'Atan2',
				kind: monaco.languages.CompletionItemKind.Function,
				documentation: "Returns the angle whose tangent is the quotient of two specified numbers.",
				insertText: 'Atan2(',
				range: range,
			},
			{
				label: 'Atan2',
				kind: monaco.languages.CompletionItemKind.Function,
				documentation: "Returns the angle whose hyperbolic tangent is the specified number.",
				insertText: 'Atanh(',
				range: range,
			},
			{
				label: 'BigMul',
				kind: monaco.languages.CompletionItemKind.Function,
				documentation: "Produces the full product of two numbers.",
				insertText: 'BigMul(',
				range: range,
			},
			{
				label: 'BitDecrement',
				kind: monaco.languages.CompletionItemKind.Function,
				documentation: "Returns the largest value that compares less than a specified value.",
				insertText: 'BitDecrement(',
				range: range,
			},
			{
				label: 'BitIncrement',
				kind: monaco.languages.CompletionItemKind.Function,
				documentation: "Returns the smallest value that compares greater than a specified value.",
				insertText: 'BitIncrement(',
				range: range,
			},
			{
				label: 'Cbrt',
				kind: monaco.languages.CompletionItemKind.Function,
				documentation: "Returns the cube root of a specified number.",
				insertText: 'Cbrt(',
				range: range,
			},
			{
				label: 'Ceiling',
				kind: monaco.languages.CompletionItemKind.Function,
				documentation: "Returns the smallest integral value that is greater than or equal to the specified number.",
				insertText: 'Ceiling(',
				range: range,
			},
			{
				label: 'Clamp',
				kind: monaco.languages.CompletionItemKind.Function,
				documentation: "Returns value clamped to the inclusive range of min and max.",
				insertText: 'Clamp(',
				range: range,
			},
			{
				label: 'CopySign',
				kind: monaco.languages.CompletionItemKind.Function,
				documentation: "Returns a value with the magnitude of x and the sign of y.",
				insertText: 'CopySign(',
				range: range,
			},
			{
				label: 'Cos',
				kind: monaco.languages.CompletionItemKind.Function,
				documentation: "Returns the cosine of the specified angle.",
				insertText: 'Cos(',
				range: range,
			},
			{
				label: 'Cosh',
				kind: monaco.languages.CompletionItemKind.Function,
				documentation: "Returns the hyperbolic cosine of the specified angle.",
				insertText: 'Cosh(',
				range: range,
			},
			{
				label: 'DivRem',
				kind: monaco.languages.CompletionItemKind.Function,
				documentation: "Produces the quotient and the remainder of two numbers.",
				insertText: 'DivRem(',
				range: range,
			},
			{
				label: 'Exp',
				kind: monaco.languages.CompletionItemKind.Function,
				documentation: "Returns e raised to the specified power.",
				insertText: 'Exp(',
				range: range,
			},
			{
				label: 'Floor',
				kind: monaco.languages.CompletionItemKind.Function,
				documentation: "Returns the largest integral value less than or equal to the specified decimal number.",
				insertText: 'Floor(',
				range: range,
			},
			{
				label: 'FusedMultiplyAdd',
				kind: monaco.languages.CompletionItemKind.Function,
				documentation: "Returns (x * y) + z, rounded as one ternary operation.",
				insertText: 'FusedMultiplyAdd(',
				range: range,
			},
			{
				label: 'IEEERemainder',
				kind: monaco.languages.CompletionItemKind.Function,
				documentation: "Returns the remainder resulting from the division of a specified number by another specified number.",
				insertText: 'IEEERemainder(',
				range: range,
			},
			{
				label: 'ILogB',
				kind: monaco.languages.CompletionItemKind.Function,
				documentation: "Returns the base 2 integer logarithm of a specified number.",
				insertText: 'ILogB(',
				range: range,
			},
			{
				label: 'Log',
				kind: monaco.languages.CompletionItemKind.Function,
				documentation: "Returns the natural (base e) logarithm of a specified number.",
				insertText: 'Log(',
				range: range,
			},
			{
				label: 'Log10',
				kind: monaco.languages.CompletionItemKind.Function,
				documentation: "Returns the base 10 logarithm of a specified number.",
				insertText: 'Log10(',
				range: range,
			},
			{
				label: 'Log2',
				kind: monaco.languages.CompletionItemKind.Function,
				documentation: "Returns the base 2 logarithm of a specified number.",
				insertText: 'Log2(',
				range: range,
			},
			{
				label: 'Max',
				kind: monaco.languages.CompletionItemKind.Function,
				documentation: "Returns the larger of two numbers.",
				insertText: 'Max(',
				range: range,
			},
			{
				label: 'MaxMagnitude',
				kind: monaco.languages.CompletionItemKind.Function,
				documentation: "Returns the larger magnitude of two numbers.",
				insertText: 'MaxMagnitude(',
				range: range,
			},
			{
				label: 'MaxMagnitude',
				kind: monaco.languages.CompletionItemKind.Function,
				documentation: "Returns the larger magnitude of two double- precision floating - point numbers.",
				insertText: 'MaxMagnitude(',
				range: range,
			},
			{
				label: 'Min',
				kind: monaco.languages.CompletionItemKind.Function,
				documentation: "Returns the smaller of two 8-bit unsigned integers.",
				insertText: 'Min(',
				range: range,
			},
			{
				label: 'MinMagnitude',
				kind: monaco.languages.CompletionItemKind.Function,
				documentation: "Returns the smaller magnitude of two double- precision floating - point numbers.",
				insertText: 'MinMagnitude(',
				range: range,
			},
			{
				label: 'Pow',
				kind: monaco.languages.CompletionItemKind.Function,
				documentation: "Returns a specified number raised to the specified power.",
				insertText: 'Pow(',
				range: range,
			},
			{
				label: 'ReciprocalEstimate',
				kind: monaco.languages.CompletionItemKind.Function,
				documentation: "Returns an estimate of the reciprocal of a specified number.",
				insertText: 'ReciprocalEstimate(',
				range: range,
			},
			{
				label: 'ReciprocalSqrtEstimate',
				kind: monaco.languages.CompletionItemKind.Function,
				documentation: "Returns an estimate of the reciprocal square root of a specified number.",
				insertText: 'ReciprocalSqrtEstimate(',
				range: range,
			},
			{
				label: 'Round',
				kind: monaco.languages.CompletionItemKind.Function,
				documentation: "Rounds a decimal value to the nearest integral value, and rounds midpoint values to the nearest even number.",
				insertText: 'Round(',
				range: range,
			},
			{
				label: 'ScaleB',
				kind: monaco.languages.CompletionItemKind.Function,
				documentation: "Returns x * 2^n computed efficiently.",
				insertText: 'ScaleB(',
				range: range,
			},
			{
				label: 'Sign',
				kind: monaco.languages.CompletionItemKind.Function,
				documentation: "Returns an integer that indicates the sign of a number.",
				insertText: 'Sign(',
				range: range,
			},
			{
				label: 'Sin',
				kind: monaco.languages.CompletionItemKind.Function,
				documentation: "Returns the sine of the specified angle.",
				insertText: 'Sin(',
				range: range,
			},
			{
				label: 'SinCos',
				kind: monaco.languages.CompletionItemKind.Function,
				documentation: "Returns the sine and cosine of the specified angle.",
				insertText: 'SinCos(',
				range: range,
			},
			{
				label: 'Sinh',
				kind: monaco.languages.CompletionItemKind.Function,
				documentation: "Returns the hyperbolic sine of the specified angle.",
				insertText: 'Sinh(',
				range: range,
			},
			{
				label: 'Sqrt',
				kind: monaco.languages.CompletionItemKind.Function,
				documentation: "Returns the square root of a specified number.",
				insertText: 'Sqrt(',
				range: range,
			},
			{
				label: 'Tan',
				kind: monaco.languages.CompletionItemKind.Function,
				documentation: "Returns the tangent of the specified angle.",
				insertText: 'Tan(',
				range: range,
			},
			{
				label: 'Tanh',
				kind: monaco.languages.CompletionItemKind.Function,
				documentation: "Returns the hyperbolic tangent of the specified angle.",
				insertText: 'Tanh(',
				range: range,
			},
			{
				label: 'Truncate',
				kind: monaco.languages.CompletionItemKind.Function,
				documentation: "Calculates the integral part of a specified number.",
				insertText: 'Truncate(',
				range: range,
			}
		],
	};
}

function getMathCompletions(model, position) {

}

function getRMScriptLanguage() {
	// separated here so can cut and paste into Monaco/Monarch playground
	return {
		// Set defaultToken to invalid to see what you do not tokenize yet
		defaultToken: 'invalid',
		ignoreCase: true,
		brackets: [
			['[', ']', 'delimiter.square']
		],

		keywords: [
			'true', 'false'
		],

		// we include these common regular expressions
		symbols: /[=><!~?:&|+\-*\/\^%;]+/,

		operators: [
			'!=', '=', '=>'
		],

		// The main tokenizer for our languages
		tokenizer: {

			// first state is default
			root: [
				// identify start of macro
				[/\[/, '@brackets', 'macro'],
				// whitespace
				{ include: '@whitespace' },
				// any char is valid outside of macro
				[/./, '']
			],

			whitespace: [
				[/[ \t\r\n]+$/, 'trailing-white'],
				[/[ \t\r\n]+/, 'white'],
				[/\/\/.*$/, 'comment'],
			],

			macro: [
				// macro name
				[/[\w\.?!/+-=]*:/, 'macro-name'],
				[/@symbols/, {
					cases: {
						'=>': 'variable-assignment',
						'@operators': 'operators',
						'@default': ''
					}
				}],
				// parameter names
				[/\w+(?==)/, 'parameter-name'],
				// keywords
				[/[a-z_$][\w$]*/, { cases: { '@keywords': 'keyword', '@default': 'identifier' } }],
				// variables
				[/{\w+}/, 'variable'],
				// trailing whitespace
				[/[ \t\r\n]+$/, 'trailing-white'],
				// whitespace
				[/[ \t\r\n]+/, 'white'],
				// commas
				[/[,]/, 'delimiter'],
				// end state and return to root state
				[/\]/, '@brackets', '@pop'],
				// back quoted parameter value
				[/[`]/, 'string', 'backQuotedValue'],
				[/["]/, 'string', 'doubleQuotedValue'],
				[/[']/, 'string', 'singleQuotedValue'],
				// json parameter value
				[/<json>/, { token: 'variable.value', next: 'jsonValue', nextEmbedded: 'json' }],
				// numbers
				[/\d*\.\d+([eE][\-+]?\d+)?/, 'number.float'],
				[/0[xX][0-9a-fA-F]+/, 'number.hex'],
				[/\d+/, 'number'],
			],

			singleQuotedValue: [
				// any char other then delimiting char is valid
				[/[^\\']+/, 'string'],
				// delimiting char pops back to macro state
				[/[']/, { token: 'string', bracket: '@close', next: '@pop' }]
			],

			doubleQuotedValue: [
				// any char other then delimiting char is valid
				[/[^\\"]+/, 'string'],
				// delimiting char pops back to macro state
				[/["]/, { token: 'string', bracket: '@close', next: '@pop' }]
			],

			backQuotedValue: [
				// any char other then delimiting char is valid
				[/[^\\`]+/, 'string'],
				// delimiting char pops back to macro state
				[/[`]/, { token: 'string', bracket: '@close', next: '@pop' }]
			],

			jsonValue: [
				// delimiting char pops back to macro state
				[/<\/json>/, { token: 'variable.value', bracket: '@close', next: '@pop', nextEmbedded: '@pop' }],
				// any char other then delimiting char is valid
				[/./, '']
			]
		},
	}
}