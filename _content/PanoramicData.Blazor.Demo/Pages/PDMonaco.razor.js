export function configureMonaco() {

	// register ncalc themes
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

	// register a tokens provider
	monaco.languages.setMonarchTokensProvider('ncalc', getNCalcTokenProvider());

	// register rmscript themes
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

	// Register a tokens provider for the language
	monaco.languages.setMonarchTokensProvider('rmscript', getRMScriptLanguage());
}

function getNCalcTokenProvider() {
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
