using System;

namespace Nez.ImGuiTools.ObjectInspectors
{
	/// <summary>
	/// Adding this to an int field will show it in the inspector as a bitmask
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class BitmaskInspectableAttribute : Attribute
	{
		public Type FlagType;
		public BitmaskInspectableAttribute(Type flagType)
		{
			Insist.IsTrue(flagType.IsEnum && flagType.GetAttribute<FlagsAttribute>() != null);
			FlagType = flagType;
		}
	}
}
