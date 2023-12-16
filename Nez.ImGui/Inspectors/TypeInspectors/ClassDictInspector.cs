using ImGuiNET;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Nez.ImGuiTools.TypeInspectors
{
	public class ClassDictInspector : AbstractTypeInspector
	{
		IDictionary _dict;
		Type _dictKeyType, _dictValueType;
		Dictionary<string, AbstractTypeInspector> _valueInspectors = new Dictionary<string, AbstractTypeInspector>();

		List<object> _toRemove = new List<object>();
		IDictionary _modifiedValues;

		object _keyToCreate;
		AbstractTypeInspector _keyCreateInspector;

		public override void Initialize()
		{
			base.Initialize();

			_dict = (IDictionary)_getter(_target);
			var genericArgs = _valueType.GetGenericArguments();
			_dictKeyType = genericArgs[0];
			_dictValueType = genericArgs[1];

			var dictType = typeof(Dictionary<,>);
			var constructedListType = dictType.MakeGenericType(_dictKeyType, _dictValueType);
			_modifiedValues = (IDictionary)Activator.CreateInstance(constructedListType);

			// null check. we just create an instance if null
			if (_dict == null)
				_dict = (IDictionary)Activator.CreateInstance(constructedListType);
		}

		public override void DrawMutable()
		{
			foreach(var key in _dict.Keys)
			{
				if(_valueInspectors.ContainsKey(key.ToString()) is false)
				{
					var valueInspector = TypeInspectorUtils.GetInspectorForType(_dictValueType, _dict[key], null);
					valueInspector.SetTarget(_dict[key], _dict.GetType().GetMethod("get_Item"), _dictValueType, "Value");
					valueInspector.SetGetter((obj) => _dict[key], "Value");
					valueInspector.SetSetter(obj => _modifiedValues.Add(key, obj));
					valueInspector.Initialize();
					_valueInspectors.Add(key.ToString(), valueInspector);
				}
			}

			_dict = GetValue<IDictionary>();
			foreach (var obj in _toRemove)
			{
				_dict.Remove(obj);
				_valueInspectors.Remove(obj.ToString());
			}
			_toRemove.Clear();

			foreach(var key in _modifiedValues.Keys)
				_dict[key] = _modifiedValues[key];
			_modifiedValues.Clear();

			ImGui.Indent();
			if (ImGui.CollapsingHeader($"{_name} [{_dict.Count}]###{_name}", ImGuiTreeNodeFlags.FramePadding))
			{
				ImGui.Indent();

				if (ImGui.Button("Add Element"))
				{
					_keyToCreate = _dictKeyType == typeof(string) ? "key" : Activator.CreateInstance(_dictKeyType);
					_keyCreateInspector = TypeInspectorUtils.GetInspectorForType(_dictKeyType, _keyToCreate, null);
					_keyCreateInspector.SetTarget(_keyToCreate, _dict.GetType().GetMethod("get_Item"), _dictKeyType);
					_keyCreateInspector.SetGetter((obj) => _keyToCreate, "Key");
					_keyCreateInspector.SetSetter(obj => _keyToCreate = obj);
					_keyCreateInspector.Initialize();
					ImGui.OpenPopup("Add new key");
				}

				bool _ = true;
				if (ImGui.BeginPopupModal("Add new key", ref _, ImGuiWindowFlags.AlwaysAutoResize))
				{
					ImGui.TextWrapped("Add a new key");
					NezImGui.MediumVerticalSpace();
					ImGui.Separator();
					NezImGui.SmallVerticalSpace();

					_keyCreateInspector.Draw();

					if (ImGui.Button("Cancel", new System.Numerics.Vector2(120, 0)))
					{
						ImGui.CloseCurrentPopup();
					}

					ImGui.SetItemDefaultFocus();
					ImGui.SameLine();
					if (ImGui.Button("Confirm", new System.Numerics.Vector2(120, 0)) && !_dict.Contains(_keyToCreate))
					{
						var emptyVal = _dictValueType == typeof(string) ? "value" : Activator.CreateInstance(_dictValueType);
						_dict.Add(_keyToCreate, emptyVal);
						ImGui.CloseCurrentPopup();
					}

					ImGui.EndPopup();
				}

				ImGui.SameLine(ImGui.GetWindowWidth() - ImGui.GetItemRectSize().X -
								ImGui.GetStyle().ItemInnerSpacing.X);

				if (ImGui.Button("Clear"))
				{
					ImGui.OpenPopup("Clear Data");
				}

				if (NezImGui.SimpleDialog("Clear Data", "Are you sure you want to clear the data?"))
				{
					_dict.Clear();
					Debug.Log($"dict count: {_dict.Count}");
				}

				ImGui.PushItemWidth(-ImGui.GetStyle().IndentSpacing);
				foreach (var key in _valueInspectors.Keys)
				{
					if (ImGui.CollapsingHeader(key.ToString()))
					{
						ImGui.Indent();
						_valueInspectors[key.ToString()].Draw();

						if(ImGui.Button("Remove element"))
							_toRemove.Add(key);
						ImGui.Unindent();
					}
				}

				ImGui.PopItemWidth();
				ImGui.Unindent();
			}

			ImGui.Unindent();
		}

	}
}
