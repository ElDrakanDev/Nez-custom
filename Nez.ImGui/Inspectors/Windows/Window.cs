namespace Nez.ImGuiTools.Windows
{
	public abstract class Window
	{
		public bool IsActive;
		public virtual System.Numerics.Vector2 Size => new System.Numerics.Vector2(300, Screen.Height / 2);
		public abstract string Title { get; }

		public abstract void Show();
	}
}
