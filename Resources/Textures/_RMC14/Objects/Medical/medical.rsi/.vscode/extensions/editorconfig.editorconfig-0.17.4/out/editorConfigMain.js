"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.activate = activate;
const vscode_1 = require("vscode");
const api_1 = require("./api");
const generateEditorConfig_1 = require("./commands/generateEditorConfig");
const DocumentWatcher_1 = require("./DocumentWatcher");
const EditorConfigCompletionProvider_1 = require("./EditorConfigCompletionProvider");
/**
 * Main entry
 */
function activate(ctx) {
    ctx.subscriptions.push(new DocumentWatcher_1.default());
    // register .editorconfig file completion provider
    const editorConfigFileSelector = {
        language: 'editorconfig',
        pattern: '**/.editorconfig',
        scheme: 'file',
    };
    vscode_1.languages.registerCompletionItemProvider(editorConfigFileSelector, new EditorConfigCompletionProvider_1.default());
    // register an internal command used to automatically display IntelliSense
    // when editing a .editorconfig file
    vscode_1.commands.registerCommand('editorconfig._triggerSuggestAfterDelay', () => {
        setTimeout(() => {
            vscode_1.commands.executeCommand('editor.action.triggerSuggest');
        }, 100);
    });
    // register a command handler to generate a .editorconfig file
    vscode_1.commands.registerCommand('EditorConfig.generate', generateEditorConfig_1.generateEditorConfig);
    return {
        applyTextEditorOptions: api_1.applyTextEditorOptions,
        fromEditorConfig: api_1.fromEditorConfig,
        resolveCoreConfig: api_1.resolveCoreConfig,
        resolveTextEditorOptions: api_1.resolveTextEditorOptions,
        toEditorConfig: api_1.toEditorConfig,
    };
}
//# sourceMappingURL=editorConfigMain.js.map