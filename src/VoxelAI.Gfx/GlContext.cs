using System;
using System.Runtime.InteropServices;

namespace VoxelAI.Gfx
{
	public class GlContext : IDisposable
	{
		private const uint PFD_DRAW_TO_WINDOW = 4u;

		private const uint PFD_SUPPORT_OPENGL = 32u;

		private const uint PFD_DOUBLEBUFFER = 1u;

		private const byte PFD_TYPE_RGBA = 0;

		private const byte PFD_MAIN_PLANE = 0;

		public IntPtr Hdc
		{
			get;
			private set;
		}

		public IntPtr Hrc
		{
			get;
			private set;
		}

		public int ColorBits
		{
			get;
			private set;
		}

		public int DepthBits
		{
			get;
			private set;
		}

		public string Error
		{
			get;
			private set;
		}

		public static bool IsCurrent
		{
			get
			{
				return wglGetCurrentContext() != IntPtr.Zero;
			}
		}

		[DllImport("user32.dll")]
		private static extern IntPtr GetDC(IntPtr hwnd);

		[DllImport("user32.dll")]
		private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

		[DllImport("gdi32.dll")]
		private static extern int ChoosePixelFormat(IntPtr hdc, ref PIXELFORMATDESCRIPTOR pfd);

		[DllImport("gdi32.dll")]
		private static extern bool SetPixelFormat(IntPtr hdc, int format, ref PIXELFORMATDESCRIPTOR pfd);

		[DllImport("gdi32.dll")]
		private static extern bool SwapBuffers(IntPtr hdc);

		[DllImport("gdi32.dll")]
		private static extern int DescribePixelFormat(IntPtr hdc, int format, int size, ref PIXELFORMATDESCRIPTOR pfd);

		[DllImport("opengl32.dll")]
		private static extern IntPtr wglCreateContext(IntPtr hdc);

		[DllImport("opengl32.dll")]
		private static extern bool wglMakeCurrent(IntPtr hdc, IntPtr hglrc);

		[DllImport("opengl32.dll")]
		private static extern bool wglDeleteContext(IntPtr hglrc);

		[DllImport("opengl32.dll")]
		private static extern IntPtr wglGetCurrentContext();

		public bool Create(IntPtr hwnd)
		{
			Hdc = GetDC(hwnd);
			if (Hdc == IntPtr.Zero)
			{
				Error = "GetDC failed";
				return false;
			}
			PIXELFORMATDESCRIPTOR pfd = default(PIXELFORMATDESCRIPTOR);
			pfd.nSize = (ushort)Marshal.SizeOf(typeof(PIXELFORMATDESCRIPTOR));
			pfd.nVersion = 1;
			pfd.dwFlags = 37u;
			pfd.iPixelType = 0;
			pfd.cColorBits = 32;
			pfd.cDepthBits = 24;
			pfd.cStencilBits = 8;
			pfd.iLayerType = 0;
			int num = ChoosePixelFormat(Hdc, ref pfd);
			if (num == 0)
			{
				Error = "ChoosePixelFormat failed";
				return false;
			}
			if (!SetPixelFormat(Hdc, num, ref pfd))
			{
				Error = "SetPixelFormat failed";
				return false;
			}
			PIXELFORMATDESCRIPTOR pfd2 = default(PIXELFORMATDESCRIPTOR);
			pfd2.nSize = (ushort)Marshal.SizeOf(typeof(PIXELFORMATDESCRIPTOR));
			if (DescribePixelFormat(Hdc, num, pfd2.nSize, ref pfd2) != 0)
			{
				ColorBits = pfd2.cColorBits;
				DepthBits = pfd2.cDepthBits;
			}
			Hrc = wglCreateContext(Hdc);
			if (Hrc == IntPtr.Zero)
			{
				Error = "wglCreateContext failed";
				return false;
			}
			return MakeCurrent();
		}

		public bool MakeCurrent()
		{
			if (Hrc == IntPtr.Zero)
			{
				return false;
			}
			if (!wglMakeCurrent(Hdc, Hrc))
			{
				Error = "wglMakeCurrent failed";
				return false;
			}
			return true;
		}

		public void SwapBuffers()
		{
			if (Hdc != IntPtr.Zero)
			{
				SwapBuffers(Hdc);
			}
		}

		public void Dispose()
		{
			try
			{
				if (Hrc != IntPtr.Zero)
				{
					wglMakeCurrent(IntPtr.Zero, IntPtr.Zero);
					wglDeleteContext(Hrc);
					Hrc = IntPtr.Zero;
				}
				if (Hdc != IntPtr.Zero)
				{
					ReleaseDC(IntPtr.Zero, Hdc);
					Hdc = IntPtr.Zero;
				}
			}
			catch
			{
			}
		}
	}
}
