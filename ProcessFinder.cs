using System.Runtime.InteropServices;

namespace remotewinlauncher
{
	public static class ProcessFinder
	{
		[DllImport("user32.dll")]
		private static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

		[DllImport("user32.dll")]
		public static extern int SetWindowText(IntPtr hWnd, string text);

		public static IntPtr GetHandleWindow(string title)
		{
			return FindWindow(null, title);
		}
	}
}
