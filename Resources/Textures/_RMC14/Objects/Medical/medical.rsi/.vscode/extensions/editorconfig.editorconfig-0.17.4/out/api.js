"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.resolveTextEditorOptions = resolveTextEditorOptions;
exports.applyTextEditorOptions = applyTextEditorOptions;
exports.pickWorkspaceDefaults = pickWorkspaceDefaults;
exports.resolveCoreConfig = resolveCoreConfig;
exports.resolveFile = resolveFile;
exports.fromEditorConfig = fromEditorConfig;
exports.toEditorConfig = toEditorConfig;
const editorconfig = require("editorconfig");
const vscode_1 = require("vscode");
/**
 * Resolves `TextEditorOptions` for a `TextDocument`, combining the editor's
 * default configuration with that of EditorConfig's configuration.
 */
async function resolveTextEditorOptions(doc, { onBeforeResolve, onEmptyConfig, } = {}) {
    const coreConfig = await resolveCoreConfig(doc, { onBeforeResolve });
    if (coreConfig) {
        const defaults = pickWorkspaceDefaults(doc);
        const { activeTextEditor: editor } = vscode_1.window;
        const current = (editor === null || editor === void 0 ? void 0 : editor.document) === doc ? editor.options : undefined;
        return fromEditorConfig(coreConfig, defaults, current);
    }
    if (onEmptyConfig) {
        const { relativePath } = resolveFile(doc);
        if (relativePath) {
            onEmptyConfig(relativePath);
        }
    }
    return {};
}
/**
 * Applies new `TextEditorOptions` to the active text editor.
 */
async function applyTextEditorOptions(newOptions, { onNoActiveTextEditor, onSuccess, } = {}) {
    const editor = vscode_1.window.activeTextEditor;
    if (!editor) {
        if (onNoActiveTextEditor) {
            onNoActiveTextEditor();
        }
        return;
    }
    editor.options = newOptions;
    if (onSuccess) {
        onSuccess(newOptions);
    }
}
/**
 * Picks EditorConfig-relevant props from the editor's default configuration.
 */
function pickWorkspaceDefaults(doc) {
    const workspaceConfig = vscode_1.workspace.getConfiguration('editor', doc);
    const detectIndentation = workspaceConfig.get('detectIndentation');
    return detectIndentation
        ? {}
        : {
            tabSize: workspaceConfig.get('tabSize'),
            indentSize: workspaceConfig.get('indentSize'),
            insertSpaces: workspaceConfig.get('insertSpaces'),
        };
}
/**
 * Resolves an EditorConfig configuration for the file related to a
 * `TextDocument`.
 */
async function resolveCoreConfig(doc, { onBeforeResolve, } = {}) {
    const { fileName, relativePath } = resolveFile(doc);
    if (!fileName) {
        return {};
    }
    if (relativePath) {
        onBeforeResolve === null || onBeforeResolve === void 0 ? void 0 : onBeforeResolve(relativePath);
    }
    const config = await editorconfig.parse(fileName);
    return config;
}
function resolveFile(doc) {
    if (doc.languageId === 'Log') {
        return {};
    }
    const file = getFile();
    return {
        fileName: file === null || file === void 0 ? void 0 : file.fsPath,
        relativePath: file && vscode_1.workspace.asRelativePath(file, true),
    };
    function getFile() {
        var _a;
        if (!doc.isUntitled) {
            return doc.uri;
        }
        if ((_a = vscode_1.workspace.workspaceFolders) === null || _a === void 0 ? void 0 : _a[0]) {
            return vscode_1.Uri.joinPath(vscode_1.workspace.workspaceFolders[0].uri, doc.fileName);
        }
        return undefined;
    }
}
/**
 * Convert .editorconfig values to vscode editor options
 */
function fromEditorConfig(config = {}, defaults = pickWorkspaceDefaults(), current) {
    const resolved = {};
    if (Number.isInteger(config.indent_size)) {
        resolved.indentSize = config.indent_size;
    }
    else if (config.indent_size === 'tab') {
        resolved.indentSize = 'tabSize';
    }
    if (Number.isInteger(config.tab_width)) {
        resolved.tabSize = config.tab_width;
    }
    else if (Number.isInteger(config.indent_size)) {
        resolved.tabSize = config.indent_size;
    }
    if (config.indent_style === 'tab') {
        resolved.insertSpaces = false;
    }
    else if (config.indent_style === 'space') {
        resolved.insertSpaces = true;
    }
    const combined = { ...current, ...defaults, ...resolved };
    // decouple tabSize from indentSize when possible
    if (!Number.isInteger(config.tab_width) &&
        !(combined.insertSpaces && combined.indentSize === 'tabSize') &&
        !(config.indent_style === 'tab' && Number.isInteger(config.indent_size)) &&
        Number.isInteger(defaults.tabSize)) {
        combined.tabSize = defaults.tabSize;
    }
    return combined;
}
/**
 * Convert vscode editor options to .editorconfig values
 */
function toEditorConfig(options) {
    const result = {};
    switch (options.insertSpaces) {
        case true:
            result.indent_style = 'space';
            if (options.tabSize) {
                result.indent_size = resolveTabSize(options.tabSize);
            }
            break;
        case false:
        case 'auto':
            result.indent_style = 'tab';
            if (options.tabSize) {
                result.tab_width = resolveTabSize(options.tabSize);
            }
            break;
    }
    return result;
    /**
     * Convert vscode tabSize option into numeric value
     */
    function resolveTabSize(tabSize) {
        return tabSize === 'auto' ? 4 : parseInt(String(tabSize), 10);
    }
}
//# sourceMappingURL=api.js.map