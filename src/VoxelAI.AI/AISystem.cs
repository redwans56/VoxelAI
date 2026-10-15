using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using VoxelAI.Core;
using VoxelAI.Entities;
using VoxelAI.World;

namespace VoxelAI.AI
{
	public class AISystem
	{
		public VoxelAI.World.World World;

		public readonly List<Agent> Agents = new List<Agent>();

		public SpeciesMemory Species = new SpeciesMemory();

		public Rng Rng;

		public int TargetPopulation = 10;

		public float MutationRate = 0.16f;

		public float MutationStrength = 0.13f;

		public float EvolutionPeriod = 200f;

		public float InheritanceFraction = 0.45f;

		public bool EvolutionEnabled = true;

		private float evoTimer = 90f;

		private int nextId = 1;

		private float chronoTimer;

		private readonly Dictionary<byte, List<Vec3i>> discovered = new Dictionary<byte, List<Vec3i>>();

		private readonly HashSet<long> visited = new HashSet<long>();

		public int TotalBirths;

		public int TotalDeaths;

		public int DeathsLava;

		public int DeathsFall;

		public int DeathsDrown;

		public int DeathsCombat;

		public float BestFitnessEver;

		public readonly List<string> Chronicle = new List<string>(64);

		public int VisitedCells
		{
			get
			{
				return visited.Count;
			}
		}

		public AISystem(VoxelAI.World.World w, int seed)
		{
			World = w;
			Rng = new Rng(seed ^ 0x2F1C88);
		}

		public void PopulateAt(Vec3 center, int count)
		{
			TargetPopulation = count;
			for (int i = 0; i < count; i++)
			{
				Vec3 pos = FindSpawnNear(center, i);
				Genome g = Genome.RandomGenome(Rng);
				Agent agent = NewAgent(pos, g);
				Agents.Add(agent);
				if (World != null && World.Creatures != null)
				{
					World.Creatures.Add(agent.Body);
				}
			}
			Log("colony seeded: " + count + " agents, generation 1");
		}

		public Agent NewAgent(Vec3 pos, Genome g)
		{
			Agent agent = new Agent();
			agent.Species = Species;
			agent.G = g;
			agent.Id = nextId++;
			agent.Name = MakeName(agent.Id);
			agent.Generation = Species.Generation + 1;
			agent.AnchorHome = pos;
			Creature creature = new Creature();
			creature.Position = pos;
			creature.Name = agent.Name + " g" + agent.Generation;
			Rng rng = new Rng(agent.Id * 7919 + agent.Generation * 104729);
			float h = rng.NextFloat();
			float r;
			float g2;
			float b;
			Hue(h, out r, out g2, out b);
			creature.PickColors(rng, r, g2, b);
			agent.Body = creature;
			agent.Brain.Q[1] = 0.2f;
			agent.Brain.Q[2] = 0.2f;
			agent.Brain.Q[3] = 0.2f;
			return agent;
		}

		private static void Hue(float h, out float r, out float g, out float b)
		{
			h -= (float)Math.Floor(h);
			float num = h * 6f;
			int num2 = (int)Math.Floor(num);
			float num3 = num - (float)num2;
			float num4 = 1f - num3;
			float num5 = num3;
			switch (num2 % 6)
			{
			case 0:
				r = 0.55f;
				g = 0.25f + 0.6f * num5;
				b = 0.3f;
				break;
			case 1:
				r = 0.25f + 0.6f * num4;
				g = 0.8f;
				b = 0.35f;
				break;
			case 2:
				r = 0.3f;
				g = 0.7f;
				b = 0.4f + 0.5f * num5;
				break;
			case 3:
				r = 0.35f;
				g = 0.45f + 0.4f * num4;
				b = 0.88f;
				break;
			case 4:
				r = 0.6f + 0.35f * num5;
				g = 0.4f;
				b = 0.85f;
				break;
			default:
				r = 0.9f;
				g = 0.55f + 0.3f * num4;
				b = 0.35f;
				break;
			}
		}

		private static string MakeName(int id)
		{
			string[] array = new string[10]
			{
				"NEO",
				"VEX",
				"ORB",
				"ZED",
				"KAI",
				"RHO",
				"MUX",
				"LUX",
				"AXI",
				"TEL"
			};
			return array[id % array.Length] + "-" + (10 + id);
		}

		private Vec3 FindSpawnNear(Vec3 center, int index)
		{
			for (int i = 0; i < 60; i++)
			{
				float num = (float)index * 1.7f + (float)i * 0.9f;
				float num2 = 4f + (float)(i % 7) * 3f;
				int num3 = (int)((double)center.X + Math.Cos(num) * (double)num2);
				int num4 = (int)((double)center.Z + Math.Sin(num) * (double)num2);
				World.EnsureChunkLoaded(M.FloorDiv(num3, 16), M.FloorDiv(num4, 16), true);
				int num5 = World.SurfaceY(num3, num4);
				byte block = World.GetBlock(num3, num5, num4);
				if (block != 0 && block != 12 && block != 13 && (World.GetBlock(num3, num5 + 1, num4) == 0 || World.GetBlock(num3, num5 + 1, num4) == 8))
				{
					return new Vec3((float)num3 + 0.5f, (float)num5 + 1.3f, (float)num4 + 0.5f);
				}
			}
			return center + new Vec3(Rng.RangeF(-3f, 3f), 2f, Rng.RangeF(-3f, 3f));
		}

		public void Update(float dt)
		{
			for (int i = 0; i < Agents.Count; i++)
			{
				Agent agent = Agents[i];
				if (agent.Dead)
				{
					agent.RespawnTimer -= dt;
					if (agent.RespawnTimer <= 0f)
					{
						Reincarnate(agent);
					}
					continue;
				}
				agent.Update(World, this, dt);
				agent.Fitness = agent.SessionScore + agent.Health * 0.05f + agent.Age * 0.05f + (float)agent.MinedTotal * 1.5f + (float)agent.PlacementsTotal * 4f + (float)agent.CraftsTotal * 5f + (float)agent.OresFound * 6f + (float)agent.Kills * 15f + (float)agent.StructuresBuilt * 25f - agent.DamageTaken * 0.4f;
				if (agent.Fitness > BestFitnessEver)
				{
					BestFitnessEver = agent.Fitness;
				}
				NoteVisited(agent.Body.Position);
			}
			for (int j = 0; j < Agents.Count; j++)
			{
				Agent agent2 = Agents[j];
				if (!agent2.Dead)
				{
					if (agent2.Body.Position.Y < 2.5f)
					{
						agent2.Damage(45f * dt, "fall", World);
					}
					if (agent2.Body.OnGround && !agent2.Body.OnGroundPrev && agent2.Body.Velocity.Y < -14f)
					{
						agent2.Damage(Math.Abs(agent2.Body.Velocity.Y) * 1.9f, "fall", World);
					}
				}
			}
			if (EvolutionEnabled)
			{
				evoTimer += dt;
				if (evoTimer > EvolutionPeriod)
				{
					evoTimer = 0f;
					TournamentStep();
				}
			}
			chronoTimer += dt;
			if (chronoTimer > 40f)
			{
				chronoTimer = 0f;
			}
		}

		private void NoteVisited(Vec3 p)
		{
			long item = ((long)(int)Math.Floor(p.X / 4f) << 32) ^ (uint)(int)Math.Floor(p.Z / 4f);
			visited.Add(item);
		}

		public void Damage(Agent target, float amount, Agent from, string cause)
		{
			if (target == null || target.Dead)
			{
				return;
			}
			target.Damage(amount, cause, World);
			if (from != null)
			{
				from.Award(0.25f);
			}
			if (target.Dead)
			{
				if (from != null)
				{
					from.Kills++;
					from.SessionScore += 12f;
					from.Award(1.5f);
				}
				Log(target.Name + " g" + target.Generation + " was killed by " + ((from == null) ? "the world" : from.Name));
			}
		}

		public void NoteResource(byte block, Vec3i pos)
		{
			List<Vec3i> value;
			if (!discovered.TryGetValue(block, out value))
			{
				value = new List<Vec3i>(32);
				discovered[block] = value;
			}
			if (!value.Contains(pos))
			{
				if (value.Count > 64)
				{
					value.RemoveAt(Rng.Next(value.Count));
				}
				value.Add(pos);
			}
		}

		public bool TryGetKnownResource(byte block, Vec3 pos, out Vec3i found)
		{
			found = Vec3i.Zero;
			List<Vec3i> value;
			if (!discovered.TryGetValue(block, out value) || value.Count == 0)
			{
				return false;
			}
			float num = float.MaxValue;
			for (int i = 0; i < value.Count; i++)
			{
				if (World.GetBlock(value[i].X, value[i].Y, value[i].Z) != block)
				{
					value.RemoveAt(i);
					i--;
					continue;
				}
				float num2 = Vec3.DistanceSq(value[i].ToVec3(), pos);
				if (num2 < num)
				{
					num = num2;
					found = value[i];
				}
			}
			return found != Vec3i.Zero;
		}

		private Agent PickParent(float ignoreBelowFitness)
		{
			Agent agent = null;
			for (int i = 0; i < 3; i++)
			{
				Agent agent2 = Agents[Rng.Next(Agents.Count)];
				if (!agent2.Dead && (agent == null || agent2.Fitness > agent.Fitness))
				{
					agent = agent2;
				}
			}
			return agent;
		}

		public void Reincarnate(Agent a)
		{
			Agent agent = PickParent(-1f);
			Agent agent2 = PickParent(-1f);
			Genome genome = ((agent != null && agent2 != null && agent != agent2) ? Genome.Crossover(agent.G, agent2.G, Rng) : ((agent == null) ? Genome.RandomGenome(Rng) : agent.G.Clone()));
			genome.Mutate(Rng, MutationRate, MutationStrength);
			BrainState brainState = new BrainState();
			float num = 0f;
			Agent agent3 = agent;
			for (int i = 0; i < Agents.Count; i++)
			{
				Agent agent4 = Agents[i];
				if (!agent4.Dead && agent4 != a)
				{
					float sessionScore = agent4.SessionScore;
					if (sessionScore > num)
					{
						num = sessionScore;
						agent3 = agent4;
					}
				}
			}
			if (agent3 != null && agent3 != a)
			{
				agent3.Brain.InheritInto(brainState, InheritanceFraction, Rng);
			}
			string text = ((agent == null) ? "unknown" : (agent.Name + " g" + agent.Generation)) + ((agent2 == null || agent2 == agent) ? string.Empty : (" x " + agent2.Name + " g" + agent2.Generation));
			string name = a.Name;
			a.Dead = false;
			a.Body.Alive = true;
			a.Body.Position = FindSpawnNear(a.AnchorHome, a.Id);
			a.Body.Velocity = Vec3.Zero;
			a.Health = 100f;
			a.G = genome;
			a.Brain = brainState;
			a.Generation = Math.Max(a.Generation, Species.Generation + 1);
			a.Action = Act.Idle;
			a.Inventory.Clear();
			a.Plan = null;
			a.SessionScore = 0f;
			a.Age = 0f;
			a.DamageTaken = 0f;
			a.Status = "reborn";
			Rng rng = new Rng(a.Id * 31 + a.Generation * 7919);
			float r;
			float g;
			float b;
			Hue(rng.NextFloat(), out r, out g, out b);
			a.Body.PickColors(rng, r, g, b);
			Species.Generation = Math.Max(Species.Generation, a.Generation - 1);
			TotalBirths++;
			Species.Births = TotalBirths;
			Species.BestFitness = (int)BestFitnessEver;
			Log(name + " reborn as g" + a.Generation + " from " + text + "  [" + genome.Describe() + "]");
		}

		public void TournamentStep()
		{
			if (Agents.Count < 2 || !EvolutionEnabled)
			{
				return;
			}
			Agent agent = null;
			Agent agent2 = null;
			for (int i = 0; i < Agents.Count; i++)
			{
				Agent agent3 = Agents[i];
				if (!agent3.Dead)
				{
					if (agent == null || agent3.Fitness < agent.Fitness)
					{
						agent = agent3;
					}
					if (agent2 == null || agent3.Fitness > agent2.Fitness)
					{
						agent2 = agent3;
					}
				}
			}
			if (agent == null || agent2 == null || agent == agent2)
			{
				return;
			}
			Agent agent4 = null;
			for (int j = 0; j < Agents.Count; j++)
			{
				Agent agent5 = Agents[j];
				if (!agent5.Dead && agent5 != agent2 && (agent4 == null || agent5.Fitness > agent4.Fitness))
				{
					agent4 = agent5;
				}
			}
			if (agent4 != null)
			{
				Genome genome = Genome.Crossover(agent2.G, agent4.G, Rng);
				genome.Mutate(Rng, MutationRate, MutationStrength);
				string text = agent.Name + " g" + agent.Generation + " (fit " + (int)agent.Fitness + ")";
				agent.G = genome;
				agent.Generation = Species.Generation + 1;
				BrainState brainState = new BrainState();
				agent2.Brain.InheritInto(brainState, InheritanceFraction * 0.8f, Rng);
				agent.Brain = brainState;
				agent.SessionScore = 0f;
				agent.Health = 100f;
				agent.DamageTaken = 0f;
				agent.AnchorHome = FindSpawnNear(agent.AnchorHome, agent.Id + (int)evoTimer);
				Species.Generation = Math.Max(Species.Generation, agent.Generation);
				TotalBirths++;
				Species.Births = TotalBirths;
				Log("tournament: " + text + " replaced by a child of " + agent2.Name + " x " + agent4.Name + " -> g" + agent.Generation + " [" + genome.Describe() + "]");
			}
		}

		public void EvolveNow()
		{
			TournamentStep();
			Log("forced evolution step by the operator");
		}

		public void Log(string s)
		{
			Chronicle.Add(s);
			if (Chronicle.Count > 120)
			{
				Chronicle.RemoveAt(0);
			}
		}

		public Agent FindNearest(Vec3 p, float maxDist)
		{
			Agent result = null;
			float num = maxDist * maxDist;
			for (int i = 0; i < Agents.Count; i++)
			{
				if (!Agents[i].Dead)
				{
					float num2 = Vec3.DistanceSq(Agents[i].Body.Position, p);
					if (num2 < num)
					{
						num = num2;
						result = Agents[i];
					}
				}
			}
			return result;
		}

		public List<Agent> Leaderboard()
		{
			List<Agent> list = new List<Agent>(Agents);
			list.Sort((Agent a, Agent b) => b.Fitness.CompareTo(a.Fitness));
			return list;
		}

		public string StatusLine()
		{
			int num = 0;
			for (int i = 0; i < Agents.Count; i++)
			{
				if (!Agents[i].Dead)
				{
					num++;
				}
			}
			return string.Format(CultureInfo.InvariantCulture, "AI: {0}/{1} alive   gen {2}   births {3}   best fit {4:0}", num, Agents.Count, Species.Generation, TotalBirths, BestFitnessEver);
		}

		public string Save()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("species=" + Species.Generation + "," + Species.Births + "," + Species.BestFitness + "," + F(Species.LavaMargin) + "," + F(Species.WaterCaution) + "," + F(Species.CliffCaution) + "," + F(Species.CombatThreshold) + "," + Species.DeathsLava + "," + Species.DeathsFall + "," + Species.DeathsDrown + "," + Species.DeathsCombat + "," + F(Species.ExplorationRadius) + "," + F(Species.NightShelter));
			stringBuilder.AppendLine("counters=" + TotalBirths + "," + TotalDeaths);
			for (int i = 0; i < Agents.Count; i++)
			{
				Agent agent = Agents[i];
				stringBuilder.Append("agent=").Append(agent.Id).Append(',')
					.Append(agent.Name)
					.Append(',')
					.Append(agent.Generation)
					.Append(',');
				stringBuilder.Append((int)agent.Fitness).Append(',').Append(agent.MinedTotal)
					.Append(',')
					.Append(agent.PlacementsTotal)
					.Append(',');
				stringBuilder.Append(agent.CraftsTotal).Append(',').Append(agent.StructuresBuilt)
					.Append(',');
				for (int j = 0; j < 12; j++)
				{
					stringBuilder.Append(F(agent.G.V[j])).Append('|');
				}
				stringBuilder.Append(',');
				for (int k = 0; k < 14; k++)
				{
					stringBuilder.Append(F(agent.Brain.Q[k])).Append('|');
				}
				stringBuilder.Append(',');
				stringBuilder.Append(F(agent.Brain.SkillMining)).Append('|').Append(F(agent.Brain.SkillBuilding))
					.Append('|');
				stringBuilder.Append(F(agent.Brain.SkillCombat)).Append('|').Append(F(agent.Brain.SkillNavigation))
					.Append('|');
				stringBuilder.Append(F(agent.Brain.SkillCrafting)).Append('|').Append(F(agent.Brain.SkillSurvival));
				stringBuilder.AppendLine();
			}
			return stringBuilder.ToString();
		}

		private static string F(float v)
		{
			return v.ToString("0.####", CultureInfo.InvariantCulture);
		}

		public void Load(string data)
		{
			if (string.IsNullOrEmpty(data))
			{
				return;
			}
			string[] array = data.Split(new char[] { '\n' }, StringSplitOptions.None);
			List<Agent> list = new List<Agent>();
			for (int i = 0; i < array.Length; i++)
			{
				string text = array[i].Trim();
				if (text.StartsWith("species="))
				{
					string[] array2 = text.Substring(8).Split(new char[] { ',' }, StringSplitOptions.None);
					if (array2.Length >= 8)
					{
						Species.Generation = ParseInt(array2[0], 1);
						Species.Births = ParseInt(array2[1], 0);
						Species.BestFitness = ParseInt(array2[2], 0);
						Species.LavaMargin = ParseFloat(array2[3], 2.2f);
						Species.WaterCaution = ParseFloat(array2[4], 0.2f);
						Species.CliffCaution = ParseFloat(array2[5], 1.5f);
						Species.CombatThreshold = ParseFloat(array2[6], 0.55f);
						Species.DeathsLava = ParseInt(array2[7], 0);
						if (array2.Length > 8)
						{
							Species.DeathsFall = ParseInt(array2[8], 0);
						}
						if (array2.Length > 9)
						{
							Species.DeathsDrown = ParseInt(array2[9], 0);
						}
						if (array2.Length > 10)
						{
							Species.DeathsCombat = ParseInt(array2[10], 0);
						}
					}
				}
				else if (text.StartsWith("counters="))
				{
					string[] array3 = text.Substring(9).Split(new char[] { ',' }, StringSplitOptions.None);
					if (array3.Length >= 2)
					{
						TotalBirths = ParseInt(array3[0], 0);
						TotalDeaths = ParseInt(array3[1], 0);
					}
				}
				else
				{
					if (!text.StartsWith("agent="))
					{
						continue;
					}
					string[] array4 = text.Substring(6).Split(new char[] { ',' }, StringSplitOptions.None);
					if (array4.Length < 9)
					{
						continue;
					}
					Agent agent = new Agent();
					agent.Species = Species;
					agent.Id = ParseInt(array4[0], nextId++);
					agent.Name = array4[1];
					agent.Generation = ParseInt(array4[2], 1);
					agent.Fitness = ParseFloat(array4[3], 0f);
					agent.MinedTotal = ParseInt(array4[4], 0);
					agent.PlacementsTotal = ParseInt(array4[5], 0);
					agent.CraftsTotal = ParseInt(array4[6], 0);
					agent.StructuresBuilt = ParseInt(array4[7], 0);
					agent.G = new Genome();
					string[] array5 = array4[8].Split(new char[] { '|' }, StringSplitOptions.None);
					for (int j = 0; j < 12; j++)
					{
						agent.G.V[j] = ((j >= array5.Length) ? Rng.NextFloat() : ParseFloat(array5[j], 0.5f));
					}
					if (array4.Length > 9)
					{
						string[] array6 = array4[9].Split(new char[] { '|' }, StringSplitOptions.None);
						for (int k = 0; k < 14; k++)
						{
							if (k < array6.Length)
							{
								agent.Brain.Q[k] = ParseFloat(array6[k], 0.1f);
							}
						}
					}
					if (array4.Length > 10)
					{
						string[] array7 = array4[10].Split(new char[] { '|' }, StringSplitOptions.None);
						if (array7.Length > 0)
						{
							agent.Brain.SkillMining = ParseFloat(array7[0], 0f);
						}
						if (array7.Length > 1)
						{
							agent.Brain.SkillBuilding = ParseFloat(array7[1], 0f);
						}
						if (array7.Length > 2)
						{
							agent.Brain.SkillCombat = ParseFloat(array7[2], 0f);
						}
						if (array7.Length > 3)
						{
							agent.Brain.SkillNavigation = ParseFloat(array7[3], 0f);
						}
						if (array7.Length > 4)
						{
							agent.Brain.SkillCrafting = ParseFloat(array7[4], 0f);
						}
						if (array7.Length > 5)
						{
							agent.Brain.SkillSurvival = ParseFloat(array7[5], 0f);
						}
					}
					list.Add(agent);
				}
			}
			Vec3 center = ((World == null) ? new Vec3(0f, 70f, 0f) : World.FindSpawnPoint());
			for (int l = 0; l < list.Count; l++)
			{
				Agent agent2 = list[l];
				Vec3 vec = FindSpawnNear(center, l);
				Creature creature = new Creature();
				creature.Position = vec;
				creature.Name = agent2.Name + " g" + agent2.Generation;
				Rng rng = new Rng(agent2.Id * 7919 + agent2.Generation * 104729);
				float r;
				float g;
				float b;
				Hue(rng.NextFloat(), out r, out g, out b);
				creature.PickColors(rng, r, g, b);
				agent2.Body = creature;
				agent2.AnchorHome = vec;
				nextId = Math.Max(nextId, agent2.Id + 1);
				Agents.Add(agent2);
				if (World != null && World.Creatures != null)
				{
					World.Creatures.Add(creature);
				}
			}
			if (Agents.Count > 0)
			{
				Log("restored " + Agents.Count + " brains from the last session");
			}
		}

		private static int ParseInt(string s, int def)
		{
			int result;
			if (int.TryParse(s.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
			{
				return result;
			}
			float result2;
			if (float.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out result2))
			{
				return (int)result2;
			}
			return def;
		}

		private static float ParseFloat(string s, float def)
		{
			float result;
			if (float.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out result))
			{
				return result;
			}
			return def;
		}
	}
}
