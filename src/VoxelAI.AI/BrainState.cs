using System.Collections.Generic;
using VoxelAI.Core;

namespace VoxelAI.AI
{
	public class BrainState
	{
		public const int ActionCount = 14;

		public float[] Q = new float[14];

		public int[] Tries = new int[14];

		public float[] Skill = new float[14];

		public float SkillMining;

		public float SkillBuilding;

		public float SkillCombat;

		public float SkillNavigation;

		public float SkillCrafting;

		public float SkillSurvival;

		public float LearningRate = 0.12f;

		public float Discount = 0.55f;

		private readonly Dictionary<Vec3i, float> dangerField = new Dictionary<Vec3i, float>();

		public readonly List<string> Log = new List<string>(8);

		private readonly List<Vec3i> decayKeys = new List<Vec3i>(256);

		private readonly List<float> decayValues = new List<float>(256);

		private const int DangerCellSize = 4;

		private const int MaxDangerCells = 6000;

		public int DangerCellCount
		{
			get
			{
				return dangerField.Count;
			}
		}

		public void Reward(Act a, float reward)
		{
			int i = (int)a;
			float maxNext = 0f;
			for (int j = 0; j < 14; j++)
			{
				if (Q[j] > maxNext)
				{
					maxNext = Q[j];
				}
			}
			float target = reward + Discount * (maxNext - Q[i]);
			Q[i] = M.Clamp(Q[i] + LearningRate * (target - Q[i]), -2f, 4f);
			Tries[i]++;
			if (reward > 0.02f)
			{
				Skill[i] = M.Clamp(Skill[i] + reward * 0.02f, 0f, 1f);
			}
		}

		private static Vec3i DangerKey(int x, int y, int z)
		{
			return new Vec3i(M.FloorDiv(x, DangerCellSize), M.FloorDiv(y, DangerCellSize), M.FloorDiv(z, DangerCellSize));
		}

		public void MarkDanger(int x, int y, int z, float amount)
		{
			if (amount <= 0f)
			{
				return;
			}
			Vec3i center = DangerKey(x, y, z);
			for (int i = -1; i <= 1; i++)
			{
				for (int j = -1; j <= 1; j++)
				{
					for (int k = -1; k <= 1; k++)
					{
						float w = ((i == 0 && j == 0 && k == 0) ? 1f : (((i * i + j * j + k * k) == 1) ? 0.7f : 0.45f));
						Vec3i key = new Vec3i(center.X + i, center.Y + j, center.Z + k);
						float value;
						dangerField.TryGetValue(key, out value);
						float num = M.Clamp(value + amount * w, 0f, 1f);
						if (num > value)
						{
							dangerField[key] = num;
						}
					}
				}
			}
			if (dangerField.Count > MaxDangerCells)
			{
				PruneDanger();
			}
		}

		private void PruneDanger()
		{
			decayKeys.Clear();
			foreach (KeyValuePair<Vec3i, float> item in dangerField)
			{
				if (item.Value < 0.22f)
				{
					decayKeys.Add(item.Key);
				}
			}
			for (int i = 0; i < decayKeys.Count; i++)
			{
				dangerField.Remove(decayKeys[i]);
			}
			if (dangerField.Count > MaxDangerCells)
			{
				dangerField.Clear();
			}
		}

		public void DecayDanger(float dt)
		{
			if (dangerField.Count == 0)
			{
				return;
			}
			decayKeys.Clear();
			decayValues.Clear();
			foreach (KeyValuePair<Vec3i, float> item in dangerField)
			{
				decayKeys.Add(item.Key);
				decayValues.Add(item.Value);
			}
			for (int i = 0; i < decayKeys.Count; i++)
			{
				float num = decayValues[i] - dt * 0.006f;
				if (num <= 0.02f)
				{
					dangerField.Remove(decayKeys[i]);
				}
				else
				{
					dangerField[decayKeys[i]] = num;
				}
			}
		}

		public float DangerAt(int x, int y, int z)
		{
			if (dangerField.Count == 0)
			{
				return 0f;
			}
			float value;
			if (dangerField.TryGetValue(DangerKey(x, y, z), out value))
			{
				return value;
			}
			return 0f;
		}

		public void Note(string s)
		{
			Log.Add(s);
			if (Log.Count > 6)
			{
				Log.RemoveAt(0);
			}
		}

		public void InheritInto(BrainState child, float fraction, Rng r)
		{
			for (int i = 0; i < 14; i++)
			{
				child.Q[i] = Q[i] * fraction + (1f - fraction) * 0.1f + r.RangeF(-0.02f, 0.02f);
				child.Skill[i] = Skill[i] * (fraction + 0.2f);
				child.Tries[i] = Tries[i] / 2;
			}
			child.SkillMining = SkillMining * (fraction + 0.2f);
			child.SkillBuilding = SkillBuilding * (fraction + 0.2f);
			child.SkillCombat = SkillCombat * (fraction + 0.2f);
			child.SkillNavigation = SkillNavigation * (fraction + 0.2f);
			child.SkillCrafting = SkillCrafting * (fraction + 0.2f);
			child.SkillSurvival = SkillSurvival * (fraction + 0.2f);
			child.LearningRate = LearningRate;
			child.Discount = Discount;
		}
	}
}
