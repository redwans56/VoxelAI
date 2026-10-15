using System;

namespace VoxelAI.Core
{
	public class Noise
	{
		private readonly int[] perm = new int[512];

		public Noise(int seed)
		{
			int[] array = new int[256];
			for (int i = 0; i < 256; i++)
			{
				array[i] = i;
			}
			Rng rng = new Rng(seed * 668265263 + 1442695040);
			for (int num = 255; num > 0; num--)
			{
				int num2 = rng.Next(num + 1);
				int num3 = array[num];
				array[num] = array[num2];
				array[num2] = num3;
			}
			for (int j = 0; j < 512; j++)
			{
				perm[j] = array[j & 0xFF];
			}
		}

		private static float Fade(float t)
		{
			return t * t * t * (t * (t * 6f - 15f) + 10f);
		}

		private static float Lerp(float a, float b, float t)
		{
			return a + t * (b - a);
		}

		private static float Grad(int hash, float x, float y)
		{
			switch (hash & 7)
			{
			case 0:
				return x + y;
			case 1:
				return x - y;
			case 2:
				return 0f - x + y;
			case 3:
				return 0f - x - y;
			case 4:
				return x;
			case 5:
				return 0f - x;
			case 6:
				return y;
			default:
				return 0f - y;
			}
		}

		private static float Grad(int hash, float x, float y, float z)
		{
			switch (hash & 0xF)
			{
			case 0:
				return x + y;
			case 1:
				return 0f - x + y;
			case 2:
				return x - y;
			case 3:
				return 0f - x - y;
			case 4:
				return x + z;
			case 5:
				return 0f - x + z;
			case 6:
				return x - z;
			case 7:
				return 0f - x - z;
			case 8:
				return y + z;
			case 9:
				return 0f - y + z;
			case 10:
				return y - z;
			case 11:
				return 0f - y - z;
			case 12:
				return y + x;
			case 13:
				return 0f - y + z;
			case 14:
				return y - x;
			default:
				return 0f - y - z;
			}
		}

		public float Perlin2(float x, float y)
		{
			int num = (int)Math.Floor(x) & 0xFF;
			int num2 = (int)Math.Floor(y) & 0xFF;
			x -= (float)Math.Floor(x);
			y -= (float)Math.Floor(y);
			float t = Fade(x);
			float t2 = Fade(y);
			int hash = perm[perm[num] + num2];
			int hash2 = perm[perm[num] + num2 + 1];
			int hash3 = perm[perm[num + 1] + num2];
			int hash4 = perm[perm[num + 1] + num2 + 1];
			float num3 = Lerp(Lerp(Grad(hash, x, y), Grad(hash3, x - 1f, y), t), Lerp(Grad(hash2, x, y - 1f), Grad(hash4, x - 1f, y - 1f), t), t2);
			return num3 * 1.4f;
		}

		public float Perlin3(float x, float y, float z)
		{
			int num = (int)Math.Floor(x) & 0xFF;
			int num2 = (int)Math.Floor(y) & 0xFF;
			int num3 = (int)Math.Floor(z) & 0xFF;
			x -= (float)Math.Floor(x);
			y -= (float)Math.Floor(y);
			z -= (float)Math.Floor(z);
			float t = Fade(x);
			float t2 = Fade(y);
			float t3 = Fade(z);
			int num4 = perm[num] + num2;
			int num5 = perm[num4] + num3;
			int num6 = perm[num4 + 1] + num3;
			int num7 = perm[num + 1] + num2;
			int num8 = perm[num7] + num3;
			int num9 = perm[num7 + 1] + num3;
			float num10 = Lerp(Lerp(Lerp(Grad(perm[num5], x, y, z), Grad(perm[num8], x - 1f, y, z), t), Lerp(Grad(perm[num6], x, y - 1f, z), Grad(perm[num9], x - 1f, y - 1f, z), t), t2), Lerp(Lerp(Grad(perm[num5 + 1], x, y, z - 1f), Grad(perm[num8 + 1], x - 1f, y, z - 1f), t), Lerp(Grad(perm[num6 + 1], x, y - 1f, z - 1f), Grad(perm[num9 + 1], x - 1f, y - 1f, z - 1f), t), t2), t3);
			return num10 * 1.35f;
		}

		public float Fbm2(float x, float y, int octaves, float lacunarity, float gain)
		{
			float num = 0f;
			float num2 = 1f;
			float num3 = 1f;
			float num4 = 0f;
			for (int i = 0; i < octaves; i++)
			{
				num += Perlin2(x * num3, y * num3) * num2;
				num4 += num2;
				num2 *= gain;
				num3 *= lacunarity;
			}
			return (!(num4 > 0f)) ? 0f : (num / num4);
		}

		public float Fbm3(float x, float y, float z, int octaves, float lacunarity, float gain)
		{
			float num = 0f;
			float num2 = 1f;
			float num3 = 1f;
			float num4 = 0f;
			for (int i = 0; i < octaves; i++)
			{
				num += Perlin3(x * num3, y * num3, z * num3) * num2;
				num4 += num2;
				num2 *= gain;
				num3 *= lacunarity;
			}
			return (!(num4 > 0f)) ? 0f : (num / num4);
		}

		public float Ridged3(float x, float y, float z, int octaves)
		{
			float num = 0f;
			float num2 = 1f;
			float num3 = 1f;
			float num4 = 0f;
			for (int i = 0; i < octaves; i++)
			{
				float num5 = 1f - Math.Abs(Perlin3(x * num3, y * num3, z * num3));
				num += num5 * num5 * num2;
				num4 += num2;
				num2 *= 0.5f;
				num3 *= 2f;
			}
			return (!(num4 > 0f)) ? 0f : (num / num4);
		}
	}
}
