using System;
using System.Globalization;

namespace VoxelAI.AI
{
	public class SpeciesMemory
	{
		public float LavaMargin = 2.2f;

		public float WaterCaution = 0.2f;

		public float CliffCaution = 1.5f;

		public float NightShelter;

		public float CombatThreshold = 0.55f;

		public float ExplorationRadius = 48f;

		public int DeathsLava;

		public int DeathsFall;

		public int DeathsDrown;

		public int DeathsCombat;

		public int DeathsSuffocation;

		public int DeathsOther;

		public int Generation;

		public int Births;

		public int BestFitness;

		public void RecordDeath(string cause)
		{
			switch (cause)
			{
			case "lava":
				DeathsLava++;
				break;
			case "fall":
				DeathsFall++;
				break;
			case "water":
				DeathsDrown++;
				break;
			case "combat":
				DeathsCombat++;
				break;
			case "crush":
				DeathsSuffocation++;
				break;
			default:
				DeathsOther++;
				break;
			}
			int num = DeathsLava + DeathsFall + DeathsDrown + DeathsCombat + DeathsSuffocation + DeathsOther;
			if (num > 0)
			{
				if (DeathsLava > 0 && LavaMargin < 6f && (float)DeathsLava / (float)num > 0.25f)
				{
					LavaMargin = Math.Min(6f, LavaMargin + 0.15f);
				}
				if (DeathsDrown > 0 && WaterCaution < 1f && (float)DeathsDrown / (float)num > 0.2f)
				{
					WaterCaution = Math.Min(1f, WaterCaution + 0.04f);
				}
				if (DeathsFall > 0 && CliffCaution < 4f && (float)DeathsFall / (float)num > 0.2f)
				{
					CliffCaution = Math.Min(4f, CliffCaution + 0.2f);
				}
				if (DeathsCombat > 0 && CombatThreshold < 0.9f && (float)DeathsCombat / (float)num > 0.25f)
				{
					CombatThreshold = Math.Min(0.9f, CombatThreshold + 0.03f);
				}
			}
		}

		public string Summary()
		{
			return string.Format(CultureInfo.InvariantCulture, "GEN {0}  births {1}  best fitness {2}  lava margin {3:0.0}  water caution {4:0.00}  cliff {5:0.0}  combat thr {6:0.00}", Generation, Births, BestFitness, LavaMargin, WaterCaution, CliffCaution, CombatThreshold);
		}

		public string DeathSummary()
		{
			return string.Format(CultureInfo.InvariantCulture, "deaths: lava {0} fall {1} drown {2} combat {3} crush {4} other {5}", DeathsLava, DeathsFall, DeathsDrown, DeathsCombat, DeathsSuffocation, DeathsOther);
		}
	}
}
