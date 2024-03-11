using System;

namespace Nez.ImGuiTools.ObjectInspectors
{
	/// <summary>
	/// Adding this method will allow you to add inspector drawing on entity lists
	/// </summary>
	[AttributeUsage(AttributeTargets.Method)]
	public class InspectorEntityListExtension : Attribute
	{
	}
}
