'use strict';
// Imports /////////////////////////////////////////////////////////////////////
var path = require("path");
var net = require("net");
var vscode = require("vscode");
// Functions ///////////////////////////////////////////////////////////////////
function send(realm, client) {
    // Parameter Defaults //
    if (client == null)
        client = "";
    // Common //
    var config = vscode.workspace.getConfiguration("gmod-luadev");
    var document = vscode.window.activeTextEditor.document;
    // Document Title //
    var document_title = config.get("hidescriptname", false)
        ? "_"
        : path.basename(document.uri.fsPath);
    // Open Socket //
    var socket = new net.Socket();
    socket.connect(config.get("port", 27099));
    socket.write(realm + "\n" +
        document_title + "\n" +
        client + "\n" +
        document.getText());
    socket.on("error", function (ex) {
        if (ex.code == "ECONNREFUSED")
            vscode.window.showErrorMessage("Could not connect to LuaDev!");
        else
            vscode.window.showErrorMessage(ex.message);
    });
    socket.end();
}
function getPlayerList() {
    var config = vscode.workspace.getConfiguration("gmod-luadev");
    var socket = new net.Socket();
    socket.connect(config.get("port", 27099));
    socket.write("requestPlayers\n");
    socket.setEncoding("utf8");
    socket.on("data", function (data) {
        var clients = data.split("\n");
        clients.sort();
        vscode.window.showQuickPick(clients, {
            placeHolder: "Select Client to send to"
        }).then(function (client) {
            // Dialogue cancelled
            if (client == null)
                return;
            // Send to client
            send("client", client);
        });
    });
}
// Exports /////////////////////////////////////////////////////////////////////
function activate(context) {
    var command = vscode.commands.registerCommand;
    context.subscriptions.push(command("gmod-luadev.server", function () { return send("sv"); }), command("gmod-luadev.shared", function () { return send("sh"); }), command("gmod-luadev.clients", function () { return send("cl"); }), command("gmod-luadev.self", function () { return send("self"); }), command("gmod-luadev.client", getPlayerList));
}
exports.activate = activate;
//# sourceMappingURL=extension.js.map