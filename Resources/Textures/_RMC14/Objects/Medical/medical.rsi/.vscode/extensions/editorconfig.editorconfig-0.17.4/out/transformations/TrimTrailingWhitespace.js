"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.TrimTrailingWhitespace = void 0;
const vscode_1 = require("vscode");
const PreSaveTransformation_1 = require("./PreSaveTransformation");
class TrimTrailingWhitespace extends PreSaveTransformation_1.PreSaveTransformation {
    transform(editorconfigProperties, doc, reason) {
        const editorTrimsWhitespace = vscode_1.workspace
            .getConfiguration('files', doc.uri)
            .get('trimTrailingWhitespace', false);
        if (editorTrimsWhitespace) {
            if (editorconfigProperties.trim_trailing_whitespace === false) {
                const message = [
                    'The trimTrailingWhitespace workspace or user setting',
                    'is overriding the EditorConfig setting for this file.',
                ].join(' ');
                return {
                    edits: new Error(message),
                    message,
                };
            }
        }
        if (shouldIgnoreSetting(editorconfigProperties.trim_trailing_whitespace)) {
            return { edits: [] };
        }
        if (vscode_1.window.activeTextEditor && vscode_1.window.activeTextEditor.document === doc) {
            const trimReason = reason !== vscode_1.TextDocumentSaveReason.Manual ? 'auto-save' : null;
            vscode_1.commands.executeCommand('editor.action.trimTrailingWhitespace', {
                reason: trimReason,
            });
            return {
                edits: [],
                message: 'editor.action.trimTrailingWhitespace',
            };
        }
        const edits = [];
        for (let i = 0; i < doc.lineCount; i++) {
            const edit = this.trimLineTrailingWhitespace(doc.lineAt(i));
            if (edit) {
                edits.push(edit);
            }
        }
        return {
            edits,
            message: 'trimTrailingWhitespace()',
        };
        function shouldIgnoreSetting(value) {
            return !value || value === 'unset';
        }
    }
    trimLineTrailingWhitespace(line) {
        const trimmedLine = this.trimTrailingWhitespace(line.text);
        if (trimmedLine === line.text) {
            return;
        }
        const whitespaceBegin = new vscode_1.Position(line.lineNumber, trimmedLine.length);
        const whitespaceEnd = new vscode_1.Position(line.lineNumber, line.text.length);
        const whitespace = new vscode_1.Range(whitespaceBegin, whitespaceEnd);
        return vscode_1.TextEdit.delete(whitespace);
    }
    trimTrailingWhitespace(input) {
        return input.replace(/[\s\uFEFF\xA0]+$/g, '');
    }
}
exports.TrimTrailingWhitespace = TrimTrailingWhitespace;
//# sourceMappingURL=TrimTrailingWhitespace.js.map