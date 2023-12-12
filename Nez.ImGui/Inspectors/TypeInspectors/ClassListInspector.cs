using ImGuiNET;
using Nez.IEnumerableExtensions;
using Nez.ImGuiTools.TypeInspectors;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Nez.ImGuiTools.TypeInspectors
{
	public class ClassListInspector : AbstractTypeInspector
	{
		IList _list;
		Type _elementType;
		bool _isArray;
		Dictionary<object, AbstractTypeInspector> _objInspectors = new Dictionary<object, AbstractTypeInspector>();

		public override void Initialize()
		{
			base.Initialize();

			_list = (IList)_getter(_target);
			_isArray = _valueType.IsArray;
			_elementType = _valueType.IsArray ? _valueType.GetElementType() : _valueType.GetGenericArguments()[0];

			// null check. we just create an instance if null
			if (_list == null)
			{
				if (_isArray)
				{
					_list = Array.CreateInstance(_elementType, 1);
				}
				else
				{
					var listType = typeof(List<>);
					var constructedListType = listType.MakeGenericType(_elementType);
					_list = (IList)Activator.CreateInstance(constructedListType);
				}
			}
		}

		public override void DrawMutable()
		{
			ImGui.Indent();
			if (ImGui.CollapsingHeader($"{_name} [{_list.Count}]###{_name}", ImGuiTreeNodeFlags.FramePadding))
			{
				ImGui.Indent();

				if (!_isArray)
				{
					if (ImGui.Button("Add Element"))
					{
						if (_elementType == typeof(string))
						{
							_list.Add("");
						}
						else
						{
							_list.Add(Activator.CreateInstance(_elementType));
						}
					}

					ImGui.SameLine(ImGui.GetWindowWidth() - ImGui.GetItemRectSize().X -
								   ImGui.GetStyle().ItemInnerSpacing.X);

					// ImGui.SameLine( 0, ImGui.GetWindowWidth() * 0.65f - ImGui.GetItemRectSize().X + ImGui.GetStyle().ItemInnerSpacing.X - ImGui.GetStyle().IndentSpacing );
					if (ImGui.Button("Clear"))
					{
						ImGui.OpenPopup("Clear Data");
					}

					if (NezImGui.SimpleDialog("Clear Data", "Are you sure you want to clear the data?"))
					{
						_list.Clear();
						Debug.Log($"list count: {_list.Count}");
					}
				}

				ImGui.PushItemWidth(-ImGui.GetStyle().IndentSpacing);
				for (var i = 0; i < _list.Count; i++)
				{
					if (_objInspectors.TryGetValue(_list[i], out var inspector))
					{
						inspector.Draw();
					}
					else
					{
						inspector = TypeInspectorUtils.GetInspectorForType(_elementType, _list[i], null);
						_objInspectors[_list[i]] = inspector;
						var getter = _list.GetType().GetMethod("get_Item");
						inspector.SetTarget(_list[i], getter);
						inspector.SetGetter((obj) => _list[i], $"{_elementType.Name}[{i}]");
						inspector.Initialize();
						inspector.Draw();
					}
				}

				// Cleanup
				List<object> toRemove = new List<object>();
				foreach(var obj in _objInspectors.Keys)
					if(!_list.Contains(obj))
						toRemove.Add(obj);

				foreach (var obj in toRemove)
					_objInspectors.Remove(obj);

				ImGui.PopItemWidth();
				ImGui.Unindent();
			}

			ImGui.Unindent();
		}
	}
}
