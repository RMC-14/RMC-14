"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.generateEditorConfig = generateEditorConfig;
const fs_1 = require("fs");
const os_1 = require("os");
const path_1 = require("path");
const util_1 = require("util");
const vscode_1 = require("vscode");
const readFile = (0, util_1.promisify)(fs_1.readFile);
/**
 * Generate a .editorconfig file in the root of the workspace based on the
 * current vscode settings.
 */
async function generateEditorConfig(uri) {
    const workspaceUri = vscode_1.workspace.workspaceFolders && vscode_1.workspace.workspaceFolders[0].uri;
    const currentUri = uri || workspaceUri;
    if (!currentUri) {
        vscode_1.window.showErrorMessage("Workspace doesn't contain any folders.");
        return;
    }
    const editorConfigUri = vscode_1.Uri.parse(`${currentUri.toString()}/.editorconfig`);
    try {
        const stats = await vscode_1.workspace.fs.stat(editorConfigUri);
        if (stats.type === vscode_1.FileType.File) {
            vscode_1.window.showErrorMessage('An .editorconfig file already exists in this workspace.');
            return;
        }
    }
    catch (err) {
        if (typeof err === 'object' &&
            err !== null &&
            'name' in err &&
            'message' in err &&
            typeof err.message === 'string') {
            if (err.name === 'EntryNotFound (FileSystemError)') {
                writeFile();
            }
            else {
                vscode_1.window.showErrorMessage(err.message);
            }
            return;
        }
    }
    async function writeFile() {
        const ec = vscode_1.workspace.getConfiguration('editorconfig');
        const generateAuto = !!ec.get('generateAuto');
        if (!generateAuto) {
            const template = ec.get('template') || 'default';
            const defaultTemplatePath = (0, path_1.resolve)(__dirname, '..', 'DefaultTemplate.editorconfig');
            let templateBuffer;
            try {
                templateBuffer = await readFile(/^default$/i.test(template) ? defaultTemplatePath : template);
            }
            catch (error) {
                if (typeof error !== 'object' ||
                    error === null ||
                    !('message' in error) ||
                    typeof error.message !== 'string') {
                    return;
                }
                vscode_1.window.showErrorMessage([
                    `Could not read EditorConfig template file at ${template}`,
                    error.message,
                ].join(os_1.EOL));
                return;
            }
            try {
                vscode_1.workspace.fs.writeFile(editorConfigUri, templateBuffer);
            }
            catch (error) {
                if (typeof error !== 'object' ||
                    error === null ||
                    !('message' in error) ||
                    typeof error.message !== 'string') {
                    return;
                }
                vscode_1.window.showErrorMessage(error.message);
            }
            return;
        }
        const editor = vscode_1.workspace.getConfiguration('editor', currentUri);
        const files = vscode_1.workspace.getConfiguration('files', currentUri);
        const settingsLines = [
            '# EditorConfig is awesome: https://EditorConfig.org',
            '',
            '# top-most EditorConfig file',
            'root = true',
            '',
            '[*]',
        ];
        function addSetting(key, value) {
            if (value !== undefined) {
                settingsLines.push(`${key} = ${value}`);
            }
        }
        const insertSpaces = !!editor.get('insertSpaces');
        addSetting('indent_style', insertSpaces ? 'space' : 'tab');
        addSetting('indent_size', editor.get('tabSize'));
        const eolMap = {
            '\r\n': 'crlf',
            '\n': 'lf',
        };
        let eolKey = files.get('eol') || 'auto';
        if (eolKey === 'auto') {
            eolKey = os_1.EOL;
        }
        addSetting('end_of_line', eolMap[eolKey]);
        const encodingMap = {
            iso88591: 'latin1',
            utf8: 'utf-8',
            utf8bom: 'utf-8-bom',
            utf16be: 'utf-16-be',
            utf16le: 'utf-16-le',
        };
        addSetting('charset', encodingMap[files.get('encoding')]);
        addSetting('trim_trailing_whitespace', !!files.get('trimTrailingWhitespace'));
        const insertFinalNewline = !!files.get('insertFinalNewline');
        addSetting('insert_final_newline', insertFinalNewline);
        if (insertFinalNewline) {
            settingsLines.push('');
        }
        try {
            await vscode_1.workspace.fs.writeFile(editorConfigUri, Buffer.from(settingsLines.join(eolKey)));
        }
        catch (err) {
            if (typeof err !== 'object' ||
                err === null ||
                !('message' in err) ||
                typeof err.message !== 'string') {
                return;
            }
            vscode_1.window.showErrorMessage(err.message);
        }
    }
}
//# sourceMappingURL=generateEditorConfig.js.map