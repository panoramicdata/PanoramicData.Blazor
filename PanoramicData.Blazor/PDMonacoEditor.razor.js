var _objRef = null;
var languageOptions = {
};

export function initialize(objRef) {
	if (objRef) {
		_objRef = objRef;
	}
}

export function registerLanguage(id, language) {
	if (monaco) {

		// does language already exist?
		var languages = monaco.languages.getLanguages();
		const exists = languages.some(obj => obj.id === id);
		if (exists) {
			return false;
		}
		monaco.languages.register({ id: id });
		if (language.showCompletions) {
			monaco.languages.registerCompletionItemProvider(id, {
				provideCompletionItems: getCompletions,
				resolveCompletionItem: resolveCompletionItem
			});
			if (language.signatureHelpTriggers) {
				monaco.languages.registerSignatureHelpProvider(id, {
					provideSignatureHelp: getSignatureHelp,
					signatureHelpTriggerCharacters: language.signatureHelpTriggers,
				});
			}
		}

		languageOptions[id] = language;
		return true;
	}
}

function findParameter(functionsArray, property, value) {
	for (const func of functionsArray) {
		const match = func.parameters.find(param => param[property] === value || param[property] === '[' + value + ']');
		if (match) {
			return match;
		}
	}
	return null; // No match found
}

function findParameterWithIndex(functionsArray, property, value) {
	for (let i = 0; i < functionsArray.length; i++) {
		const func = functionsArray[i];
		const index = func.parameters.findIndex(param => param[property] === value || param[property] === '[' + value + ']');
		if (index !== -1) {  // If a matching parameter is found
			return { parameter: func.parameters[index], index: index };
		}
	}
	return null; // No match found
}

function getActiveParameter(model, position, language, signatures) {
	const textUntilPosition = model.getValueInRange({
		startLineNumber: position.lineNumber,
		startColumn: 1,
		endLineNumber: position.lineNumber,
		endColumn: position.column
	});

	// optional parameter? value= or value:
	var optionalPostfix = languageOptions[language].optionalParameterPostfix;
	if (optionalPostfix && signatures.length > 0) {
		var paramName = getLastWordBeforeCharSinceComma(textUntilPosition, optionalPostfix);
		if (paramName) {
			var result = findParameterWithIndex(signatures, 'label', paramName);
			if (result) {
				return result.index;
			}
		}
	}

	// basic counting of commas since opening delimiter
	var delimiter = languageOptions[language].functionDelimiter;
	const openParenthesisIndex = textUntilPosition.lastIndexOf(delimiter);
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

async function getCompletions(model, position) {

	var textUntilPosition = model.getValueInRange({
		startLineNumber: 1,
		startColumn: 1,
		endLineNumber: position.lineNumber,
		endColumn: position.column,
	});

	var language = model.getLanguageId();
	var functionName = getLastFunctionName(textUntilPosition, language);

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
		items = await _objRef.invokeMethodAsync("GetCompletions", range, functionName);
	}

	// return result
	return { suggestions: items };
}

function getFirstWordBeforeChar(text, char) {
	const regex = new RegExp(`\\b(\\w+)\\s*\\${char}`);
	const match = text.match(regex);
	return match ? match[1] : null;
}

function getLastFunctionName(text, language) {
	// regular expression to match function names followed by an opening delimiter
	var delimiter = languageOptions[language].functionDelimiter;
	var regexString = (needsEscaping(delimiter)) ? "([a-zA-Z_$][0-9a-zA-Z_\\.$]*)\\s*\\" + delimiter : "([a-zA-Z_$][0-9a-zA-Z_\\.$]*)\\s*" + delimiter;
	const functionRegex = new RegExp(regexString, 'g');
	let match;
	let lastFunctionName = null;
	// iterate over all matches and store the last one
	while ((match = functionRegex.exec(text)) !== null) {
		lastFunctionName = match[1];
	}
	return lastFunctionName;
}

function getLastWordBeforeChar(text, char) {
	const regex = new RegExp(`(\\b\\w+)\\s*(?=${char})`, 'g');
	const matches = text.match(regex);
	return matches ? matches[matches.length - 1] : null;
}

function getLastWordBeforeCharSinceComma(text, postfix) {
	// Find the index of the last comma in the string
	const lastCommaIndex = text.lastIndexOf(',');

	// Find the index of the last postfix character in the string
	const lastPostfixIndex = text.lastIndexOf(postfix);

	// If no postfix or no comma is found, return null
	if (lastPostfixIndex === -1) {
		return null; // No postfix character found
	}

	// Slice the string from the last comma (or start of string if no comma)
	const relevantText = lastCommaIndex !== -1
		? text.slice(lastCommaIndex + 1, lastPostfixIndex)
		: text.slice(0, lastPostfixIndex);

	// Find the last word in the relevant portion of the string
	const regex = /(\b\w+)\s*$/;
	const match = relevantText.match(regex);

	// Return the last word if found, otherwise return null
	return match ? match[1] : null;
}

async function getSignatureHelp(model, position, token, context) {

	var language = model.getLanguageId();

	// determine current function
	var textUntilPosition = model.getValueInRange({
		startLineNumber: 1,
		startColumn: 1,
		endLineNumber: position.lineNumber,
		endColumn: position.column,
	});
	var functionName = getLastFunctionName(textUntilPosition, language);

	// call out to C# to fetch signatures
	var signatures = [];
	if (_objRef) {
		signatures = await _objRef.invokeMethodAsync("GetSignatures", functionName);
	}

	if (signatures.length > 0) {

		// determine active parameter
		var activeParameter = getActiveParameter(model, position, language, signatures);

		// determine active signature
		var activeSignature = getActiveSignature(signatures, activeParameter);
	}

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

function needsEscaping(char) {
	const specialChars = /[.*+?^${}()|[\]\\]/;
	return specialChars.test(char);
}

async function resolveCompletionItem(item, token) {
	if (_objRef) {
		await _objRef.invokeMethodAsync("ResolveCompletionAsync", item.label);
	}
}

function stripNonWordChars(str) {
	return str.replace(/^\W+|\W+$/g, '');
}