using System;

namespace Nez.ImGuiTools.ObjectInspectors
{
	[AttributeUsage(AttributeTargets.Method)]
	public class InspectorEntityContextMenu : Attribute
	{
		public string Name;
		public bool IsPopup;
		public InspectorEntityContextMenu(string name, bool isPopup = false)
		{
			Name = name;
			IsPopup = isPopup;
		}
	}
}
