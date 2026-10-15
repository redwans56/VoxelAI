namespace VoxelAI.Core
{
	public class Rng
	{
		private uint s;

		public Rng(int seed)
		{
			s = (uint)seed;
			if (s == 0)
			{
				s = 2654435769u;
			}
		}

		public uint NextUInt()
		{
			s ^= s << 13;
			s ^= s >> 17;
			s ^= s << 5;
			return s;
		}

		public int Next(int maxExclusive)
		{
			return (int)((maxExclusive > 0) ? (NextUInt() % (uint)maxExclusive) : 0);
		}

		public int Range(int loInclusive, int hiExclusive)
		{
			return loInclusive + Next(hiExclusive - loInclusive);
		}

		public float NextFloat()
		{
			return (float)(NextUInt() >> 8) * 5.96046448E-08f;
		}

		public float RangeF(float lo, float hi)
		{
			return lo + NextFloat() * (hi - lo);
		}

		public bool Chance(float p)
		{
			return NextFloat() < p;
		}

		public float Gaussian()
		{
			float num = 0f;
			for (int i = 0; i < 4; i++)
			{
				num += NextFloat();
			}
			return (num - 2f) * 1.2247f;
		}

		public float NextSign()
		{
			return (!(NextFloat() < 0.5f)) ? 1f : (-1f);
		}
	}
}
