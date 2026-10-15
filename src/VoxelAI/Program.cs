using System;
using System.IO;
using System.Windows.Forms;

namespace VoxelAI
{
	internal static class Program
	{
		[STAThread]
		private static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			AppDomain.CurrentDomain.UnhandledException += delegate(object s, UnhandledExceptionEventArgs e)
			{
				try
				{
					string text = ((!(e.ExceptionObject is Exception)) ? "unknown error" : ((Exception)e.ExceptionObject).ToString());
					File.WriteAllText(Path.Combine(Path.GetTempPath(), "voxelai_crash.txt"), text);
					MessageBox.Show("Voxel AI crashed:\n\n" + text + "\n\nDetails were written to %TEMP%\\voxelai_crash.txt", "Voxel AI", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				}
				catch
				{
				}
			};
			Application.Run(new GameWindow());
		}
	}
}
