using ImGuiNET;
using Microsoft.Xna.Framework.Graphics;
using Nez.Textures;
using System;
using System.IO;

namespace Nez.ImGuiTools.TypeInspectors
{
	public class TextureInspector : AbstractTypeInspector
	{
		public static Type[] KSupportedTypes = { typeof(Texture2D), typeof(Sprite) };
		IntPtr _currentPtr;
		Texture2D _cachedTexture;
		string _texPath = string.Empty;

		/// <summary>
		/// If texture was changed outside editor, this will update our pointers
		/// </summary>
		/// <returns></returns>
		Texture2D BindChanges()
		{
			var tex = GetTexture();
			if (tex is null is false && _currentPtr == IntPtr.Zero)
			{
				_currentPtr = Core.GetGlobalManager<ImGuiManager>().BindTexture(tex);
				_cachedTexture = tex;
			}

			if (_cachedTexture != tex && tex is null is false)
			{
				var manager = Core.GetGlobalManager<ImGuiManager>();
				if (_currentPtr != IntPtr.Zero)
					manager.UnbindTexture(_currentPtr);
				_currentPtr = manager.BindTexture(tex);
			}
			return tex;
		}

		Texture2D GetTexture()
		{
			var tex = GetValue();
			if (tex is null)
				return null;
			if (tex is Sprite sprite)
				return sprite.Texture2D;
			return (Texture2D)tex;
		}

		public override void DrawMutable()
		{
			var tex = BindChanges();

			ImGui.Text(_texPath != string.Empty ?
					_texPath.Replace(Environment.CurrentDirectory + Path.DirectorySeparatorChar, "")
					: "No texture selected."
			);
			if (ImGui.IsItemHovered() && _currentPtr != IntPtr.Zero)
			{
				ImGui.BeginTooltip();
				ImGui.Image(_currentPtr, tex.Bounds.GetSize().ToNumerics());
				ImGui.EndTooltip();
			}
			ImGui.SameLine();
			if (ImGui.Button("Browse..."))
				ImGui.OpenPopup("open-texture");

			bool isFileSelectOpen = true;
			if (ImGui.BeginPopupModal("open-texture", ref isFileSelectOpen, ImGuiWindowFlags.NoTitleBar))
			{
				var picker = FilePicker.GetFilePicker(this, Path.Combine(Environment.CurrentDirectory, "Content"), ".png");
				picker.DontAllowTraverselBeyondRootFolder = true;

				if (picker.Draw())
				{
					_texPath = picker.SelectedFile;
					try
					{
						var imguiManager = Core.GetGlobalManager<ImGuiManager>();
						if (_currentPtr != IntPtr.Zero)
							imguiManager.UnbindTexture(_currentPtr);
						var newTex = Core.Scene.Content.LoadTexture(_texPath);
						_cachedTexture = newTex;
						if (_valueType == typeof(Sprite))
							SetValue(new Sprite(newTex));
						else
							SetValue(newTex);
						_currentPtr = imguiManager.BindTexture(newTex);
					}
					catch (Exception ex)
					{
						Debug.Error(ex.ToString());
					}
				}
				ImGui.EndPopup();
			}
		}
	}
}
