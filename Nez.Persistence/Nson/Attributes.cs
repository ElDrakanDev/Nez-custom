using System;

namespace Nez.Persistence
{
	/// <summary>
	/// Mark members that should be included. Public fields and properties are included by default.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public sealed class NsonIncludeAttribute : Attribute { }

	/// <summary>
	/// Mark members that shouldn't be included. Private fields and properties are excluded by default.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public sealed class NsonExcludeAttribute : Attribute { }

}