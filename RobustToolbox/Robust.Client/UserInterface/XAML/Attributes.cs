﻿using System;

namespace Avalonia.Metadata
{
    public sealed class ContentAttribute : Attribute
    {
    }
}

namespace Robust.Client.UserInterface.XAML
{
    public sealed class UsableDuringInitializationAttribute : Attribute
    {
        public UsableDuringInitializationAttribute(bool usable)
        {
        }
    }

    public sealed class DeferredContentAttribute : Attribute
    {
    }

    public interface ITestRootObjectProvider
    {
        object RootObject { get; }
    }

    public interface ITestProvideValueTarget
    {
        object TargetObject { get; }
        object TargetProperty { get; }
    }

    public interface ITestUriContext
    {
        Uri BaseUri { get; set; }
    }
}
