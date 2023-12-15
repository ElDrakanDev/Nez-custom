using ImGuiNET;
using System;
using System.IO;
using Nez.Persistence;
using Nez.Persistence.Binary;

namespace Nez.ImGuiTools.TypeInspectors
{
	internal class SerializerInspector : AbstractTypeInspector
	{
		string _savePath;
		Type[] _supportedBinaryTypes = { typeof(string), typeof(int), typeof(uint), typeof(float), typeof(double), typeof(bool) };
		bool _isBinarySupported = false;

		public override void Initialize()
		{
			base.Initialize();

			foreach (var type in _supportedBinaryTypes)
			{
				if(GetValue().GetType() == type)
				{
					_isBinarySupported = true;
					return;
				}
			}
		}

		public override void DrawMutable()
		{
			if (ImGui.CollapsingHeader($"{Name} - Serialization options"))
			{
				var path = _savePath ?? string.Empty;
				if(ImGui.InputText("Save filepath", ref path, 200))
					_savePath = path;
				
				var toJson = ImGui.Button("Save As Json");
				var toNson = ImGui.Button("Save As Nson");
				var toBinary = false;

				if (_isBinarySupported || GetValue() is IPersistable)
					ImGui.Button("Save as binary");
				else
					ImGui.Text("Binary save disabled for non IPersistable objects");

				if (toJson)
					SaveToJson();
				else if(toNson)
					SaveToNson();
				else if(toBinary)
					SaveToBinary();
			}
		}

		void SaveToJson()
		{
			try
			{
				string objAsJson = JsonEncoder.ToJson(GetValue(), new JsonSettings());
				Directory.CreateDirectory(Directory.GetParent(_savePath).FullName);
				File.WriteAllText(_savePath, objAsJson);
				Debug.Log($"Succesfully saved JSON at '{_savePath}'");
			}
			catch (Exception ex)
			{
				Debug.Error(ex.ToString());
			}
		}

		void SaveToNson()
		{
			try
			{
				string objAsNson = NsonEncoder.ToNson(GetValue(), new NsonSettings());
				Directory.CreateDirectory(Directory.GetParent(_savePath).FullName);
				File.WriteAllText(_savePath, objAsNson);
				Debug.Log($"Succesfully saved NSON at '{_savePath}'");
			}
			catch (Exception ex)
			{
				Debug.Error(ex.ToString());
			}
		}

		void SaveToBinary()
		{
			try
			{
				Directory.CreateDirectory(Directory.GetParent(_savePath).FullName);
				using (FileStream fs = File.Create(_savePath))
				{
					var writer = new BinaryPersistableWriter(fs);
					writer.Write(GetValue<IPersistable>());
					Debug.Log($"Succesfully saved Binary at '{_savePath}'");
				}
			}
			catch (Exception ex)
			{
				Debug.Error(ex.ToString());
			}
		}
	}
}
