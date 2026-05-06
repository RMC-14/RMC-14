using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// ReSharper disable CheckNamespace
namespace Content.Shared.Actions.Components;

public sealed partial class ActionsComponent : IComponentDebug
{
    public string GetDebugString()
    {
        return $"""
            Actions Count: {Actions.Count}
            """;
    }
}
