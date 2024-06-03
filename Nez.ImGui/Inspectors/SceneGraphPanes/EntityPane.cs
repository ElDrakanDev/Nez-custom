using ImGuiNET;
using System.Reflection;
using System.Linq;
using Nez.ImGuiTools.ObjectInspectors;
using System;
using System.Collections.Generic;

namespace Nez.ImGuiTools.SceneGraphPanes
{
	public class EntityPane
	{
		/// <summary>
		/// if this number of entites is exceeded we switch to a list clipper to keep things fast
		/// </summary>
		const int MIN_ENTITIES_FOR_CLIPPER = 100;
		string _newEntityName = "";
		MethodInfo[] _listExtensionMethods;
		MethodInfo[] _entityContextMethods;

		public EntityPane()
		{
			var listExtensionMethods = new List<MethodInfo>();
			var entityContextMethods = new List<MethodInfo>();
			var assemblies = AppDomain.CurrentDomain.GetAssemblies();

			foreach (var assembly in assemblies)
			{
				listExtensionMethods.AddRange(
					assembly.GetTypes()
						.SelectMany(t => t.GetMethods(BindingFlags.Public	| BindingFlags.NonPublic | BindingFlags.Static))
						.Where(m => m.GetCustomAttributes(typeof(InspectorEntityListExtension), false).Length > 0)
				);
				entityContextMethods.AddRange(
					assembly.GetTypes()
						.SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
						.Where(m => m.GetCustomAttributes(typeof(InspectorEntityContextMenu), false).Length > 0
							&& (
								m.GetParameters().FirstOrDefault().ParameterType == typeof(Entity)
								|| m.GetParameters().FirstOrDefault().ParameterType.IsSubclassOf(typeof(Entity))
							)
						)
				);
			}

			_listExtensionMethods = listExtensionMethods.ToArray();
			_entityContextMethods = entityContextMethods.ToArray();

			Insist.IsFalse(
				_entityContextMethods.Select(
					m => m.GetAttribute<InspectorEntityContextMenu>().Name.Replace(" ", "-")
				).ToHashSet().Count < _entityContextMethods.Length);
		}

		unsafe public void Draw()
		{
			if (Core.Scene.Entities.Count > MIN_ENTITIES_FOR_CLIPPER)
			{
				var clipperPtr = ImGuiNative.ImGuiListClipper_ImGuiListClipper();
				var clipper = new ImGuiListClipperPtr(clipperPtr);
				
				clipper.Begin(Core.Scene.Entities.Count, -1);

				while (clipper.Step())
					for (var i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
						DrawEntity(Core.Scene.Entities[i]);

				ImGuiNative.ImGuiListClipper_destroy(clipperPtr);
			}
			else
			{
				for (var i = 0; i < Core.Scene.Entities.Count; i++)
					DrawEntity(Core.Scene.Entities[i]);
			}

			NezImGui.MediumVerticalSpace();
			if (NezImGui.CenteredButton("Create Entity", 0.6f))
			{
				ImGui.OpenPopup("create-entity");
			}

			DrawCreateEntityPopup();

			foreach (var method in _listExtensionMethods)
				method.Invoke(method.DeclaringType, null);
		}

		void DrawEntity(Entity entity, bool onlyDrawRoots = true)
		{
			if (onlyDrawRoots && entity.Transform.Parent != null)
				return;

			ImGui.PushID((int) entity.Id);
			if(entity.Enabled is false)
				ImGui.PushStyleColor(ImGuiCol.Text, Microsoft.Xna.Framework.Color.Gray.ToVector4().ToNumerics());
			bool treeNodeOpened;
			if (entity.Transform.ChildCount > 0)
			{
				treeNodeOpened = ImGui.TreeNodeEx($"{entity.Name} ({entity.Transform.ChildCount})###{entity.Id}",
					ImGuiTreeNodeFlags.OpenOnArrow);
			}
			else
			{
				treeNodeOpened = ImGui.TreeNodeEx($"{entity.Name} ({entity.Transform.ChildCount})###{entity.Id}",
					ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.OpenOnArrow);
			}

			if (entity.Enabled is false)
				ImGui.PopStyleColor();

			NezImGui.ShowContextMenuTooltip();

			// context menu for entity commands
			ImGui.OpenPopupOnItemClick("entityContextMenu", ImGuiPopupFlags.MouseButtonRight);
			DrawEntityContextMenuPopup(entity);

			// we are looking for a double-click that is not on the arrow
			if (ImGui.IsMouseDoubleClicked(0) && ImGui.IsItemClicked() &&
			    (ImGui.GetMousePos().X - ImGui.GetItemRectMin().X) > ImGui.GetTreeNodeToLabelSpacing())
			{
				Core.GetGlobalManager<ImGuiManager>().StartInspectingEntity(entity);
			}

			if (treeNodeOpened)
			{
				for (var i = 0; i < entity.Transform.ChildCount; i++)
					DrawEntity(entity.Transform.GetChild(i).Entity, false);

				ImGui.TreePop();
			}

			ImGui.PopID();
		}

		void DrawEntityContextMenuPopup(Entity entity)
		{
			if (ImGui.BeginPopup("entityContextMenu"))
			{
				if (ImGui.Selectable("Clone Entity " + entity.Name))
				{
					var clone = entity.Clone(Core.Scene.Camera.Position);
					entity.Scene.AddEntity(clone);
				}

				if (ImGui.Selectable("Destroy Entity"))
					entity.Destroy();

				if (ImGui.Selectable("Create Child Entity", false, ImGuiSelectableFlags.DontClosePopups))
					ImGui.OpenPopup("create-new-entity");

				if (ImGui.BeginPopup("create-new-entity"))
				{
					ImGui.Text("New Entity Name:");
					ImGui.InputText("##newChildEntityName", ref _newEntityName, 25);

					if (ImGui.Button("Cancel"))
					{
						_newEntityName = "";
						ImGui.CloseCurrentPopup();
					}

					ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.GetItemRectSize().X);

					ImGui.PushStyleColor(ImGuiCol.Button, Microsoft.Xna.Framework.Color.Green.PackedValue);
					if (ImGui.Button("Create"))
					{
						_newEntityName = _newEntityName.Length > 0 ? _newEntityName : Utils.RandomString(8);
						var newEntity = new Entity(_newEntityName);
						newEntity.Transform.SetParent(entity.Transform);
						entity.Scene.AddEntity(newEntity);

						_newEntityName = "";
						ImGui.CloseCurrentPopup();
					}

					ImGui.PopStyleColor();

					ImGui.EndPopup();
				}

				foreach(var method in _entityContextMethods)
				{
					var attr = method.GetAttribute<InspectorEntityContextMenu>();
					var selectableName = attr.Name;

					if (attr.IsPopup)
					{
						var id = $"{method.Name}-{selectableName.Replace(" ", "")}";
						if (ImGui.Selectable(selectableName, false, ImGuiSelectableFlags.DontClosePopups))
							ImGui.OpenPopup(id);
						if (ImGui.BeginPopup(id))
						{
							method.Invoke(null, new object[]{entity});
							ImGui.EndPopup();
						}
					}
					else
					{
						if (ImGui.Selectable(selectableName))
							method.Invoke(null, new object[] { entity });
					}
				}

				ImGui.EndPopup();
			}
		}

		void DrawCreateEntityPopup()
		{
			if (ImGui.BeginPopup("create-entity"))
			{
				ImGui.Text("New Entity Name:");
				ImGui.InputText("##newEntityName", ref _newEntityName, 25);

				if (ImGui.Button("Cancel"))
				{
					_newEntityName = "";
					ImGui.CloseCurrentPopup();
				}

				ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.GetItemRectSize().X);

				ImGui.PushStyleColor(ImGuiCol.Button, Microsoft.Xna.Framework.Color.Green.PackedValue);
				if (ImGui.Button("Create"))
				{
					_newEntityName = _newEntityName.Length > 0 ? _newEntityName : Utils.RandomString(8);
					var newEntity = new Entity(_newEntityName);
					newEntity.Transform.Position = Core.Scene.Camera.Transform.Position;
					Core.Scene.AddEntity(newEntity);

					_newEntityName = "";
					ImGui.CloseCurrentPopup();
				}

				ImGui.PopStyleColor();
				ImGui.EndPopup();
			}
		}
	}
}