using ImGuiNET;
using Microsoft.Xna.Framework;
using Nez.ImGuiTools.ObjectInspectors;
using System;
using System.Linq;

namespace Nez.ImGuiTools.TypeInspectors
{
	public class BitmaskInspector : AbstractTypeInspector
	{
		public struct BitmaskSelectable
		{
			public string Name;
			public int FlagValue;

			public BitmaskSelectable(string name, int flagValue)
			{
				Name = name;
				FlagValue = flagValue;
			}
		}

		int _allSelectedValue;
		BitmaskSelectable[] _selectOptions;

		public static bool IsValidTypeForInspector(Type t)
		{
			return t == typeof(int) && t.GetAttribute<BitmaskInspectableAttribute>() != null;
		}

		public override void Initialize()
		{
			base.Initialize();
			Type enumType = _memberInfo.GetAttribute<BitmaskInspectableAttribute>().FlagType;
			var names = Enum.GetNames(enumType);

			_selectOptions = names.Select(name => new BitmaskSelectable(name, (int)Enum.Parse(enumType, name))).ToArray();
			_allSelectedValue = _selectOptions.Sum(x => x.FlagValue);
		}

		string GetPreview()
		{
			var bitmask = GetValue<int>();
			string preview;

			if (bitmask == 0) preview = "None";
			else if (bitmask == _allSelectedValue) preview = "All";
			else if (_selectOptions.Any(x => x.FlagValue == bitmask))
				preview = _selectOptions.First(x => x.FlagValue == bitmask).Name;
			else preview = "Mixed";

			return preview;
		}

		public override void DrawMutable()
		{
			var bitmask = GetValue<int>();

			if(ImGui.BeginCombo(_name, GetPreview()))
			{
				foreach(var option in _selectOptions)
				{
					bool isSet = Flags.IsFlagSet(bitmask, option.FlagValue);

					if(isSet)
						ImGui.PushStyleColor(ImGuiCol.Text, Color.Yellow.PackedValue);

					if (ImGui.Selectable(option.Name, false, ImGuiSelectableFlags.DontClosePopups))
					{
						bitmask ^= option.FlagValue;
						SetValue(bitmask);
					}

					if(isSet)
						ImGui.PopStyleColor();
				}
				ImGui.EndCombo();
			}
		}
	}
}
