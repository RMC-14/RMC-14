"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.SetEndOfLine = void 0;
const vscode_1 = require("vscode");
const PreSaveTransformation_1 = require("./PreSaveTransformation");
const eolMap = {
    LF: vscode_1.EndOfLine.LF,
    CRLF: vscode_1.EndOfLine.CRLF,
};
/**
 * Sets the end of line, but only when there is a reason to do so.
 * This is to preserve redo history when possible.
 */
class SetEndOfLine extends PreSaveTransformation_1.PreSaveTransformation {
    constructor() {
        super(...arguments);
        this.eolMap = eolMap;
    }
    transform(editorconfigProperties, doc) {
        const eolKey = (editorconfigProperties.end_of_line || '').toUpperCase();
        const eol = this.eolMap[eolKey];
        /**
         * VSCode normalizes line endings on every file-save operation
         * according to whichever EOL sequence is dominant. If the file already
         * has the appropriate dominant EOL sequence, there is nothing more to do,
         * so we defer to VSCode's built-in functionality by applying no edits.
         */
        return !eol || doc.eol === eol
            ? { edits: [] }
            : {
                edits: [vscode_1.TextEdit.setEndOfLine(eol)],
                message: `setEndOfLine(${eolKey})`,
            };
    }
}
exports.SetEndOfLine = SetEndOfLine;
//# sourceMappingURL=SetEndOfLine.js.map