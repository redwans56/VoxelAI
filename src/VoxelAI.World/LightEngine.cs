namespace VoxelAI.World
{
	public class LightEngine
	{
		private const int MaxQueued = 131072;

		private readonly int[] qx = new int[131072];

		private readonly int[] qy = new int[131072];

		private readonly int[] qz = new int[131072];

		private readonly int[] qv = new int[131072];

		private int head;

		private int tail;

		private readonly int[] rx = new int[131072];

		private readonly int[] ry = new int[131072];

		private readonly int[] rz = new int[131072];

		private readonly int[] rv = new int[131072];

		private int rhead;

		private int rtail;

		private static readonly int[] DX = new int[6]
		{
			1,
			-1,
			0,
			0,
			0,
			0
		};

		private static readonly int[] DY = new int[6]
		{
			0,
			0,
			1,
			-1,
			0,
			0
		};

		private static readonly int[] DZ = new int[6]
		{
			0,
			0,
			0,
			0,
			1,
			-1
		};

		public int LastWork;

		public void Push(int x, int y, int z, int v)
		{
			if (tail < 131072 && v > 0)
			{
				qx[tail] = x;
				qy[tail] = y;
				qz[tail] = z;
				qv[tail] = v;
				tail++;
			}
		}

		private void PushRemove(int x, int y, int z, int v)
		{
			if (rtail < 131072)
			{
				rx[rtail] = x;
				ry[rtail] = y;
				rz[rtail] = z;
				rv[rtail] = v;
				rtail++;
			}
		}

		public void ResetQueue()
		{
			head = (tail = (rhead = (rtail = 0)));
			LastWork = 0;
		}

		public void Solve(ILightVolume vol, bool sky)
		{
			int num = 0;
			while (rhead < rtail && ++num <= 600000)
			{
				int num2 = rx[rhead];
				int num3 = ry[rhead];
				int num4 = rz[rhead];
				int num5 = rv[rhead];
				rhead++;
				LastWork++;
				for (int i = 0; i < 6; i++)
				{
					int x = num2 + DX[i];
					int num6 = num3 + DY[i];
					int z = num4 + DZ[i];
					if (num6 < 0 || num6 >= vol.MaxY)
					{
						continue;
					}
					int num7 = ((!sky) ? vol.LightBlockValue(x, num6, z) : vol.LightSky(x, num6, z));
					if (num7 == 0)
					{
						continue;
					}
					if (num7 < num5)
					{
						if (sky)
						{
							vol.LightSetSky(x, num6, z, 0);
						}
						else
						{
							vol.LightSetBlockValue(x, num6, z, 0);
						}
						PushRemove(x, num6, z, num7);
					}
					else
					{
						Push(x, num6, z, num7);
					}
				}
			}
			rhead = (rtail = 0);
			num = 0;
			while (head < tail && ++num <= 600000)
			{
				int num8 = qx[head];
				int num9 = qy[head];
				int num10 = qz[head];
				int num11 = qv[head];
				head++;
				LastWork++;
				for (int j = 0; j < 6; j++)
				{
					int x2 = num8 + DX[j];
					int num12 = num9 + DY[j];
					int z2 = num10 + DZ[j];
					if (num12 < 0 || num12 >= vol.MaxY || vol.LightIsOpaque(x2, num12, z2))
					{
						continue;
					}
					int num13 = num11 - 1;
					if (sky && j == 3 && num11 == 15)
					{
						num13 = 15;
					}
					if (sky && DY[j] == -1 && num11 == 15)
					{
						num13 = 15;
					}
					if (num13 <= 0)
					{
						continue;
					}
					byte b = ((!sky) ? vol.LightBlockValue(x2, num12, z2) : vol.LightSky(x2, num12, z2));
					if (b < num13)
					{
						if (sky)
						{
							vol.LightSetSky(x2, num12, z2, (byte)num13);
						}
						else
						{
							vol.LightSetBlockValue(x2, num12, z2, (byte)num13);
						}
						Push(x2, num12, z2, num13);
					}
				}
			}
			head = (tail = 0);
		}

		public void RemoveSky(ILightVolume vol, int x, int y, int z, int oldLevel)
		{
			ResetQueue();
			if (oldLevel > 0)
			{
				vol.LightSetSky(x, y, z, 0);
				PushRemove(x, y, z, oldLevel);
				Solve(vol, true);
			}
		}

		public void RemoveBlock(ILightVolume vol, int x, int y, int z, int oldLevel)
		{
			ResetQueue();
			if (oldLevel > 0)
			{
				vol.LightSetBlockValue(x, y, z, 0);
				PushRemove(x, y, z, oldLevel);
				Solve(vol, false);
			}
		}

		public void SeedSky(ILightVolume vol, int x, int y, int z, int level)
		{
			if (level > 0 && y >= 0 && y < vol.MaxY && !vol.LightIsOpaque(x, y, z) && vol.LightSky(x, y, z) < level)
			{
				vol.LightSetSky(x, y, z, (byte)level);
				Push(x, y, z, level);
			}
		}

		public void SeedBlock(ILightVolume vol, int x, int y, int z, int level)
		{
			if (level > 0 && y >= 0 && y < vol.MaxY && !vol.LightIsOpaque(x, y, z) && vol.LightBlockValue(x, y, z) < level)
			{
				vol.LightSetBlockValue(x, y, z, (byte)level);
				Push(x, y, z, level);
			}
		}
	}
}
