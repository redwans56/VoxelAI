using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using VoxelAI.Game;
using VoxelAI.Gfx;

namespace VoxelAI
{
	public class GameWindow : Form
	{
		private struct NativeMessage
		{
			public IntPtr hWnd;

			public uint msg;

			public IntPtr wParam;

			public IntPtr lParam;

			public uint time;

			public int x;

			public int y;
		}

		private VoxelAI.Game.Game game;

		private GlContext ctx;

		private InputState input = new InputState();

		private readonly Stopwatch clock = Stopwatch.StartNew();

		private float lastTime;

		private bool running;

		private bool shuttingDown;

		private int frameErrors;

		private bool autoScreenshots;

		private const uint PM_NOREMOVE = 0u;

		public GameWindow(bool autoScreenshots = false)
		{
			this.autoScreenshots = autoScreenshots;
			Text = "Voxel AI - .NET Framework voxel world with self improving AI";
			base.ClientSize = new Size(1280, 720);
			base.StartPosition = FormStartPosition.CenterScreen;
			BackColor = Color.Black;
			base.KeyPreview = true;
			MinimumSize = new Size(640, 400);
		}

		private static void LogException(Exception ex)
		{
			try
			{
				File.WriteAllText(Path.Combine(Path.GetTempPath(), "voxelai_crash.txt"), ex.ToString());
			}
			catch
			{
			}
		}

		[DllImport("user32.dll")]
		private static extern bool PeekMessage(out NativeMessage msg, IntPtr hWnd, uint min, uint max, uint remove);

		protected override bool IsInputKey(Keys keyData)
		{
			return true;
		}

		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);
			ctx = new GlContext();
			if (!ctx.Create(base.Handle))
			{
				MessageBox.Show(this, "Could not create an OpenGL context.\n\n" + ctx.Error + "\n\nA GPU driver from 2005 or newer is required.", "Voxel AI", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				Close();
				return;
			}
			GL.LoadExtensions();
			GL.SetSwapInterval(1);
			game = new VoxelAI.Game.Game(input);
			game.Window = this;
			game.AutoCaptureMode = autoScreenshots;
			game.Camera.Width = base.ClientSize.Width;
			game.Camera.Height = base.ClientSize.Height;
			try
			{
				game.Init();
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, "Renderer init failed:\n" + ex.Message, "Voxel AI");
				Close();
				return;
			}
			game.Resize(base.ClientSize.Width, base.ClientSize.Height);
			Application.Idle += OnIdle;
			running = true;
			lastTime = (float)clock.Elapsed.TotalSeconds;
		}

		private void OnIdle(object sender, EventArgs e)
		{
			while (running && AppStillIdle() && !base.IsDisposed)
			{
				if (game == null)
				{
					running = false;
					break;
				}
				if (game.Current == Mode.Exiting)
				{
					Close();
					break;
				}
				float num = (float)clock.Elapsed.TotalSeconds;
				float num2 = num - lastTime;
				lastTime = num;
				if (num2 <= 0f)
				{
					num2 = 0.0001f;
				}
				DoCursorWarp();
				try
				{
					game.Frame(num2);
					frameErrors = 0;
				}
				catch (Exception ex)
				{
					frameErrors++;
					if (frameErrors == 1)
					{
						LogException(ex);
					}
					if (frameErrors > 120)
					{
						LogException(ex);
						MessageBox.Show("Voxel AI stopped after 120 failed frames:\n\n" + ex.Message + "\n\nDetails: %TEMP%\\voxelai_crash.txt", "Voxel AI");
						Close();
						return;
					}
				}
				ctx.SwapBuffers();
				input.EndFrame();
				if (!GL.HasSwapInterval)
				{
					Thread.Sleep(1);
				}
			}
		}

		private static bool AppStillIdle()
		{
			NativeMessage msg;
			return !PeekMessage(out msg, IntPtr.Zero, 0u, 0u, 0u);
		}

		private void DoCursorWarp()
		{
			if (game != null && input.MouseCaptured)
			{
				Point p = new Point(base.ClientSize.Width / 2, base.ClientSize.Height / 2);
				Point point = PointToScreen(p);
				if (Cursor.Position != point)
				{
					input.MarkWarped();
					Cursor.Position = point;
				}
				Cursor.Hide();
			}
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			input.SetKey(e.KeyCode, true);
			e.Handled = true;
			base.OnKeyDown(e);
		}

		protected override void OnKeyUp(KeyEventArgs e)
		{
			input.SetKey(e.KeyCode, false);
			e.Handled = true;
			base.OnKeyUp(e);
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			input.SetMouseButton(e.Button, true);
			if (!input.MouseCaptured && game != null && game.Current == Mode.Playing)
			{
				game.SetMouseCaptured(true);
			}
			base.OnMouseDown(e);
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			input.SetMouseButton(e.Button, false);
			base.OnMouseUp(e);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			input.OnMouseMove(e.X, e.Y);
			base.OnMouseMove(e);
		}

		protected override void OnMouseWheel(MouseEventArgs e)
		{
			input.OnWheel(e.Delta);
			base.OnMouseWheel(e);
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			if (game != null && game.Current == Mode.Playing)
			{
				input.SetCapture(false, base.ClientSize.Width, base.ClientSize.Height);
				Cursor.Show();
			}
			base.OnMouseLeave(e);
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			if (base.ClientSize.Width > 0 && base.ClientSize.Height > 0)
			{
				if (ctx != null && ctx.Hrc != IntPtr.Zero)
				{
					ctx.MakeCurrent();
				}
				if (game != null)
				{
					game.Resize(base.ClientSize.Width, base.ClientSize.Height);
					input.SetCapture(input.MouseCaptured, base.ClientSize.Width, base.ClientSize.Height);
				}
			}
		}

		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			if (!shuttingDown && game != null)
			{
				shuttingDown = true;
				Application.Idle -= OnIdle;
				running = false;
				Cursor.Show();
				try
				{
					game.Shutdown();
				}
				catch
				{
				}
				if (ctx != null)
				{
					ctx.Dispose();
				}
			}
			base.OnFormClosing(e);
		}
	}
}
