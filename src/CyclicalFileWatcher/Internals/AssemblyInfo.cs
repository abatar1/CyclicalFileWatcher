﻿using System.ComponentModel;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("CyclicalFileWatcher.UnitTests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")] 

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable CheckNamespace
namespace System.Runtime.CompilerServices;

#if !NET5_0_OR_GREATER

[EditorBrowsable(EditorBrowsableState.Never)]
internal static class IsExternalInit;

#endif // !NET5_0_OR_GREATER

#if !NET7_0_OR_GREATER

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
internal sealed class RequiredMemberAttribute : Attribute;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
internal sealed class CompilerFeatureRequiredAttribute(string featureName) : Attribute
{
    public string FeatureName { get; } = featureName;
    public bool IsOptional  { get; init; }

    public const string RefStructs = nameof(RefStructs);
    public const string RequiredMembers = nameof(RequiredMembers);
}

#endif // !NET7_0_OR_GREATER

#if !NET7_0_OR_GREATER
[AttributeUsage(AttributeTargets.Constructor)]
internal sealed class SetsRequiredMembersAttribute : Attribute;
#endif

