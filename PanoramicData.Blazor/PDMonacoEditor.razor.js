var _objRef = null;

export function initialize(objRef) {
	if (objRef) {
		_objRef = objRef;
	}
}

export function registerLanguage(id, completions, triggerChars) {
	if (monaco) {
		monaco.languages.register({ id: id });
		if (completions) {
			monaco.languages.registerCompletionItemProvider(id, {
				provideCompletionItems: getCompletions
			});
			if (triggerChars) {
				monaco.languages.registerSignatureHelpProvider(id, {
					provideSignatureHelp: getSignatureHelp,
					signatureHelpTriggerCharacters: triggerChars
				});
			}
		}
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


async function getCompletions(model, position) {

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

async function getSignatureHelp(model, position, token, context) {

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