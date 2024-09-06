export function configureMonaco() {

	// register custom themes
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
