using System;
using System.Runtime.InteropServices;

namespace VoxelAI.Gfx
{
	public static class GL
	{
		private delegate void DelGenBuffers(int n, int[] buffers);

		private delegate void DelBindBuffer(uint target, int buffer);

		private delegate void DelBufferData(uint target, int size, IntPtr data, uint usage);

		private delegate void DelBufferDataArr(uint target, int size, float[] data, uint usage);

		private delegate void DelDeleteBuffers(int n, int[] buffers);

		private delegate int DelSwapInterval(int interval);

		private const string OPENGL32 = "opengl32.dll";

		public const uint GL_FALSE = 0u;

		public const uint GL_TRUE = 1u;

		public const uint GL_POINTS = 0u;

		public const uint GL_LINES = 1u;

		public const uint GL_LINE_LOOP = 2u;

		public const uint GL_LINE_STRIP = 3u;

		public const uint GL_TRIANGLES = 4u;

		public const uint GL_QUADS = 7u;

		public const uint GL_DEPTH_BUFFER_BIT = 256u;

		public const uint GL_COLOR_BUFFER_BIT = 16384u;

		public const uint GL_SMOOTH = 7425u;

		public const uint GL_FLAT = 7424u;

		public const uint GL_CULL_FACE = 2884u;

		public const uint GL_BACK = 1029u;

		public const uint GL_FRONT = 1028u;

		public const uint GL_CCW = 2305u;

		public const uint GL_CW = 2304u;

		public const uint GL_DEPTH_TEST = 2929u;

		public const uint GL_LESS = 513u;

		public const uint GL_LEQUAL = 515u;

		public const uint GL_GREATER = 516u;

		public const uint GL_GEQUAL = 518u;

		public const uint GL_BLEND = 3042u;

		public const uint GL_SRC_ALPHA = 770u;

		public const uint GL_ONE_MINUS_SRC_ALPHA = 771u;

		public const uint GL_ONE = 1u;

		public const uint GL_ZERO = 0u;

		public const uint GL_ALPHA_TEST = 3008u;

		public const uint GL_TEXTURE_2D = 3553u;

		public const uint GL_TEXTURE_ENV = 8960u;

		public const uint GL_TEXTURE_ENV_MODE = 8704u;

		public const uint GL_MODULATE = 8448u;

		public const uint GL_REPLACE = 7681u;

		public const uint GL_DECAL = 8449u;

		public const uint GL_TEXTURE_MAG_FILTER = 10240u;

		public const uint GL_TEXTURE_MIN_FILTER = 10241u;

		public const uint GL_TEXTURE_WRAP_S = 10242u;

		public const uint GL_TEXTURE_WRAP_T = 10243u;

		public const uint GL_CLAMP = 10496u;

		public const uint GL_CLAMP_TO_EDGE = 33071u;

		public const uint GL_REPEAT = 10497u;

		public const uint GL_NEAREST = 9728u;

		public const uint GL_LINEAR = 9729u;

		public const uint GL_NEAREST_MIPMAP_NEAREST = 9984u;

		public const uint GL_LINEAR_MIPMAP_NEAREST = 9985u;

		public const uint GL_NEAREST_MIPMAP_LINEAR = 9986u;

		public const uint GL_LINEAR_MIPMAP_LINEAR = 9987u;

		public const uint GL_RGBA = 6408u;

		public const uint GL_RGB = 6407u;

		public const uint GL_BGRA = 32993u;

		public const uint GL_RGBA8 = 32856u;

		public const uint GL_UNSIGNED_BYTE = 5121u;

		public const uint GL_UNSIGNED_SHORT = 5123u;

		public const uint GL_UNSIGNED_INT = 5125u;

		public const uint GL_FLOAT = 5126u;

		public const uint GL_PROJECTION = 5889u;

		public const uint GL_MODELVIEW = 5888u;

		public const uint GL_TEXTURE = 5890u;

		public const uint GL_MODELVIEW_MATRIX = 2982u;

		public const uint GL_PROJECTION_MATRIX = 2983u;

		public const uint GL_FILL = 6914u;

		public const uint GL_LINE = 6913u;

		public const uint GL_FOG = 2912u;

		public const uint GL_FOG_MODE = 2917u;

		public const uint GL_FOG_DENSITY = 2914u;

		public const uint GL_FOG_START = 2915u;

		public const uint GL_FOG_END = 2916u;

		public const uint GL_FOG_COLOR = 2918u;

		public const uint GL_EXP = 2048u;

		public const uint GL_EXP2 = 2049u;

		public const uint GL_POLYGON_OFFSET_FILL = 32823u;

		public const uint GL_POLYGON_OFFSET_LINE = 10754u;

		public const uint GL_PERSPECTIVE_CORRECTION_HINT = 3152u;

		public const uint GL_NICEST = 4354u;

		public const uint GL_FASTEST = 4353u;

		public const uint GL_DONT_CARE = 4352u;

		public const uint GL_VERSION = 7938u;

		public const uint GL_VENDOR = 7936u;

		public const uint GL_RENDERER = 7937u;

		public const uint GL_EXTENSIONS = 7939u;

		public const uint GL_PACK_ALIGNMENT = 3333u;

		public const uint GL_UNPACK_ALIGNMENT = 3317u;

		public const uint GL_ARRAY_BUFFER = 34962u;

		public const uint GL_ELEMENT_ARRAY_BUFFER = 34963u;

		public const uint GL_STATIC_DRAW = 35044u;

		public const uint GL_DYNAMIC_DRAW = 35048u;

		public const uint GL_STREAM_DRAW = 35040u;

		public const uint GL_VERTEX_ARRAY = 32884u;

		public const uint GL_COLOR_ARRAY = 32886u;

		public const uint GL_TEXTURE_COORD_ARRAY = 32888u;

		public const uint GL_NORMAL_ARRAY = 32887u;

		public const uint GL_LIGHTING = 2896u;

		public const uint GL_LIGHT0 = 16384u;

		public const uint GL_COLOR_MATERIAL = 2903u;

		public const uint GL_SCISSOR_TEST = 3089u;

		public const uint GL_NO_ERROR = 0u;

		public const uint GL_MULTISAMPLE = 32925u;

		public const int GL_MAX_TEXTURE_SIZE = 3379;

		private static DelGenBuffers _genBuffers;

		private static DelBindBuffer _bindBuffer;

		private static DelBufferData _bufferData;

		private static DelBufferDataArr _bufferDataArr;

		private static DelDeleteBuffers _deleteBuffers;

		private static DelSwapInterval _swapInterval;

		public static bool HasVbo
		{
			get;
			private set;
		}

		public static bool HasSwapInterval
		{
			get;
			private set;
		}

		public static string Version
		{
			get
			{
				return PtrToString(7938u);
			}
		}

		public static string Vendor
		{
			get
			{
				return PtrToString(7936u);
			}
		}

		public static string RendererName
		{
			get
			{
				return PtrToString(7937u);
			}
		}

		[DllImport("opengl32.dll", EntryPoint = "glClear")]
		public static extern void Clear(uint mask);

		[DllImport("opengl32.dll", EntryPoint = "glClearColor")]
		public static extern void ClearColor(float r, float g, float b, float a);

		[DllImport("opengl32.dll", EntryPoint = "glClearDepth")]
		public static extern void ClearDepth(double d);

		[DllImport("opengl32.dll", EntryPoint = "glViewport")]
		public static extern void Viewport(int x, int y, int w, int h);

		[DllImport("opengl32.dll", EntryPoint = "glScissor")]
		public static extern void Scissor(int x, int y, int w, int h);

		[DllImport("opengl32.dll", EntryPoint = "glMatrixMode")]
		public static extern void MatrixMode(uint mode);

		[DllImport("opengl32.dll", EntryPoint = "glLoadIdentity")]
		public static extern void LoadIdentity();

		[DllImport("opengl32.dll", EntryPoint = "glLoadMatrixf")]
		public static extern void LoadMatrixf(float[] m);

		[DllImport("opengl32.dll", EntryPoint = "glMultMatrixf")]
		public static extern void MultMatrixf(float[] m);

		[DllImport("opengl32.dll", EntryPoint = "glPushMatrix")]
		public static extern void PushMatrix();

		[DllImport("opengl32.dll", EntryPoint = "glPopMatrix")]
		public static extern void PopMatrix();

		[DllImport("opengl32.dll", EntryPoint = "glTranslatef")]
		public static extern void Translatef(float x, float y, float z);

		[DllImport("opengl32.dll", EntryPoint = "glRotatef")]
		public static extern void Rotatef(float angleDeg, float x, float y, float z);

		[DllImport("opengl32.dll", EntryPoint = "glScalef")]
		public static extern void Scalef(float x, float y, float z);

		[DllImport("opengl32.dll", EntryPoint = "glOrtho")]
		public static extern void Ortho(double l, double r, double b, double t, double n, double f);

		[DllImport("opengl32.dll", EntryPoint = "glFrustum")]
		public static extern void Frustum(double l, double r, double b, double t, double n, double f);

		[DllImport("opengl32.dll", EntryPoint = "glEnable")]
		public static extern void Enable(uint cap);

		[DllImport("opengl32.dll", EntryPoint = "glDisable")]
		public static extern void Disable(uint cap);

		[DllImport("opengl32.dll", EntryPoint = "glBlendFunc")]
		public static extern void BlendFunc(uint sfactor, uint dfactor);

		[DllImport("opengl32.dll", EntryPoint = "glCullFace")]
		public static extern void CullFace(uint mode);

		[DllImport("opengl32.dll", EntryPoint = "glFrontFace")]
		public static extern void FrontFace(uint mode);

		[DllImport("opengl32.dll", EntryPoint = "glShadeModel")]
		public static extern void ShadeModel(uint mode);

		[DllImport("opengl32.dll", EntryPoint = "glDepthFunc")]
		public static extern void DepthFunc(uint func);

		[DllImport("opengl32.dll", EntryPoint = "glDepthMask")]
		public static extern void DepthMask(bool flag);

		[DllImport("opengl32.dll", EntryPoint = "glColorMask")]
		public static extern void ColorMask(bool r, bool g, bool b, bool a);

		[DllImport("opengl32.dll", EntryPoint = "glAlphaFunc")]
		public static extern void AlphaFunc(uint func, float reference);

		[DllImport("opengl32.dll", EntryPoint = "glLineWidth")]
		public static extern void LineWidth(float w);

		[DllImport("opengl32.dll", EntryPoint = "glPolygonOffset")]
		public static extern void PolygonOffset(float factor, float units);

		[DllImport("opengl32.dll", EntryPoint = "glHint")]
		public static extern void Hint(uint target, uint mode);

		[DllImport("opengl32.dll", EntryPoint = "glPixelStorei")]
		public static extern void PixelStorei(uint pname, int param);

		[DllImport("opengl32.dll", EntryPoint = "glFinish")]
		public static extern void Finish();

		[DllImport("opengl32.dll", EntryPoint = "glFlush")]
		public static extern void Flush2();

		[DllImport("opengl32.dll", EntryPoint = "glGetError")]
		public static extern uint GetError();

		[DllImport("opengl32.dll", EntryPoint = "glGetString")]
		public static extern IntPtr GetStringPtr(uint name);

		[DllImport("opengl32.dll", EntryPoint = "glGetIntegerv")]
		public static extern void GetIntegerv(uint pname, int[] data);

		[DllImport("opengl32.dll", EntryPoint = "glGetFloatv")]
		public static extern void GetFloatv(uint pname, float[] data);

		[DllImport("opengl32.dll", EntryPoint = "glReadPixels")]
		public static extern void ReadPixels(int x, int y, int w, int h, uint format, uint type, byte[] pixels);

		[DllImport("opengl32.dll", EntryPoint = "glReadPixels")]
		public static extern void ReadPixelsBgra(int x, int y, int w, int h, uint format, uint type, byte[] pixels);

		[DllImport("opengl32.dll", EntryPoint = "glGenTextures")]
		public static extern void GenTextures(int n, int[] textures);

		[DllImport("opengl32.dll", EntryPoint = "glDeleteTextures")]
		public static extern void DeleteTextures(int n, int[] textures);

		[DllImport("opengl32.dll", EntryPoint = "glBindTexture")]
		public static extern void BindTexture(uint target, int texture);

		[DllImport("opengl32.dll", EntryPoint = "glTexImage2D")]
		public static extern void TexImage2D(uint target, int level, int internalFormat, int w, int h, int border, uint format, uint type, IntPtr pixels);

		[DllImport("opengl32.dll", EntryPoint = "glTexImage2D")]
		public static extern void TexImage2D(uint target, int level, int internalFormat, int w, int h, int border, uint format, uint type, byte[] pixels);

		[DllImport("opengl32.dll", EntryPoint = "glTexSubImage2D")]
		public static extern void TexSubImage2D(uint target, int level, int xoff, int yoff, int w, int h, uint format, uint type, byte[] pixels);

		[DllImport("opengl32.dll", EntryPoint = "glTexParameteri")]
		public static extern void TexParameteri(uint target, uint pname, int param);

		[DllImport("opengl32.dll", EntryPoint = "glTexParameterf")]
		public static extern void TexParameterf(uint target, uint pname, float param);

		[DllImport("opengl32.dll", EntryPoint = "glTexEnvi")]
		public static extern void TexEnvi(uint target, uint pname, int param);

		[DllImport("opengl32.dll", EntryPoint = "glTexCoordPointer")]
		public static extern void TexCoordPointer(int size, uint type, int stride, IntPtr pointer);

		[DllImport("opengl32.dll", EntryPoint = "glVertexPointer")]
		public static extern void VertexPointer(int size, uint type, int stride, IntPtr pointer);

		[DllImport("opengl32.dll", EntryPoint = "glColorPointer")]
		public static extern void ColorPointer(int size, uint type, int stride, IntPtr pointer);

		[DllImport("opengl32.dll", EntryPoint = "glEnableClientState")]
		public static extern void EnableClientState(uint array);

		[DllImport("opengl32.dll", EntryPoint = "glDisableClientState")]
		public static extern void DisableClientState(uint array);

		[DllImport("opengl32.dll", EntryPoint = "glDrawArrays")]
		public static extern void DrawArrays(uint mode, int first, int count);

		[DllImport("opengl32.dll", EntryPoint = "glDrawElements")]
		public static extern void DrawElements(uint mode, int count, uint type, IntPtr indices);

		[DllImport("opengl32.dll", EntryPoint = "glColor4f")]
		public static extern void Color4f(float r, float g, float b, float a);

		[DllImport("opengl32.dll", EntryPoint = "glColor3f")]
		public static extern void Color3f(float r, float g, float b);

		[DllImport("opengl32.dll", EntryPoint = "glColor4ub")]
		public static extern void Color4ub(byte r, byte g, byte b, byte a);

		[DllImport("opengl32.dll", EntryPoint = "glFogf")]
		public static extern void Fogf(uint pname, float param);

		[DllImport("opengl32.dll", EntryPoint = "glFogi")]
		public static extern void Fogi(uint pname, int param);

		[DllImport("opengl32.dll", EntryPoint = "glFogfv")]
		public static extern void Fogfv(uint pname, float[] params4);

		[DllImport("opengl32.dll", EntryPoint = "glPolygonMode")]
		public static extern void PolygonMode(uint face, uint mode);

		[DllImport("opengl32.dll", EntryPoint = "glBegin")]
		public static extern void Begin(uint mode);

		[DllImport("opengl32.dll", EntryPoint = "glEnd")]
		public static extern void End();

		[DllImport("opengl32.dll", EntryPoint = "glVertex3f")]
		public static extern void Vertex3f(float x, float y, float z);

		[DllImport("opengl32.dll", EntryPoint = "glTexCoord2f")]
		public static extern void TexCoord2f(float u, float v);

		[DllImport("opengl32.dll", CharSet = CharSet.Ansi)]
		private static extern IntPtr wglGetProcAddress(string proc);

		private static IntPtr Proc(string name)
		{
			try
			{
				IntPtr intPtr = wglGetProcAddress(name);
				if (intPtr == IntPtr.Zero || intPtr == (IntPtr)1 || intPtr == (IntPtr)2 || intPtr == (IntPtr)3 || intPtr == (IntPtr)(-1))
				{
					return IntPtr.Zero;
				}
				return intPtr;
			}
			catch
			{
				return IntPtr.Zero;
			}
		}

		private static T Load<T>(string name) where T : class
		{
			IntPtr intPtr = Proc(name);
			if (intPtr == IntPtr.Zero)
			{
				return (T)null;
			}
			try
			{
				return (T)(object)Marshal.GetDelegateForFunctionPointer(intPtr, typeof(T));
			}
			catch
			{
				return (T)null;
			}
		}

		public static void LoadExtensions()
		{
			_genBuffers = Load<DelGenBuffers>("glGenBuffers");
			_bindBuffer = Load<DelBindBuffer>("glBindBuffer");
			_bufferData = Load<DelBufferData>("glBufferData");
			_bufferDataArr = Load<DelBufferDataArr>("glBufferData");
			_deleteBuffers = Load<DelDeleteBuffers>("glDeleteBuffers");
			_swapInterval = Load<DelSwapInterval>("wglSwapIntervalEXT");
			HasVbo = _genBuffers != null && _bindBuffer != null && _bufferDataArr != null && _deleteBuffers != null;
			HasSwapInterval = _swapInterval != null;
		}

		public static void SetSwapInterval(int interval)
		{
			if (_swapInterval != null)
			{
				try
				{
					_swapInterval(interval);
				}
				catch
				{
				}
			}
		}

		private static string PtrToString(uint which)
		{
			try
			{
				IntPtr stringPtr = GetStringPtr(which);
				if (stringPtr == IntPtr.Zero)
				{
					return "unknown";
				}
				return Marshal.PtrToStringAnsi(stringPtr);
			}
			catch
			{
				return "unknown";
			}
		}

		public static int GenBuffer()
		{
			if (!HasVbo)
			{
				return 0;
			}
			try
			{
				int[] array = new int[1];
				_genBuffers(1, array);
				return array[0];
			}
			catch
			{
				return 0;
			}
		}

		public static void BindBuffer(uint target, int buffer)
		{
			if (_bindBuffer != null)
			{
				_bindBuffer(target, buffer);
			}
		}

		public static void BufferData(uint target, float[] data, uint usage)
		{
			if (_bufferDataArr != null)
			{
				_bufferDataArr(target, data.Length * 4, data, usage);
			}
		}

		public static void BufferData(uint target, float[] data, int floatCount, uint usage)
		{
			if (_bufferDataArr != null && data != null)
			{
				int n = ((floatCount < 0) ? 0 : ((floatCount > data.Length) ? data.Length : floatCount));
				_bufferDataArr(target, n * 4, data, usage);
			}
		}

		public static void DeleteBuffer(ref int buffer)
		{
			if (buffer != 0 && _deleteBuffers != null)
			{
				int[] buffers = new int[1]
				{
					buffer
				};
				_deleteBuffers(1, buffers);
				buffer = 0;
			}
		}
	}
}
