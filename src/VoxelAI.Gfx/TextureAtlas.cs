using System;
using VoxelAI.Core;

namespace VoxelAI.Gfx
{
	public class TextureAtlas
	{
		public const int TileSize = 16;

		public const int TilesPerRow = 16;

		public const int AtlasSize = 256;

		private byte[] mosaic = new byte[262144];

		private byte[][] tiles = new byte[34][];

		private readonly Rng rng = new Rng(20260517);

		private int texId;

		private int animFrame;

		private float animTimer;

		private bool mipmapsBuilt;

		public int TextureId
		{
			get
			{
				return texId;
			}
		}

		public TextureAtlas()
		{
			for (int i = 0; i < 34; i++)
			{
				tiles[i] = new byte[1024];
			}
			BuildAllTiles();
			Compose();
		}

		public void Upload()
		{
			if (texId == 0)
			{
				int[] array = new int[1];
				GL.GenTextures(1, array);
				texId = array[0];
			}
			GL.BindTexture(3553u, texId);
			GL.PixelStorei(3317u, 4);
			GL.TexImage2D(3553u, 0, 32856, 256, 256, 0, 6408u, 5121u, mosaic);
			BuildMipChain();
			GL.TexParameteri(3553u, 10240u, 9728);
			GL.TexParameteri(3553u, 10241u, (!mipmapsBuilt) ? 9728 : 9987);
			GL.TexParameteri(3553u, 10242u, 33071);
			GL.TexParameteri(3553u, 10243u, 33071);
			GL.TexEnvi(8960u, 8704u, 8448);
		}

		public void Update(float dt)
		{
			animTimer += dt;
			if (!(animTimer < 0.22f))
			{
				animTimer = 0f;
				animFrame++;
				MakeWaterTile(tiles[14], animFrame);
				MakeLavaTile(tiles[15], animFrame);
				Blit(tiles[14], 14);
				Blit(tiles[15], 15);
				GL.BindTexture(3553u, texId);
				GL.PixelStorei(3317u, 4);
				GL.TexSubImage2D(3553u, 0, TileX(14), TileY(14), 16, 16, 6408u, 5121u, tiles[14]);
				GL.TexSubImage2D(3553u, 0, TileX(15), TileY(15), 16, 16, 6408u, 5121u, tiles[15]);
				BuildMipChain();
			}
		}

		public static void GetUv(int tile, out float u0, out float v0, out float u1, out float v1)
		{
			float num = 0.0625f;
			u0 = (float)(tile % 16) * num + 0.0006f;
			v0 = (float)(tile / 16) * num + 0.0006f;
			u1 = u0 + num - 0.0012f;
			v1 = v0 + num - 0.0012f;
		}

		private static int TileX(int tile)
		{
			return tile % 16 * 16;
		}

		private static int TileY(int tile)
		{
			return tile / 16 * 16;
		}

		private void Compose()
		{
			for (int i = 0; i < 34; i++)
			{
				Blit(tiles[i], i);
			}
		}

		private void Blit(byte[] tile, int index)
		{
			int num = TileX(index);
			int num2 = TileY(index);
			for (int i = 0; i < 16; i++)
			{
				int sourceIndex = i * 16 * 4;
				int destinationIndex = ((num2 + i) * 256 + num) * 4;
				Array.Copy(tile, sourceIndex, mosaic, destinationIndex, 64);
			}
		}

		private void BuildMipChain()
		{
			int num = 256;
			byte[] array = mosaic;
			int num2 = 1;
			mipmapsBuilt = true;
			while (num > 1)
			{
				int num3 = num / 2;
				byte[] array2 = new byte[num3 * num3 * 4];
				for (int i = 0; i < num3; i++)
				{
					for (int j = 0; j < num3; j++)
					{
						int num4 = 0;
						int num5 = 0;
						int num6 = 0;
						int num7 = 0;
						for (int k = 0; k < 2; k++)
						{
							for (int l = 0; l < 2; l++)
							{
								int num8 = ((i * 2 + k) * num + (j * 2 + l)) * 4;
								num4 += array[num8];
								num5 += array[num8 + 1];
								num6 += array[num8 + 2];
								num7 += array[num8 + 3];
							}
						}
						int num9 = (i * num3 + j) * 4;
						array2[num9] = (byte)(num4 >> 2);
						array2[num9 + 1] = (byte)(num5 >> 2);
						array2[num9 + 2] = (byte)(num6 >> 2);
						array2[num9 + 3] = (byte)(num7 >> 2);
					}
				}
				try
				{
					GL.TexImage2D(3553u, num2, 32856, num3, num3, 0, 6408u, 5121u, array2);
				}
				catch
				{
					mipmapsBuilt = false;
				}
				array = array2;
				num = num3;
				num2++;
			}
		}

		private static void Px(byte[] t, int x, int y, int r, int g, int b, int a)
		{
			if (x >= 0 && y >= 0 && x < 16 && y < 16)
			{
				int num = (y * 16 + x) * 4;
				t[num] = (byte)M.Clamp(r, 0, 255);
				t[num + 1] = (byte)M.Clamp(g, 0, 255);
				t[num + 2] = (byte)M.Clamp(b, 0, 255);
				t[num + 3] = (byte)M.Clamp(a, 0, 255);
			}
		}

		private void Fill(byte[] t, int r, int g, int b, int a)
		{
			for (int i = 0; i < 16; i++)
			{
				for (int j = 0; j < 16; j++)
				{
					Px(t, j, i, r, g, b, a);
				}
			}
		}

		private void Speckle(byte[] t, int amount, int r0, int g0, int b0, int variance)
		{
			for (int i = 0; i < amount; i++)
			{
				int x = rng.Next(16);
				int y = rng.Next(16);
				int num = rng.Range(-variance, variance + 1);
				Px(t, x, y, r0 + num, g0 + num, b0 + num, 255);
			}
		}

		private void NoiseOverlay(byte[] t, int variance)
		{
			for (int i = 0; i < 16; i++)
			{
				for (int j = 0; j < 16; j++)
				{
					int num = (i * 16 + j) * 4;
					int num2 = rng.Range(-variance, variance + 1);
					t[num] = (byte)M.Clamp(t[num] + num2, 0, 255);
					t[num + 1] = (byte)M.Clamp(t[num + 1] + num2, 0, 255);
					t[num + 2] = (byte)M.Clamp(t[num + 2] + num2, 0, 255);
				}
			}
		}

		private void BuildAllTiles()
		{
			byte[] t = tiles[0];
			Fill(t, 96, 158, 68, 255);
			NoiseOverlay(t, 26);
			Speckle(t, 60, 108, 172, 74, 24);
			byte[] array = tiles[2];
			Fill(array, 134, 96, 67, 255);
			NoiseOverlay(array, 22);
			Speckle(array, 40, 120, 84, 58, 16);
			byte[] array2 = tiles[1];
			Array.Copy(array, array2, array.Length);
			for (int i = 0; i < 16; i++)
			{
				int num = 3 + rng.Next(3);
				for (int j = 0; j < num; j++)
				{
					Px(array2, i, j, 96 + rng.Range(-12, 13), 158 + rng.Range(-14, 15), 68 + rng.Range(-10, 11), 255);
				}
				if (rng.Chance(0.55f))
				{
					Px(array2, i, num, 88, 146, 62, 255);
				}
			}
			byte[] t2 = tiles[3];
			Fill(t2, 130, 130, 130, 255);
			NoiseOverlay(t2, 16);
			for (int k = 0; k < 5; k++)
			{
				int num2 = rng.Next(12);
				int num3 = rng.Next(12);
				for (int l = 0; l < rng.Range(3, 7); l++)
				{
					Px(t2, num2 + l, num3 + rng.Range(0, 2), 112, 112, 112, 255);
				}
			}
			byte[] t3 = tiles[4];
			Fill(t3, 96, 96, 96, 255);
			int num4 = 4;
			for (int m = 0; m < num4; m++)
			{
				for (int n = 0; n < num4; n++)
				{
					int num5 = n * 4 + 1;
					int num6 = m * 4 + 1;
					int num7 = rng.Range(120, 160);
					for (int num8 = 0; num8 < 3; num8++)
					{
						for (int num9 = 0; num9 < 3; num9++)
						{
							Px(t3, num5 + num9, num6 + num8, num7, num7, num7, 255);
						}
					}
				}
			}
			NoiseOverlay(t3, 10);
			byte[] t4 = tiles[5];
			Fill(t4, 219, 207, 163, 255);
			NoiseOverlay(t4, 12);
			byte[] t5 = tiles[6];
			Fill(t5, 128, 124, 120, 255);
			for (int num10 = 0; num10 < 34; num10++)
			{
				int num11 = rng.Range(96, 168);
				Px(t5, rng.Next(16), rng.Next(16), num11, num11, num11 - 4, 255);
			}
			NoiseOverlay(t5, 14);
			byte[] t6 = tiles[7];
			Fill(t6, 60, 60, 64, 255);
			for (int num12 = 0; num12 < 40; num12++)
			{
				int num13 = rng.Range(30, 96);
				Px(t6, rng.Next(16), rng.Next(16), num13, num13, num13 + 4, 255);
			}
			byte[] t7 = tiles[8];
			Fill(t7, 106, 78, 48, 255);
			for (int num14 = 0; num14 < 16; num14++)
			{
				int num15 = 96 + rng.Range(-18, 20);
				for (int num16 = 0; num16 < 16; num16++)
				{
					if (rng.Chance(0.85f))
					{
						Px(t7, num14, num16, num15, num15 - 20, 30, 255);
					}
				}
			}
			NoiseOverlay(t7, 8);
			byte[] t8 = tiles[9];
			Fill(t8, 150, 118, 74, 255);
			for (int num17 = 0; num17 < 16; num17++)
			{
				for (int num18 = 0; num18 < 16; num18++)
				{
					float num19 = (float)num18 - 7.5f;
					float num20 = (float)num17 - 7.5f;
					float num21 = (float)Math.Sqrt(num19 * num19 + num20 * num20);
					int num22 = (((int)(num21 * 2.2f) % 2 != 0) ? (-14) : 12);
					Px(t8, num18, num17, 150 + num22, 118 + num22, 74 + num22, 255);
				}
			}
			byte[] t9 = tiles[10];
			Fill(t9, 56, 118, 44, 255);
			for (int num23 = 0; num23 < 16; num23++)
			{
				for (int num24 = 0; num24 < 16; num24++)
				{
					if (rng.Chance(0.14f))
					{
						Px(t9, num24, num23, 0, 0, 0, 0);
						continue;
					}
					int num25 = rng.Range(-26, 27);
					Px(t9, num24, num23, 56 + num25, 118 + num25, 44 + num25, 255);
				}
			}
			byte[] t10 = tiles[11];
			Fill(t10, 162, 130, 78, 255);
			for (int num26 = 0; num26 < 16; num26++)
			{
				if (num26 % 4 == 3)
				{
					for (int num27 = 0; num27 < 16; num27++)
					{
						Px(t10, num27, num26, 120, 94, 56, 255);
					}
				}
				else
				{
					NoiseOverlayRow(t10, num26, 12);
				}
			}
			for (int num28 = 0; num28 < 4; num28++)
			{
				int x = rng.Next(16);
				int num29 = num28 * 4;
				for (int num30 = 0; num30 < 4; num30++)
				{
					Px(t10, x, num29 + num30, 130, 102, 62, 255);
				}
			}
			byte[] t11 = tiles[12];
			Fill(t11, 210, 235, 245, 0);
			for (int num31 = 0; num31 < 16; num31++)
			{
				Px(t11, num31, 0, 226, 244, 250, 190);
				Px(t11, num31, 15, 226, 244, 250, 190);
			}
			for (int num32 = 0; num32 < 16; num32++)
			{
				Px(t11, 0, num32, 226, 244, 250, 190);
				Px(t11, 15, num32, 226, 244, 250, 190);
			}
			for (int num33 = 0; num33 < 10; num33++)
			{
				int num34 = rng.Range(2, 13);
				int y = rng.Range(2, 13);
				Px(t11, num34, y, 255, 255, 255, 150);
				if (rng.Chance(0.5f))
				{
					Px(t11, num34 + 1, y, 255, 255, 255, 110);
				}
			}
			byte[] t12 = tiles[13];
			Fill(t12, 150, 76, 60, 255);
			for (int num35 = 0; num35 < 16; num35++)
			{
				bool flag = num35 % 4 == 3;
				for (int num36 = 0; num36 < 16; num36++)
				{
					int num37 = num35 / 4 % 2 * 4;
					bool flag2 = (num36 + num37) % 8 == 7;
					if (flag || flag2)
					{
						Px(t12, num36, num35, 190, 186, 180, 255);
					}
					else
					{
						Px(t12, num36, num35, 150 + rng.Range(-14, 15), 76 + rng.Range(-10, 11), 60 + rng.Range(-8, 9), 255);
					}
				}
			}
			MakeWaterTile(tiles[14], 0);
			MakeLavaTile(tiles[15], 0);
			byte[] t13 = tiles[16];
			Fill(t13, 244, 248, 252, 255);
			NoiseOverlay(t13, 7);
			byte[] t14 = tiles[17];
			Fill(t14, 58, 122, 58, 255);
			for (int num38 = 0; num38 < 16; num38++)
			{
				int num39 = ((num38 % 5 == 0) ? (-22) : ((num38 % 5 == 2) ? 14 : 0));
				for (int num40 = 0; num40 < 16; num40++)
				{
					Px(t14, num38, num40, 58 + num39, 122 + num39, 58 + num39, 255);
				}
			}
			for (int num41 = 1; num41 < 16; num41 += 4)
			{
				for (int num42 = 2; num42 < 16; num42 += 5)
				{
					Px(t14, num42, num41, 226, 232, 200, 255);
				}
			}
			byte[] t15 = tiles[18];
			Fill(t15, 68, 132, 62, 255);
			NoiseOverlay(t15, 12);
			for (int num43 = 4; num43 < 12; num43++)
			{
				for (int num44 = 4; num44 < 12; num44++)
				{
					Px(t15, num44, num43, 92, 152, 78, 255);
				}
			}
			MakeOre(tiles[19], 24, 24, 26, 4);
			MakeOre(tiles[20], 198, 156, 118, 4);
			MakeOre(tiles[21], 236, 200, 68, 4);
			MakeOre(tiles[22], 98, 226, 224, 4);
			byte[] t16 = tiles[23];
			Fill(t16, 156, 124, 74, 255);
			for (int num45 = 0; num45 < 16; num45++)
			{
				for (int num46 = 0; num46 < 16; num46++)
				{
					if (num46 % 5 == 4 || num45 % 5 == 4)
					{
						Px(t16, num46, num45, 96, 72, 40, 255);
					}
					else
					{
						Px(t16, num46, num45, 156 + rng.Range(-10, 11), 124 + rng.Range(-8, 9), 74 + rng.Range(-6, 7), 255);
					}
				}
			}
			byte[] t17 = tiles[24];
			Fill(t17, 148, 118, 70, 255);
			for (int num47 = 0; num47 < 16; num47++)
			{
				for (int num48 = 0; num48 < 16; num48++)
				{
					if (num47 < 4)
					{
						Px(t17, num48, num47, 120, 94, 54, 255);
					}
					else if (num47 % 5 == 0)
					{
						Px(t17, num48, num47, 118, 92, 52, 255);
					}
					else
					{
						Px(t17, num48, num47, 148 + rng.Range(-10, 11), 118 + rng.Range(-8, 9), 70 + rng.Range(-6, 7), 255);
					}
				}
			}
			for (int num49 = 5; num49 < 12; num49++)
			{
				for (int num50 = 3; num50 < 8; num50++)
				{
					Px(t17, num50, num49, 106, 106, 106, 255);
				}
			}
			byte[] t18 = tiles[25];
			Fill(t18, 116, 116, 116, 255);
			NoiseOverlay(t18, 12);
			for (int num51 = 6; num51 < 14; num51++)
			{
				for (int num52 = 4; num52 < 12; num52++)
				{
					Px(t18, num52, num51, 42, 38, 36, 255);
				}
			}
			for (int num53 = 5; num53 < 11; num53++)
			{
				Px(t18, num53, 13, 236, 150, 48, 255);
			}
			for (int num54 = 5; num54 < 11; num54++)
			{
				Px(t18, num54, 12, 200, 96, 30, 255);
			}
			byte[] t19 = tiles[26];
			Fill(t19, 122, 122, 122, 255);
			NoiseOverlay(t19, 12);
			for (int num55 = 0; num55 < 6; num55++)
			{
				Px(t19, rng.Next(16), rng.Next(16), 96, 96, 96, 255);
			}
			byte[] t20 = tiles[27];
			Fill(t20, 126, 126, 126, 255);
			NoiseOverlay(t20, 10);
			for (int num56 = 6; num56 < 10; num56++)
			{
				for (int num57 = 6; num57 < 10; num57++)
				{
					Px(t20, num57, num56, 70, 70, 70, 255);
				}
			}
			byte[] t21 = tiles[28];
			Fill(t21, 190, 148, 70, 255);
			for (int num58 = 0; num58 < 26; num58++)
			{
				int x2 = rng.Next(16);
				int y2 = rng.Next(16);
				int num59 = rng.Range(0, 50);
				Px(t21, x2, y2, 250, 216 + num59 / 3, 120 + num59, 255);
			}
			byte[] array3 = tiles[30];
			Fill(array3, 255, 255, 255, 0);
			for (int num60 = 0; num60 < 7; num60++)
			{
				int num61 = rng.Range(2, 14);
				int num62 = rng.Range(2, 14);
				int num63 = rng.Range(2, 5);
				for (int num64 = num62 - num63; num64 <= num62 + num63; num64++)
				{
					for (int num65 = num61 - num63; num65 <= num61 + num63; num65++)
					{
						float num66 = (num65 - num61) * (num65 - num61) + (num64 - num62) * (num64 - num62);
						if (num66 > (float)(num63 * num63))
						{
							continue;
						}
						int num67 = (int)(215f * (1f - num66 / (float)(num63 * num63)));
						if (num67 >= 12)
						{
							int num68 = (M.Mod(num64, 16) * 16 + M.Mod(num65, 16)) * 4;
							if (num67 > array3[num68 + 3])
							{
								array3[num68] = 252;
								array3[num68 + 1] = 252;
								array3[num68 + 2] = byte.MaxValue;
								array3[num68 + 3] = (byte)num67;
							}
						}
					}
				}
			}
			byte[] t22 = tiles[31];
			byte[] array4 = tiles[32];
			for (int num69 = 0; num69 < 16; num69++)
			{
				for (int num70 = 0; num70 < 16; num70++)
				{
					float num71 = (float)num70 - 7.5f;
					float num72 = (float)num69 - 7.5f;
					float num73 = (float)Math.Sqrt(num71 * num71 + num72 * num72);
					if (num73 < 6.4f)
					{
						Px(t22, num70, num69, 255, 246, 200, 255);
					}
					else if (num73 < 8f)
					{
						Px(t22, num70, num69, 255, 236, 170, (int)(150f * (8f - num73) / 1.6f));
					}
					else
					{
						Px(t22, num70, num69, 255, 255, 255, 0);
					}
					if (num73 < 5.6f)
					{
						Px(array4, num70, num69, 226, 232, 244, 250);
					}
					else if (num73 < 6.6f)
					{
						Px(array4, num70, num69, 210, 218, 234, (int)(140f * (6.6f - num73)));
					}
					else
					{
						Px(array4, num70, num69, 255, 255, 255, 0);
					}
					int num74 = (num69 * 16 + num70) * 4;
					array4[num74 + 3] = (byte)M.Clamp(array4[num74 + 3], 0, 255);
				}
			}
			for (int num75 = 0; num75 < 6; num75++)
			{
				int num76 = rng.Range(3, 13);
				int num77 = rng.Range(3, 13);
				for (int num78 = 0; num78 < 2; num78++)
				{
					for (int num79 = 0; num79 < 2; num79++)
					{
						Px(array4, num76 + num79, num77 + num78, 196, 204, 222, 240);
					}
				}
			}
			byte[] t23 = tiles[33];
			for (int num80 = 0; num80 < 16; num80++)
			{
				for (int num81 = 0; num81 < 16; num81++)
				{
					float num82 = (float)num81 - 7.5f;
					float num83 = (float)num80 - 7.5f;
					float num84 = (float)Math.Sqrt(num82 * num82 + num83 * num83);
					int a = ((num84 < 7f) ? ((int)(255f * (1f - num84 / 7f))) : 0);
					Px(t23, num81, num80, 255, 255, 255, a);
				}
			}
			byte[] array5 = tiles[29];
			Array.Copy(array, array5, array.Length);
			for (int num85 = 0; num85 < 16; num85++)
			{
				int num86 = 3 + rng.Next(3);
				for (int num87 = 0; num87 < num86; num87++)
				{
					Px(array5, num85, num87, 244, 248, 252, 255);
				}
			}
		}

		private void NoiseOverlayRow(byte[] t, int y, int variance)
		{
			for (int i = 0; i < 16; i++)
			{
				int num = (y * 16 + i) * 4;
				int num2 = rng.Range(-variance, variance + 1);
				t[num] = (byte)M.Clamp(t[num] + num2, 0, 255);
				t[num + 1] = (byte)M.Clamp(t[num + 1] + num2, 0, 255);
				t[num + 2] = (byte)M.Clamp(t[num + 2] + num2, 0, 255);
			}
		}

		private void MakeOre(byte[] t, int r, int g, int b, int blobs)
		{
			Array.Copy(tiles[3], t, 1024);
			for (int i = 0; i < blobs; i++)
			{
				int num = rng.Range(2, 14);
				int num2 = rng.Range(2, 14);
				int num3 = rng.Range(2, 4);
				for (int j = 0; j < num3; j++)
				{
					for (int k = 0; k < num3; k++)
					{
						if (!rng.Chance(0.25f))
						{
							int num4 = rng.Range(-22, 23);
							Px(t, num + k, num2 + j, r + num4, g + num4, b + num4, 255);
						}
					}
				}
			}
		}

		private void MakeWaterTile(byte[] t, int frame)
		{
			for (int i = 0; i < 16; i++)
			{
				for (int j = 0; j < 16; j++)
				{
					float num = (float)Math.Sin((float)j * 0.7f + (float)frame * 0.9f) * 0.5f + (float)Math.Cos((float)i * 0.5f - (float)frame * 0.6f) * 0.5f;
					int num2 = (int)(num * 14f);
					Px(t, j, i, 46 + num2 / 2, 108 + num2, 196 + num2 / 2, 176);
				}
			}
		}

		private void MakeLavaTile(byte[] t, int frame)
		{
			for (int i = 0; i < 16; i++)
			{
				for (int j = 0; j < 16; j++)
				{
					float num = (float)Math.Sin((float)j * 0.55f + (float)frame * 0.65f) * (float)Math.Cos((float)i * 0.45f - (float)frame * 0.5f);
					int num2 = (int)(num * 40f);
					Px(t, j, i, 214 + num2 / 3, 96 + num2, 24 + num2 / 4, 255);
				}
			}
		}
	}
}
