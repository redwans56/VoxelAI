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

		public readonly Dictionary<Vec3i, float> Danger = new Dictionary<Vec3i, float>();

		public readonly List<string> Log = new List<string>(8);

		public float LearningRate = 0.12f;

		private readonly List<Vec3i> decayKeys = new List<Vec3i>();

		private readonly List<float> decayValues = new List<float>();

		public void Reward(Act a, float reward)
		{
			float learningRate = LearningRate;
			Q[(int)a] = Q[(int)a] + learningRate * (reward - Q[(int)a]);
			Tries[(int)a]++;
			if (reward > 0.02f)
			{
				Skill[(int)a] = M.Clamp(Skill[(int)a] + reward * 0.02f, 0f, 1f);
			}
		}

		public void MarkDanger(int x, int y, int z, float amount)
		{
			Vec3i key = new Vec3i(x, y, z);
			float value;
			Danger.TryGetValue(key, out value);
			Danger[key] = M.Clamp(value + amount, 0f, 1f);
		}

		public void DecayDanger(float dt)
		{
			if (Danger.Count == 0)
			{
				return;
			}
			decayKeys.Clear();
			decayValues.Clear();
			foreach (KeyValuePair<Vec3i, float> item in Danger)
			{
				decayKeys.Add(item.Key);
				decayValues.Add(item.Value);
			}
			int count = Danger.Count;
			for (int i = 0; i < decayKeys.Count; i++)
			{
				float num = decayValues[i] - dt * 0.006f;
				if (num <= 0.02f)
				{
					Danger.Remove(decayKeys[i]);
				}
				else
				{
					Danger[decayKeys[i]] = num;
				}
			}
			if (Danger.Count != count && count <= 0)
			{
			}
		}

		public float DangerAt(int x, int y, int z)
		{
			float num = 0f;
			for (int i = -1; i <= 1; i++)
			{
				for (int j = -1; j <= 1; j++)
				{
					for (int k = -1; k <= 1; k++)
					{
						float value;
						if (Danger.TryGetValue(new Vec3i(x + i, y + j, z + k), out value) && value > num)
						{
							num = value;
						}
					}
				}
			}
			return num;
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
			}
			child.SkillMining = SkillMining * (fraction + 0.2f);
			child.SkillBuilding = SkillBuilding * (fraction + 0.2f);
			child.SkillCombat = SkillCombat * (fraction + 0.2f);
			child.SkillNavigation = SkillNavigation * (fraction + 0.2f);
			child.SkillCrafting = SkillCrafting * (fraction + 0.2f);
			child.SkillSurvival = SkillSurvival * (fraction + 0.2f);
		}
	}
}
