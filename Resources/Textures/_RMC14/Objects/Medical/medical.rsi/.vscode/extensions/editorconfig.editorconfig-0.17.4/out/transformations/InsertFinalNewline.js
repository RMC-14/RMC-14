"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.InsertFinalNewline = void 0;
const vscode_1 = require("vscode");
const PreSaveTransformation_1 = require("./PreSaveTransformation");
const lineEndings = {
    CR: '\r',
    CRLF: '\r\n',
    LF: '\n',
};
class InsertFinalNewline extends PreSaveTransformation_1.PreSaveTransformation {
    constructor() {
        super(...arguments);
        this.lineEndings = lineEndings;
    }
    transform(editorconfigProperties, doc) {
        var _a;
        const lineCount = doc.lineCount;
        const lastLine = doc.lineAt(lineCount - 1);
        if (shouldIgnoreSetting(editorconfigProperties.insert_final_newline) ||
            lineCount === 0 ||
            lastLine.isEmptyOrWhitespace) {
            return { edits: [] };
        }
        if (vscode_1.window.activeTextEditor && vscode_1.window.activeTextEditor.document === doc) {
            vscode_1.commands.executeCommand('editor.action.insertFinalNewLine');
            return {
                edits: [],
                message: 'editor.action.insertFinalNewLine',
            };
        }
        const position = new vscode_1.Position(lastLine.lineNumber, lastLine.text.length);
        const eol = ((_a = editorconfigProperties.end_of_line) !== null && _a !== void 0 ? _a : 'lf').toUpperCase();
        return {
            edits: [
                vscode_1.TextEdit.insert(position, this.lineEndings[eol]),
            ],
            message: `insertFinalNewline(${eol})`,
        };
        function shouldIgnoreSetting(value) {
            return !value || value === 'unset';
        }
    }
}
exports.InsertFinalNewline = InsertFinalNewline;
//# sourceMappingURL=InsertFinalNewline.js.map