var _objRef = null;

export function configureMonaco(objRef) {

	if (objRef) {
		_objRef = objRef;
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

		// Register a signature help provider
		monaco.languages.registerSignatureHelpProvider("ncalc", {
			provideSignatureHelp: getNCalcSignatureHelp,
			signatureHelpTriggerCharacters: ['(', ',']
		});

	}
}

function getActiveParameter(model, position) {
	const textUntilPosition = model.getValueInRange({
		startLineNumber: position.lineNumber,
		startColumn: 1,
		endLineNumber: position.lineNumber,
		endColumn: position.column
	});
	// basic counting of commas since opening parenthesis
	const openParenthesisIndex = textUntilPosition.lastIndexOf('(');
	if (openParenthesisIndex === -1) {
		return 0;
	}
	const paramsString = textUntilPosition.substring(openParenthesisIndex + 1);
	return paramsString.split(',').length - 1;
}

function getActiveSignature(signatures, activeParameter) {
	// selects first signature with at least N parameters
	for (let i = 0; i < signatures.length; i++) {
		if (signatures[i].parameters.length >= (activeParameter + 1)) {
			return i;
		}
	}
	return 0; // unknown - return first
}

function getLastFunctionName(text) {
	// regular expression to match function names followed by an opening parenthesis
	const functionRegex = /([a-zA-Z_$][0-9a-zA-Z_$]*)\s*\(/g;
	let match;
	let lastFunctionName = null;
	// iterate over all matches and store the last one
	while ((match = functionRegex.exec(text)) !== null) {
		lastFunctionName = match[1];
	}
	return lastFunctionName;
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

async function getNCalcCompletions(model, position) {

	var textUntilPosition = model.getValueInRange({
		startLineNumber: 1,
		startColumn: 1,
		endLineNumber: position.lineNumber,
		endColumn: position.column,
	});

	var word = model.getWordUntilPosition(position);

	var range = {
		startLineNumber: position.lineNumber,
		endLineNumber: position.lineNumber,
		startColumn: word.startColumn,
		endColumn: word.endColumn,
	};

	// call out to C# to fetch completion items
	var items = [];
	if (_objRef) {
		items = await _objRef.invokeMethodAsync("GetCompletions", range);
	}

	// return result
	return { suggestions: items };
}

async function getNCalcSignatureHelp(model, position, token, context) {

	// determine current function
	var textUntilPosition = model.getValueInRange({
		startLineNumber: 1,
		startColumn: 1,
		endLineNumber: position.lineNumber,
		endColumn: position.column,
	});
	var functionName = getLastFunctionName(textUntilPosition);

	// determine active parameter
	var activeParameter = getActiveParameter(model, position);

	// call out to C# to fetch signatures
	var signatures = [];
	if (_objRef) {
		signatures = await _objRef.invokeMethodAsync("GetSignatures", functionName);
	}

	// determine active signature
	var activeSignature = getActiveSignature(signatures, activeParameter);

	// return result
	return {
		value: {
			signatures: signatures,
			activeSignature: activeSignature,
			activeParameter: activeParameter
		},
		dispose: function () {
		}
	};
}