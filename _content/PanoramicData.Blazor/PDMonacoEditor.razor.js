var _objRef = null;
var _disabledKeys = new Set();
var _keyEventListeners = new Map(); // Track event listeners for cleanup
var languageOptions = {};

// Characters that may start a (dotted) function name: ASCII letters, "_" and "$"
function isIdentifierStart(ch) {
	return (
		(ch >= "a" && ch <= "z") ||
		(ch >= "A" && ch <= "Z") ||
		ch === "_" ||
		ch === "$"
	);
}

// Characters that may continue a (dotted) function name: the above plus digits and "."
function isIdentifierPart(ch) {
	return isIdentifierStart(ch) || (ch >= "0" && ch <= "9") || ch === ".";
}

// Same set of characters as the regular expression class \s
function isWhitespace(ch) {
	return ch !== undefined && ch.length === 1 && ch.trim() === "";
}

// Monaco is loaded by the host page as a global script
function getMonaco() {
	return window.monaco;
}

export function initialize(objRef) {
	if (objRef) {
		_objRef = objRef;
	}
}

function toKeyId(binding) {
	return `${binding.keyCode}-${binding.ctrlKey}-${binding.altKey}-${binding.shiftKey}`;
}

function fromKeyId(keyId) {
	const [keyCode, ctrlKey, altKey, shiftKey] = keyId.split("-");
	return {
		keyCode: parseInt(keyCode, 10),
		ctrlKey: ctrlKey === "true",
		altKey: altKey === "true",
		shiftKey: shiftKey === "true",
	};
}

export function disableKeyBinding(keyCode, ctrlKey, altKey, shiftKey) {
	// Create a key identifier for the combination
	const binding = { keyCode, ctrlKey, altKey, shiftKey };
	const keyId = toKeyId(binding);
	_disabledKeys.add(keyId);

	// Wait for Monaco to be ready and then intercept the key events
	const monaco = getMonaco();
	if (monaco && monaco.editor) {
		// Apply to all existing editors
		monaco.editor.getEditors().forEach((editor) => {
			interceptKeyboardEvent(editor, binding, keyId);
		});

		// Also intercept for any new editors that might be created
		const originalCreate = monaco.editor.create;
		if (!originalCreate._pdIntercepted) {
			monaco.editor.create = function (container, options, override) {
				const editor = originalCreate.call(this, container, options, override);

				// Apply all currently disabled key combinations to new editor
				_disabledKeys.forEach((disabledKeyId) => {
					interceptKeyboardEvent(
						editor,
						fromKeyId(disabledKeyId),
						disabledKeyId,
					);
				});

				return editor;
			};
			originalCreate._pdIntercepted = true;
		}
	}
}

function findEditorInputElement(domNode) {
	return (
		domNode.querySelector(".monaco-editor textarea") ||
		domNode.querySelector(".monaco-editor input") ||
		domNode.querySelector(".view-lines")
	);
}

function createKeyEventHandler(binding, keyId) {
	return function (event) {
		const matches =
			event.keyCode === binding.keyCode &&
			event.ctrlKey === binding.ctrlKey &&
			event.altKey === binding.altKey &&
			event.shiftKey === binding.shiftKey;

		if (matches) {
			console.log(`Intercepted disabled key combination: ${keyId}`);
			event.preventDefault();
			event.stopPropagation();
			event.stopImmediatePropagation();
		}
	};
}

function interceptKeyboardEvent(editor, binding, keyId) {
	if (!editor) return;

	try {
		const domNode = editor.getDomNode();
		if (!domNode) return;

		// Find the actual editor textarea/input element
		const editorElement = findEditorInputElement(domNode);
		if (!editorElement) {
			console.warn("Could not find Monaco editor input element");
			return;
		}

		// Add the event listener with capture=true to intercept before Monaco
		const keyEventHandler = createKeyEventHandler(binding, keyId);
		editorElement.addEventListener("keydown", keyEventHandler, true);

		// Store the listener for cleanup
		if (!_keyEventListeners.has(keyId)) {
			_keyEventListeners.set(keyId, new Map());
		}
		_keyEventListeners
			.get(keyId)
			.set(editor, { element: editorElement, handler: keyEventHandler });

		console.log(`Intercepted keyboard events for key binding: ${keyId}`);
	} catch (error) {
		console.warn("Failed to intercept Monaco keyboard events:", error);
	}
}

export function enableKeyBinding(keyCode, ctrlKey, altKey, shiftKey) {
	const keyId = toKeyId({ keyCode, ctrlKey, altKey, shiftKey });

	// Remove from disabled keys
	_disabledKeys.delete(keyId);

	// Remove all event listeners for this key combination
	if (_keyEventListeners.has(keyId)) {
		_keyEventListeners.get(keyId).forEach(({ element, handler }) => {
			element.removeEventListener("keydown", handler, true);
		});
		_keyEventListeners.delete(keyId);
		console.log(`Re-enabled key binding: ${keyId}`);
	}
}

export function registerLanguage(id, language) {
	const monaco = getMonaco();
	if (!monaco) {
		return false;
	}

	// does language already exist?
	const exists = monaco.languages.getLanguages().some((obj) => obj.id === id);
	if (exists) {
		return false;
	}
	monaco.languages.register({ id: id });
	if (language.showCompletions) {
		monaco.languages.registerCompletionItemProvider(id, {
			provideCompletionItems: getCompletions,
			resolveCompletionItem: resolveCompletionItem,
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

function findParameterIndex(functionsArray, property, value) {
	for (const func of functionsArray) {
		const index = func.parameters.findIndex(
			(param) =>
				param[property] === value || param[property] === "[" + value + "]",
		);
		if (index !== -1) {
			return index;
		}
	}
	return -1; // No match found
}

function getActiveParameter(model, position, language, signatures) {
	const textUntilPosition = model.getValueInRange({
		startLineNumber: position.lineNumber,
		startColumn: 1,
		endLineNumber: position.lineNumber,
		endColumn: position.column,
	});

	// optional parameter? value= or value:
	var optionalPostfix = languageOptions[language].optionalParameterPostfix;
	if (optionalPostfix && signatures.length > 0) {
		var paramName = getLastWordBeforeCharSinceComma(
			textUntilPosition,
			optionalPostfix,
		);
		if (paramName) {
			const index = findParameterIndex(signatures, "label", paramName);
			if (index !== -1) {
				return index;
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
	return paramsString.split(",").length - 1;
}

function getActiveSignature(signatures, activeParameter) {
	// selects first signature with at least N parameters
	for (let i = 0; i < signatures.length; i++) {
		if (signatures[i].parameters.length >= activeParameter + 1) {
			return i;
		}
	}
	return 0; // unknown - return first
}

function getTextUntilPosition(model, position) {
	return model.getValueInRange({
		startLineNumber: 1,
		startColumn: 1,
		endLineNumber: position.lineNumber,
		endColumn: position.column,
	});
}

async function getCompletions(model, position) {
	var textUntilPosition = getTextUntilPosition(model, position);

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
		items = await _objRef.invokeMethodAsync(
			"GetCompletions",
			range,
			functionName,
		);
	}

	// return result
	return { suggestions: items };
}

// Index of the first character at or after index that does not satisfy the predicate
function skipWhile(text, index, predicate) {
	let end = index;
	while (end < text.length && predicate(text[end])) {
		end++;
	}
	return end;
}

// Index just after the delimiter that follows position nameEnd (optionally after whitespace), or -1
function findDelimiterEnd(text, nameEnd, delimiter) {
	// Prefer the most whitespace before the delimiter
	const whitespaceEnd = skipWhile(text, nameEnd, isWhitespace);
	for (let index = whitespaceEnd; index >= nameEnd; index--) {
		if (text[index] === delimiter) {
			return index + 1;
		}
	}
	return -1;
}

/**
 * Tries to match "identifier, optional whitespace, delimiter" starting at index start
 * (the identifier may contain dots). Returns { name, end } or null.
 */
function matchFunctionCallAt(text, start, delimiter) {
	if (!isIdentifierStart(text[start])) {
		return null;
	}

	// Prefer the longest identifier
	const identifierEnd = skipWhile(text, start + 1, isIdentifierPart);
	for (let nameEnd = identifierEnd; nameEnd > start; nameEnd--) {
		const end = findDelimiterEnd(text, nameEnd, delimiter);
		if (end !== -1) {
			return { name: text.slice(start, nameEnd), end };
		}
	}

	return null;
}

function getLastFunctionName(text, language) {
	// find the last function name followed by the language's opening delimiter
	const delimiter = languageOptions[language].functionDelimiter;
	let lastFunctionName = null;
	let index = 0;
	while (index < text.length) {
		const match = matchFunctionCallAt(text, index, delimiter);
		if (match) {
			lastFunctionName = match.name;
			index = match.end;
		} else {
			index++;
		}
	}
	return lastFunctionName;
}

function getLastWordBeforeCharSinceComma(text, postfix) {
	// Find the index of the last comma in the string
	const lastCommaIndex = text.lastIndexOf(",");

	// Find the index of the last postfix character in the string
	const lastPostfixIndex = text.lastIndexOf(postfix);

	// If no postfix or no comma is found, return null
	if (lastPostfixIndex === -1) {
		return null; // No postfix character found
	}

	// Slice the string from the last comma (or start of string if no comma)
	const relevantText =
		lastCommaIndex !== -1
			? text.slice(lastCommaIndex + 1, lastPostfixIndex)
			: text.slice(0, lastPostfixIndex);

	// Find the last word in the relevant portion of the string
	const regex = /(\b\w+)\s*$/;
	const match = relevantText.match(regex);

	// Return the last word if found, otherwise return null
	return match ? match[1] : null;
}

async function getSignatureHelp(model, position) {
	var language = model.getLanguageId();

	// determine current function
	var textUntilPosition = getTextUntilPosition(model, position);
	var functionName = getLastFunctionName(textUntilPosition, language);

	// call out to C# to fetch signatures
	var signatures = [];
	if (_objRef) {
		signatures = await _objRef.invokeMethodAsync("GetSignatures", functionName);
	}

	let activeParameter;
	let activeSignature;
	if (signatures.length > 0) {
		activeParameter = getActiveParameter(model, position, language, signatures);
		activeSignature = getActiveSignature(signatures, activeParameter);
	}

	// return result
	const value = { signatures, activeSignature, activeParameter };
	const result = { value, dispose: function () {} };
	return result;
}

async function resolveCompletionItem(item) {
	if (_objRef) {
		await _objRef.invokeMethodAsync("ResolveCompletionAsync", item.label);
	}
}
