using System;
using System.Text;
using VoxelAI.Core;

namespace VoxelAI.AI
{
	public class Genome
	{
		public float[] V = new float[12];

		public int LineageId;

		public int Generations;

		public float this[int gene]
		{
			get
			{
				return V[gene];
			}
		}

		public static Genome RandomGenome(Rng r)
		{
			Genome genome = new Genome();
			for (int i = 0; i < 12; i++)
			{
				genome.V[i] = r.NextFloat();
			}
			genome.LineageId = r.Next(100000);
			return genome;
		}

		public Genome Clone()
		{
			Genome genome = new Genome();
			Array.Copy(V, genome.V, 12);
			genome.LineageId = LineageId;
			genome.Generations = Generations;
			return genome;
		}

		public static Genome Crossover(Genome a, Genome b, Rng r)
		{
			Genome genome = new Genome();
			for (int i = 0; i < 12; i++)
			{
				float num = r.NextFloat() * 0.5f + 0.25f;
				genome.V[i] = a.V[i] * num + b.V[i] * (1f - num);
			}
			genome.LineageId = ((!(r.NextFloat() < 0.5f)) ? b.LineageId : a.LineageId);
			genome.Generations = Math.Max(a.Generations, b.Generations) + 1;
			return genome;
		}

		public void Mutate(Rng r, float rate, float strength)
		{
			for (int i = 0; i < 12; i++)
			{
				if (!(r.NextFloat() > rate))
				{
					float num = r.Gaussian() * strength;
					if (r.Chance(0.12f))
					{
						num = r.RangeF(-0.45f, 0.45f);
					}
					V[i] = M.Clamp(V[i] + num, 0f, 1f);
				}
			}
		}

		public string Describe()
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (V[0] > 0.66f)
			{
				stringBuilder.Append("Aggressive ");
			}
			else if (V[0] < 0.33f)
			{
				stringBuilder.Append("Peaceful ");
			}
			if (V[7] > 0.6f)
			{
				stringBuilder.Append("Builder ");
			}
			if (V[6] > 0.6f)
			{
				stringBuilder.Append("Miner ");
			}
			if (V[3] > 0.65f)
			{
				stringBuilder.Append("Hoarder ");
			}
			if (V[1] > 0.65f)
			{
				stringBuilder.Append("Explorer ");
			}
			if (V[4] > 0.65f)
			{
				stringBuilder.Append("Cautious ");
			}
			if (V[2] > 0.65f)
			{
				stringBuilder.Append("Social ");
			}
			if (stringBuilder.Length == 0)
			{
				stringBuilder.Append("Balanced ");
			}
			stringBuilder.Append("L").Append(LineageId % 1000);
			return stringBuilder.ToString();
		}
	}
}
