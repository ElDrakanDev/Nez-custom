using ImGuiNET;
using System;
using Microsoft.Xna.Framework;
using System.Collections;
using System.Collections.Generic;


namespace Nez.ImGuiTools.TypeInspectors
{
	public class SimpleDictInspector : AbstractTypeInspector
	{
		public static Type[] SupportedValueTypes =
		{
			typeof(bool), typeof(Color), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float),
			typeof(string), typeof(Vector2), typeof(Vector3)
		};

		Type _dictKeyType, _dictValueType;
		IDictionary _dict;
		RangeAttribute _rangeAttribute;
		Action<object> _inspectMethodAction;
		bool _isUnsignedInt;
		List<object> _toRemove = new List<object>();
		IDictionary _modifiedValues;

		AbstractTypeInspector _keyCreateInspector;
		object _keyToCreate;

		public override void Initialize()
		{
			base.Initialize();
			_dict = (IDictionary)_getter(_target);
			_rangeAttribute = _memberInfo.GetAttribute<RangeAttribute>();

			var genericArgs = _valueType.GetGenericArguments();
			_dictKeyType = genericArgs[0];
			_dictValueType = genericArgs[1];

			var valueTypeName = _dictValueType.Name.ToString();
			var dictType = typeof(Dictionary<,>);
			var constructedListType = dictType.MakeGenericType(_dictKeyType, _dictValueType);
			_modifiedValues = (IDictionary)Activator.CreateInstance(constructedListType);

			if (_dict == null)
			{
				_dict = (IDictionary)Activator.CreateInstance(constructedListType);
				_setter(_dict);
			}

			switch (valueTypeName[0].ToString().ToUpper() + valueTypeName.Substring(1))
			{
				case "String":
					_inspectMethodAction = InspectString; break;
				case "Boolean":
					_inspectMethodAction = InspectBoolean; break;
				case "Color":
					_inspectMethodAction = InspectColor; break;
				case "Int32":
					_inspectMethodAction = InspectInt32; break;
				case "UInt32":
					_inspectMethodAction = InspectUInt32; break;
				case "Int64":
					_inspectMethodAction = InspectInt64; break;
				case "UInt64":
					_inspectMethodAction = InspectUInt64; break;
				case "Single":
					_inspectMethodAction = InspectSingle; break;
				case "Vector2":
					_inspectMethodAction = InspectVector2; break;
				case "Vector3":
					_inspectMethodAction = InspectVector3; break;
			}

			// fix up the Range.minValue if we have an unsigned value to avoid overflow when converting
			_isUnsignedInt = _valueType == typeof(uint) || _valueType == typeof(ulong);
			if (_isUnsignedInt && _rangeAttribute == null)
				_rangeAttribute = new RangeAttribute(0);
			else if (_isUnsignedInt && _rangeAttribute != null && _rangeAttribute.MinValue < 0)
				_rangeAttribute.MinValue = 0;
		}

		public override void DrawMutable()
		{
			foreach (var obj in _toRemove) _dict.Remove(obj);
			_toRemove.Clear();

			foreach (var key in _modifiedValues.Keys)
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

				bool isExpanded = false;

				foreach (var key in _dict.Keys)
				{
					if (ImGui.CollapsingHeader(key.ToString()) && isExpanded is false)
					{
						isExpanded = true;
						_inspectMethodAction(key);
						if(ImGui.Button("Remove key"))
							_toRemove.Add(key);
					}
				}
				HandleTooltip();
			}
		}
		void InspectBoolean(object key)
		{
			var value = (bool)GetValue<IDictionary>()[key];
			if (ImGui.Checkbox(_name, ref value))
				_modifiedValues[key] = value;
		}

		void InspectColor(object key)
		{
			var value = ((Color)GetValue<IDictionary>()[key]).ToNumerics();
			if (ImGui.ColorEdit4(_name, ref value))
				_modifiedValues[key] = value.ToXNAColor();
		}

		/// <summary>
		/// simplifies int, uint, long and ulong handling. They all get converted to Int32 so there is some precision loss.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		bool InspectAnyInt(ref int value)
		{
			if (_rangeAttribute != null)
			{
				if (_rangeAttribute.UseDragVersion)
					return ImGui.DragInt(_name, ref value, 1, (int)_rangeAttribute.MinValue,
						(int)_rangeAttribute.MaxValue);
				else
					return ImGui.SliderInt(_name, ref value, (int)_rangeAttribute.MinValue,
						(int)_rangeAttribute.MaxValue);
			}
			else
			{
				return ImGui.InputInt(_name, ref value);
			}
		}

		void InspectInt32(object key)
		{
			var value = Convert.ToInt32(GetValue<IDictionary>()[key]);

			if (InspectAnyInt(ref value))
				_modifiedValues[key] = value;
		}

		void InspectUInt32(object key)
		{
			var value = Convert.ToInt32(GetValue<IDictionary>()[key]);
			if (InspectAnyInt(ref value))
				_modifiedValues[key] = Convert.ToUInt32(value);
		}

		void InspectInt64(object key)
		{
			var value = Convert.ToInt32(GetValue<IDictionary>()[key]);
			if (InspectAnyInt(ref value))
				_modifiedValues[key] = Convert.ToInt64(value);
		}

		unsafe void InspectUInt64(object key)
		{
			var value = Convert.ToInt32(GetValue<IDictionary>()[key]);
			if (InspectAnyInt(ref value))
				_modifiedValues[key] = Convert.ToUInt64(value);
		}

		void InspectSingle(object key)
		{
			var value = GetValue<float>();
			if (_rangeAttribute != null)
			{
				if (_rangeAttribute.UseDragVersion)
				{
					if (ImGui.DragFloat(_name, ref value, 1, _rangeAttribute.MinValue, _rangeAttribute.MaxValue))
						_modifiedValues[key] = value;
				}
				else
				{
					if (ImGui.SliderFloat(_name, ref value, _rangeAttribute.MinValue, _rangeAttribute.MaxValue))
						_modifiedValues[key] = value;
				}
			}
			else
			{
				if (ImGui.DragFloat(_name, ref value))
					_modifiedValues[key] = value;
			}
		}

		void InspectString(object key)
		{
			var value = (string)GetValue<IDictionary>()[key] ?? string.Empty;
			if (ImGui.InputText(_name, ref value, 100))
				_modifiedValues[key] = value;
		}

		void InspectVector2(object key)
		{
			var value = ((Vector2)GetValue<IDictionary>()[key]).ToNumerics();
			if (ImGui.DragFloat2(_name, ref value))
				_modifiedValues[key] = value.ToXNA();
		}

		void InspectVector3(object key)
		{
			var value = ((Vector3)GetValue<IDictionary>()[key]).ToNumerics();
			if (ImGui.DragFloat3(_name, ref value))
				_modifiedValues[key] = value.ToXNA();
		}
	}
}
