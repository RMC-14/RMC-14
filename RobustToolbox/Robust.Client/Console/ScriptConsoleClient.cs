#if CLIENT_SCRIPTING
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting.Hosting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Text;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.ViewVariables;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;
using Robust.Shared.Scripting;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
using Color = Robust.Shared.Maths.Color;

#nullable enable

namespace Robust.Client.Console
{
    internal sealed class ScriptConsoleClient : ScriptConsole
    {
#pragma warning disable 649
        [Dependency] private readonly IReflectionManager _reflectionManager = default!;
        [Dependency] private readonly IDependencyCollection _dependency = default!;
#pragma warning restore 649

        private readonly StringBuilder _inputBuffer = new();
        private int _linesEntered;

        // Necessary for syntax highlighting.
        private readonly Workspace _highlightWorkspace = new AdhocWorkspace();

        private readonly ScriptGlobals _globals;
        private ScriptState? _state;

        private (string[] imports, string code)? _autoImportRepeatBuffer;

        public ScriptConsoleClient()
        {
            Title = "Robust C# Interactive (CLIENT)";
            ScriptInstanceShared.InitDummy();
            IoCManager.InjectDependencies(this);

            _globals = new ScriptGlobalsImpl(this);

            OutputPanel.AddText("Robust C# interactive console (CLIENT).");
            OutputPanel.AddText(">");
        }

        // No-op for now.
        protected override void Complete() { }

        protected override async void Run()
        {
            var code = InputBar.Text;
            InputBar.Clear();

            if (_autoImportRepeatBuffer.HasValue && code == "y")
            {
                var (imports, repeatCode) = _autoImportRepeatBuffer.Value;
                var sb = new StringBuilder();
                foreach (var import in imports)
                {
                    sb.AppendFormat("using {0};\n", import);
                }

                sb.Append(repeatCode);

                code = sb.ToString();
            }
            else
            {
                // Remove > or . at the end of the output panel.
                OutputPanel.RemoveEntry(^1);

                _inputBuffer.AppendLine(code);
                _linesEntered += 1;

                var tree = SyntaxFactory.ParseSyntaxTree(SourceText.From(_inputBuffer.ToString()),
                    ScriptInstanceShared.ParseOptions);

                if (!SyntaxFactory.IsCompleteSubmission(tree))
                {
                    if (_linesEntered == 1)
                    {
                        OutputPanel.AddText($"> {code}");
                    }
                    else
                    {
                        OutputPanel.AddText($". {code}");
                    }

                    OutputPanel.AddText(".");
                    return;
                }

                code = _inputBuffer.ToString().Trim();

                // Remove echo of partial submission from the output panel.
                for (var i = 1; i < _linesEntered; i++)
                {
                    OutputPanel.RemoveEntry(^1);
                }

                _inputBuffer.Clear();
                _linesEntered = 0;
            }

            Script newScript;

            if (_state != null)
            {
                newScript = _state.Script.ContinueWith(code);
            }
            else
            {
                var options = ScriptInstanceShared.GetScriptOptions(_reflectionManager).AddReferences(typeof(Image).Assembly);
                newScript = CSharpScript.Create(code, options, typeof(ScriptGlobals));
            }

            // Compile ahead of time so that we can do syntax highlighting correctly for the echo.
            newScript.Compile();

            // Echo entered script.
            var echoMessage = new FormattedMessage();
            echoMessage.PushColor(Color.FromHex("#D4D4D4"));
            echoMessage.AddText("> ");
            ScriptInstanceShared.AddWithSyntaxHighlighting(newScript, echoMessage, code, _highlightWorkspace);

            OutputPanel.AddMessage(echoMessage);

            try
            {
                if (_state != null)
                {
                    _state = await newScript.RunFromAsync(_state, _ => true);
                }
                else
                {
                    _state = await newScript.RunAsync(_globals, _ => true);
                }
            }
            catch (CompilationErrorException e)
            {
                var msg = new FormattedMessage();

                msg.PushColor(Color.Crimson);

                foreach (var diagnostic in e.Diagnostics)
                {
                    msg.AddText(diagnostic.ToString());
                    msg.AddText("\n");
                }

                OutputPanel.AddMessage(msg);
                OutputPanel.AddText(">");

                PromptAutoImports(e.Diagnostics, code);
                return;
            }

            if (_state.Exception != null)
            {
                var msg = new FormattedMessage();
                msg.PushColor(Color.Crimson);
                msg.AddText(CSharpObjectFormatter.Instance.FormatException(_state.Exception));
                OutputPanel.AddMessage(msg);
            }
            else if (ScriptInstanceShared.HasReturnValue(newScript))
            {
                var msg = new FormattedMessage();
                msg.AddText(ScriptInstanceShared.SafeFormat(_state.ReturnValue));
                OutputPanel.AddMessage(msg);
            }

            OutputPanel.AddText(">");
        }

        private void PromptAutoImports(IEnumerable<Diagnostic> diags, string code)
        {
            if (!ScriptInstanceShared.CalcAutoImports(_reflectionManager, diags, out var found))
                return;

            OutputPanel.AddText($"Auto-import {string.Join(", ", found)} (enter 'y')?");

            _autoImportRepeatBuffer = (found.ToArray(), code);
        }

        private sealed class ScriptGlobalsImpl : ScriptGlobals
        {
            private readonly ScriptConsoleClient _owner;

            [field: Dependency] public override IClientViewVariablesManager vvm { get; } = default!;

            public ScriptGlobalsImpl(ScriptConsoleClient owner) : base(owner._dependency)
            {
                _owner = owner;
            }

            public override void vv(object a)
            {
                vvm.OpenVV(a);
            }

            protected override void WriteSyntax(object toString)
            {
                var code = toString.ToString();

                if (code == null)
                    return;

                var options = ScriptInstanceShared.GetScriptOptions(_owner._reflectionManager).AddReferences(typeof(Image).Assembly);
                var script = CSharpScript.Create(code, options, typeof(ScriptGlobals));
                script.Compile();

                var syntax = new FormattedMessage();
                ScriptInstanceShared.AddWithSyntaxHighlighting(script, syntax, code, _owner._highlightWorkspace);

                _owner.OutputPanel.AddMessage(syntax);
            }

            public override void write(object toString)
            {
                _owner.OutputPanel.AddText(toString?.ToString() ?? "");
            }

            public override void show(object obj)
            {
                write(ScriptInstanceShared.SafeFormat(obj));
            }
        }
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [PublicAPI]
    public abstract class ScriptGlobals : ScriptGlobalsShared
    {
        public abstract IClientViewVariablesManager vvm { get; }

        public abstract void vv(object a);

        protected ScriptGlobals(IDependencyCollection dependencies) : base(dependencies)
        {
        }
    }
}
#endif
