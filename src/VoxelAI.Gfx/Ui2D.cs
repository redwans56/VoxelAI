using System;
using System.Runtime.InteropServices;

namespace VoxelAI.Gfx
{
	public class Ui2D
	{
		private static readonly byte[] Font = new byte[320]
		{
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			95,
			0,
			0,
			0,
			7,
			0,
			7,
			0,
			20,
			127,
			20,
			127,
			20,
			36,
			42,
			127,
			42,
			18,
			35,
			19,
			8,
			100,
			98,
			54,
			73,
			85,
			34,
			80,
			0,
			5,
			3,
			0,
			0,
			0,
			28,
			34,
			65,
			0,
			0,
			65,
			34,
			28,
			0,
			20,
			8,
			62,
			8,
			20,
			8,
			8,
			62,
			8,
			8,
			0,
			80,
			48,
			0,
			0,
			8,
			8,
			8,
			8,
			8,
			0,
			96,
			96,
			0,
			0,
			32,
			16,
			8,
			4,
			2,
			62,
			81,
			73,
			69,
			62,
			0,
			66,
			127,
			64,
			0,
			66,
			97,
			81,
			73,
			70,
			33,
			65,
			69,
			75,
			49,
			24,
			20,
			18,
			127,
			16,
			39,
			69,
			69,
			69,
			57,
			60,
			74,
			73,
			73,
			48,
			1,
			113,
			9,
			5,
			3,
			54,
			73,
			73,
			73,
			54,
			6,
			73,
			73,
			41,
			30,
			0,
			54,
			54,
			0,
			0,
			0,
			86,
			54,
			0,
			0,
			8,
			20,
			34,
			65,
			0,
			20,
			20,
			20,
			20,
			20,
			0,
			65,
			34,
			20,
			8,
			2,
			1,
			81,
			9,
			6,
			50,
			73,
			121,
			65,
			62,
			126,
			17,
			17,
			17,
			126,
			127,
			73,
			73,
			73,
			54,
			62,
			65,
			65,
			65,
			34,
			127,
			65,
			65,
			34,
			28,
			127,
			73,
			73,
			73,
			65,
			127,
			9,
			9,
			9,
			1,
			62,
			65,
			73,
			73,
			122,
			127,
			8,
			8,
			8,
			127,
			0,
			65,
			127,
			65,
			0,
			32,
			64,
			65,
			63,
			1,
			127,
			8,
			20,
			34,
			65,
			127,
			64,
			64,
			64,
			64,
			127,
			2,
			12,
			2,
			127,
			127,
			4,
			8,
			16,
			127,
			62,
			65,
			65,
			65,
			62,
			127,
			9,
			9,
			9,
			6,
			62,
			65,
			81,
			33,
			94,
			127,
			9,
			25,
			41,
			70,
			70,
			73,
			73,
			73,
			49,
			1,
			1,
			127,
			1,
			1,
			63,
			64,
			64,
			64,
			63,
			31,
			32,
			64,
			32,
			31,
			63,
			64,
			56,
			64,
			63,
			99,
			20,
			8,
			20,
			99,
			7,
			8,
			112,
			8,
			7,
			97,
			81,
			73,
			69,
			67,
			0,
			127,
			65,
			65,
			0,
			2,
			4,
			8,
			16,
			32,
			0,
			65,
			65,
			127,
			0,
			4,
			2,
			1,
			2,
			4,
			64,
			64,
			64,
			64,
			64
		};

		private const int GlyphsPerRow = 8;

		private const int Cell = 8;

		private const int FontTexSize = 64;

		private const float Advance = 6f;

		private float[] color = new float[73728];

		private int colorVerts;

		private float[] font = new float[98304];

		private int fontVerts;

		private float[] atlas = new float[49152];

		private int atlasVerts;

		public int Width = 1280;

		public int Height = 720;

		public int TextureAtlasId;

		public int FontTextureId
		{
			get;
			private set;
		}

		public void BuildFontTexture()
		{
			byte[] array = new byte[16384];
			for (int i = 0; i < 64; i++)
			{
				int num = i % 8 * 8;
				int num2 = i / 8 * 8;
				for (int j = 0; j < 5; j++)
				{
					byte b = Font[i * 5 + j];
					for (int k = 0; k < 7; k++)
					{
						bool flag = (b & (1 << k)) != 0;
						int num3 = num + 1 + j;
						int num4 = num2 + 1 + k;
						int num5 = (num4 * 64 + num3) * 4;
						array[num5] = byte.MaxValue;
						array[num5 + 1] = byte.MaxValue;
						array[num5 + 2] = byte.MaxValue;
						array[num5 + 3] = (byte)(flag ? byte.MaxValue : 0);
					}
				}
			}
			int[] array2 = new int[1];
			GL.GenTextures(1, array2);
			FontTextureId = array2[0];
			GL.BindTexture(3553u, FontTextureId);
			GL.PixelStorei(3317u, 4);
			GL.TexImage2D(3553u, 0, 32856, 64, 64, 0, 6408u, 5121u, array);
			GL.TexParameteri(3553u, 10240u, 9729);
			GL.TexParameteri(3553u, 10241u, 9729);
			GL.TexParameteri(3553u, 10242u, 33071);
			GL.TexParameteri(3553u, 10243u, 33071);
		}

		public void Begin(int width, int height)
		{
			Width = Math.Max(1, width);
			Height = Math.Max(1, height);
			colorVerts = (fontVerts = (atlasVerts = 0));
		}

		private void EnsureColor(int verts)
		{
			if (colorVerts + verts * 6 > color.Length)
			{
				float[] destinationArray = new float[Math.Max(color.Length * 2, colorVerts * 6 + verts * 6 + 4096)];
				Array.Copy(color, destinationArray, colorVerts * 6);
				color = destinationArray;
			}
		}

		private void EnsureFont(int verts)
		{
			if (fontVerts + verts * 8 > font.Length)
			{
				float[] destinationArray = new float[Math.Max(font.Length * 2, fontVerts * 8 + verts * 8 + 4096)];
				Array.Copy(font, destinationArray, fontVerts * 8);
				font = destinationArray;
			}
		}

		private void EnsureAtlas(int verts)
		{
			if (atlasVerts + verts * 8 > atlas.Length)
			{
				float[] destinationArray = new float[Math.Max(atlas.Length * 2, atlasVerts * 8 + verts * 8 + 4096)];
				Array.Copy(atlas, destinationArray, atlasVerts * 8);
				atlas = destinationArray;
			}
		}

		private void PushColorVertex(float x, float y, float r, float g, float b, float a)
		{
			int num = colorVerts * 6;
			color[num] = x;
			color[num + 1] = y;
			color[num + 2] = r;
			color[num + 3] = g;
			color[num + 4] = b;
			color[num + 5] = a;
			colorVerts++;
		}

		public void Rect(float x, float y, float w, float h, float r, float g, float b, float a)
		{
			EnsureColor(6);
			PushColorVertex(x, y + h, r, g, b, a);
			PushColorVertex(x + w, y + h, r, g, b, a);
			PushColorVertex(x + w, y, r, g, b, a);
			PushColorVertex(x, y + h, r, g, b, a);
			PushColorVertex(x + w, y, r, g, b, a);
			PushColorVertex(x, y, r, g, b, a);
		}

		public void GradientQuad(float x, float y, float w, float h, float rt, float gt, float bt, float rb2, float gb2, float bb2, float a)
		{
			EnsureColor(6);
			PushColorVertex(x, y + h, rb2, gb2, bb2, a);
			PushColorVertex(x + w, y + h, rb2, gb2, bb2, a);
			PushColorVertex(x + w, y, rt, gt, bt, a);
			PushColorVertex(x, y + h, rb2, gb2, bb2, a);
			PushColorVertex(x + w, y, rt, gt, bt, a);
			PushColorVertex(x, y, rt, gt, bt, a);
		}

		public void Outline(float x, float y, float w, float h, float r, float g, float b, float a, float t)
		{
			Rect(x, y, w, t, r, g, b, a);
			Rect(x, y + h - t, w, t, r, g, b, a);
			Rect(x, y, t, h, r, g, b, a);
			Rect(x + w - t, y, t, h, r, g, b, a);
		}

		public void TileRect(float x, float y, float w, float h, int tile, float r, float g, float b, float a)
		{
			float u;
			float v;
			float u2;
			float v2;
			TextureAtlas.GetUv(tile, out u, out v, out u2, out v2);
			EnsureAtlas(6);
			PushAtlas(x, y + h, u, v2, r, g, b, a);
			PushAtlas(x + w, y + h, u2, v2, r, g, b, a);
			PushAtlas(x + w, y, u2, v, r, g, b, a);
			PushAtlas(x, y + h, u, v2, r, g, b, a);
			PushAtlas(x + w, y, u2, v, r, g, b, a);
			PushAtlas(x, y, u, v, r, g, b, a);
		}

		private void PushAtlas(float x, float y, float u, float v, float r, float g, float b, float a)
		{
			int num = atlasVerts * 8;
			atlas[num] = x;
			atlas[num + 1] = y;
			atlas[num + 2] = u;
			atlas[num + 3] = v;
			atlas[num + 4] = r;
			atlas[num + 5] = g;
			atlas[num + 6] = b;
			atlas[num + 7] = a;
			atlasVerts++;
		}

		private static int GlyphIndex(char c)
		{
			char c2 = char.ToUpperInvariant(c);
			if (c2 < ' ' || c2 > '_')
			{
				c2 = '?';
			}
			return c2 - 32;
		}

		public float TextWidth(string s, float scale)
		{
			if (string.IsNullOrEmpty(s))
			{
				return 0f;
			}
			return (float)s.Length * 6f * scale;
		}

		public void Text(float x, float y, string s, float scale, float r, float g, float b, float a)
		{
			if (string.IsNullOrEmpty(s))
			{
				return;
			}
			float num = 6f * scale;
			float num2 = x;
			EnsureFont(s.Length * 6);
			foreach (char c in s)
			{
				if (c == ' ')
				{
					num2 += num;
					continue;
				}
				int num3 = GlyphIndex(c);
				int num4 = num3 % 8 * 8;
				int num5 = num3 / 8 * 8;
				float u = (float)(num4 + 1) / 64f;
				float v = (float)(num5 + 1) / 64f;
				float u2 = (float)(num4 + 7) / 64f;
				float v2 = (float)(num5 + 8) / 64f;
				float num6 = 6f * scale;
				float num7 = 7f * scale;
				PushFont(num2, y + num7, u, v2, r, g, b, a);
				PushFont(num2 + num6, y + num7, u2, v2, r, g, b, a);
				PushFont(num2 + num6, y, u2, v, r, g, b, a);
				PushFont(num2, y + num7, u, v2, r, g, b, a);
				PushFont(num2 + num6, y, u2, v, r, g, b, a);
				PushFont(num2, y, u, v, r, g, b, a);
				num2 += num;
			}
		}

		public void TextShadow(float x, float y, string s, float scale, float r, float g, float b, float a)
		{
			Text(x + scale, y + scale, s, scale, 0f, 0f, 0f, a * 0.65f);
			Text(x, y, s, scale, r, g, b, a);
		}

		public void TextCentered(float cx, float y, string s, float scale, float r, float g, float b, float a)
		{
			Text(cx - TextWidth(s, scale) * 0.5f, y, s, scale, r, g, b, a);
		}

		private void PushFont(float x, float y, float u, float v, float r, float g, float b, float a)
		{
			int num = fontVerts * 8;
			font[num] = x;
			font[num + 1] = y;
			font[num + 2] = u;
			font[num + 3] = v;
			font[num + 4] = r;
			font[num + 5] = g;
			font[num + 6] = b;
			font[num + 7] = a;
			fontVerts++;
		}

		public void End()
		{
			GL.MatrixMode(5889u);
			GL.PushMatrix();
			GL.LoadIdentity();
			GL.Ortho(0.0, Width, Height, 0.0, -1.0, 1.0);
			GL.MatrixMode(5888u);
			GL.PushMatrix();
			GL.LoadIdentity();
			GL.Disable(2929u);
			GL.Disable(2884u);
			GL.Enable(3042u);
			GL.BlendFunc(770u, 771u);
			GL.Enable(3553u);
			if (colorVerts > 0)
			{
				GL.Disable(3553u);
				GL.EnableClientState(32884u);
				GL.EnableClientState(32886u);
				GL.DisableClientState(32888u);
				DrawClient(color, colorVerts, 6, 6, false);
				GL.DisableClientState(32886u);
				GL.DisableClientState(32884u);
				GL.Enable(3553u);
			}
			if (atlasVerts > 0 && TextureAtlasId != 0)
			{
				GL.BindTexture(3553u, TextureAtlasId);
				DrawClient(atlas, atlasVerts, 8, 8, true);
			}
			if (fontVerts > 0 && FontTextureId != 0)
			{
				GL.BindTexture(3553u, FontTextureId);
				DrawClient(font, fontVerts, 8, 8, true);
			}
			GL.Disable(3553u);
			GL.Disable(3042u);
			GL.Enable(2929u);
			GL.Enable(2884u);
			GL.MatrixMode(5889u);
			GL.PopMatrix();
			GL.MatrixMode(5888u);
			GL.PopMatrix();
		}

		private void DrawClient(float[] data, int verts, int stride, int texOffset, bool textured)
		{
			GCHandle gCHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
			try
			{
				IntPtr pointer = gCHandle.AddrOfPinnedObject();
				GL.EnableClientState(32884u);
				GL.VertexPointer(2, 5126u, stride * 4, pointer);
				if (textured)
				{
					GL.EnableClientState(32888u);
					GL.TexCoordPointer(2, 5126u, stride * 4, (IntPtr)(pointer.ToInt64() + 8));
					GL.EnableClientState(32886u);
					GL.ColorPointer(4, 5126u, stride * 4, (IntPtr)(pointer.ToInt64() + 16));
				}
				else
				{
					GL.DisableClientState(32888u);
					GL.EnableClientState(32886u);
					GL.ColorPointer(4, 5126u, stride * 4, (IntPtr)(pointer.ToInt64() + 8));
				}
				GL.DrawArrays(4u, 0, verts);
			}
			finally
			{
				GL.DisableClientState(32886u);
				GL.DisableClientState(32888u);
				GL.DisableClientState(32884u);
				gCHandle.Free();
			}
		}
	}
}
