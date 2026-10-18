using System;
using System.Collections.Generic;
using System.Text;
using VoxelAI.Core;
using VoxelAI.Entities;
using VoxelAI.World;

namespace VoxelAI.AI
{
	public class Agent
	{
		public int Id;

		public string Name = "AGENT";

		public Creature Body;

		public Genome G;

		public BrainState Brain = new BrainState();

		public SpeciesMemory Species;

		public int Generation = 1;

		public int Deaths;

		public string DeathCause = "-";

		public float Fitness;

		public float SessionScore;

		public float Health = 100f;

		public float Age;

		public float Lifetime;

		public float DamageTaken;

		public float LastDamageAge = -99f;

		public string LastDamageCause = "-";

		public Act Action;

		public float ActionTime;

		public float ActionLimit;

		public float ActionReward;

		public Vec3i TargetBlock;

		public bool HasBlockTarget;

		public Vec3 MoveTarget;

		public bool HasMoveTarget;

		public float MiningProgress;

		public float MiningHardness = 1f;

		public byte MiningBlock;

		public Dictionary<byte, int> Inventory = new Dictionary<byte, int>();

		public List<Placement> Plan;

		public int PlanIndex;

		public Vec3i PlanOrigin;

		public float DecisionTimer;

		public string Status = "waking up";

		public float StuckTimer;

		public float StillTimer;

		public float LastTargetDist = float.MaxValue;

		public float depthBelowSurface;

		public float MotionTimer;

		public Vec3 LastPos;

		public Vec3 MotionLast;

		private bool hasStuckDir;

		public Vec3 StuckDir;

		public Vec3 AnchorHome;

		public float SwimTimer;

		public float Breath = 10f;

		public const float MaxBreath = 10f;

		public float RestTimer;

		public int PlacementsTotal;

		public int MinedTotal;

		public int CraftsTotal;

		public int StructuresBuilt;

		public int OresFound;

		public int Kills;

		public int Pickups;

		public bool Dead;

		public float RespawnTimer;

		public bool IsSelected;

		private float SenseRadius;

		private Vec3i bestResource;

		private byte bestResourceBlock;

		private float bestResourceScore;

		private bool hazardLavaNear;

		private Vec3i nearestLava;

		private bool waterNear;

		private bool cliffAhead;

		private int nearestDropIndex;

		private float nearestDropDist;

		private Agent nearestAgent;

		private float nearestAgentDist;

		private bool hasFood;

		private int foodBlock;

		private bool darkHere;

		public string ShortName
		{
			get
			{
				return Name + " g" + Generation;
			}
		}

		public float HealthFrac
		{
			get
			{
				return M.Clamp(Health / 100f, 0f, 1f);
			}
		}

		private static readonly float[] SteerAngles = new float[6]
		{
			0.62f,
			-0.62f,
			1.15f,
			-1.15f,
			1.95f,
			-1.95f
		};

		public int CarryCount
		{
			get
			{
				int num = 0;
				foreach (KeyValuePair<byte, int> item in Inventory)
				{
					num += item.Value;
				}
				return num;
			}
		}

		public int CarryLimit
		{
			get
			{
				return 24 + (int)(G[3] * 56f);
			}
		}

		public float BuildMaterials
		{
			get
			{
				return CountOf(4) + CountOf(3) + CountOf(9) + CountOf(2) + CountOf(5) + CountOf(7) + CountOf(10) + CountOf(11);
			}
		}

		public int CountOf(byte b)
		{
			int value;
			return Inventory.TryGetValue(b, out value) ? value : 0;
		}

		public void AddItem(byte b, int n)
		{
			if (b != 0 && n > 0)
			{
				int num = CountOf(b);
				Inventory[b] = Math.Min(num + n, CarryLimit);
			}
		}

		public bool TakeItem(byte b, int n)
		{
			int num = CountOf(b);
			if (num < n)
			{
				return false;
			}
			if (num == n)
			{
				Inventory.Remove(b);
			}
			else
			{
				Inventory[b] = num - n;
			}
			return true;
		}

		public bool HasAny(params byte[] kinds)
		{
			foreach (byte b in kinds)
			{
				if (CountOf(b) > 0)
				{
					return true;
				}
			}
			return false;
		}

		public void Update(VoxelAI.World.World w, AISystem sys, float dt)
		{
			if (Dead)
			{
				RespawnTimer -= dt;
				return;
			}
			Age += dt;
			Lifetime += dt;
			float num = Vec3.Distance(new Vec3(Body.Position.X, 0f, Body.Position.Z), new Vec3(MotionLast.X, 0f, MotionLast.Z));
			MotionLast = Body.Position;
			if (Action == Act.Mine && MiningProgress > 0.05f)
			{
				MotionTimer = 0f;
			}
			else if (num < 0.035f * dt * 30f)
			{
				MotionTimer += dt;
			}
			else
			{
				MotionTimer = 0f;
			}
			SessionScore += dt * 0.01f;
			Brain.DecayDanger(dt);
			Brain.LearningRate = 0.05f + G[11] * 0.25f;
			w.CollectItems(Body.Position, 1.3f + G[3] * 0.8f, OnPickup);
			PickUpNearbyDrops(w);
			bool lava;
			bool inLiquid = Physics.InLiquid(w, Body.Position, Body.Size * 0.5f, out lava);
			if (lava)
			{
				ReactToLava(w, dt);
				return;
			}
			UpdateBreath(w, inLiquid, dt);
			if (Health < 100f && Age - LastDamageAge > 8f && Action != Act.Flee)
			{
				Health = Math.Min(100f, Health + dt * (0.5f + Brain.SkillSurvival * 1.3f));
			}
			UpdateSwimming(w, inLiquid, dt);
			if (Action == Act.Build && Plan != null)
			{
				ExecuteBuild(w, sys, dt);
				return;
			}
			DecisionTimer -= dt;
			if (DecisionTimer <= 0f)
			{
				DecisionTimer = 0.22f + (1f - G[10]) * 0.35f;
				SenseAll(w, sys);
				Decide(w, sys);
			}
			Execute(w, sys, dt);
		}

		private void UpdateBreath(VoxelAI.World.World w, bool inLiquid, float dt)
		{
			int hx = (int)Math.Floor(Body.Position.X);
			int hy = (int)Math.Floor(Body.Position.Y + Body.Size.Y * 0.72f);
			int hz = (int)Math.Floor(Body.Position.Z);
			byte headBlock = w.GetBlock(hx, hy, hz);
			bool headWet = headBlock == 12;
			if (headWet)
			{
				Breath -= dt;
				if (Breath <= 0f)
				{
					Breath = 0f;
					Damage(7f * dt, "water", w);
					Brain.SkillSurvival = M.Clamp(Brain.SkillSurvival + 0.015f * dt, 0f, 1f);
				}
			}
			else if (Breath < MaxBreath)
			{
				Breath = Math.Min(MaxBreath, Breath + dt * 2.4f);
			}
			if (!inLiquid)
			{
				if (Breath < MaxBreath && !headWet)
				{
					Breath = Math.Min(MaxBreath, Breath + dt * 1.6f);
				}
			}
		}

		private void UpdateSwimming(VoxelAI.World.World w, bool inLiquid, float dt)
		{
			if (inLiquid)
			{
				SwimTimer += dt;
				float num = Body.Position.Y + Body.Size.Y * 0.5f;
				bool headStuck = w.GetBlock((int)Math.Floor(Body.Position.X), (int)Math.Floor(num), (int)Math.Floor(Body.Position.Z)) != 0;
				if (headStuck || SwimTimer > 4f || Breath < 5.5f)
				{
					Body.Velocity.Y = Math.Max(Body.Velocity.Y, 3.6f);
					if (SwimTimer > 4f)
					{
						Brain.Note("surfacing");
					}
					if (Breath < 3.5f && Action != Act.Flee && Action != Act.Build)
					{
						SetAction(Act.SwimUp, "gasping for air", 3f);
					}
				}
			}
			else
			{
				SwimTimer = 0f;
			}
		}

		private void PickUpNearbyDrops(VoxelAI.World.World w)
		{
			if (CarryCount >= CarryLimit)
			{
				return;
			}
			Vec3 center = Body.Position;
			for (int i = 0; i < w.Items.Count; i++)
			{
				ItemEntity itemEntity = w.Items[i];
				Vec3 a = center - itemEntity.Position;
				float num = a.Length;
				if (num < 2.8f && num > 0.05f)
				{
					Vec3 pull = a / num * (2.2f + G[3] * 2.2f);
					itemEntity.Velocity.X = M.ExpSmooth(itemEntity.Velocity.X, pull.X, 6f, 0.05f);
					itemEntity.Velocity.Z = M.ExpSmooth(itemEntity.Velocity.Z, pull.Z, 6f, 0.05f);
					if (itemEntity.OnGround)
					{
						itemEntity.Velocity.Y = Math.Max(itemEntity.Velocity.Y, 1.4f);
					}
				}
			}
		}

		private void ReactToLava(VoxelAI.World.World w, float dt)
		{
			Damage(28f * dt, "lava", w);
			Brain.MarkDanger((int)Math.Floor(Body.Position.X), (int)Math.Floor(Body.Position.Y), (int)Math.Floor(Body.Position.Z), 0.4f);
			Vec3 target = Body.Position;
			float num = float.MaxValue;
			for (int i = -4; i <= 4; i++)
			{
				for (int j = -4; j <= 4; j++)
				{
					int num2 = (int)Math.Floor(Body.Position.X) + i;
					int num3 = (int)Math.Floor(Body.Position.Z) + j;
					int num4 = w.GroundY(num2, num3);
					if (num4 > 0 && w.GetB(num2, num4, num3) != B.Lava && w.GetB(num2, num4, num3) != B.Water)
					{
						float num5 = Math.Abs(i) + Math.Abs(j);
						if (num5 < num)
						{
							num = num5;
							target = new Vec3((float)num2 + 0.5f, (float)num4 + 1.2f, (float)num3 + 0.5f);
						}
					}
				}
			}
			MoveTowards(w, target, 4.6f, dt);
			Body.Velocity.Y = Math.Max(Body.Velocity.Y, 3.5f);
			SetAction(Act.Flee, "OUT OF LAVA!", 4f);
		}

		private void OnPickup(byte block, int count)
		{
			if (CarryCount < CarryLimit)
			{
				AddItem(block, count);
				Pickups += count;
				SessionScore += 0.4f * (float)count;
				if (Action == Act.Collect)
				{
					Award(0.35f * (float)count);
				}
				int num = OreValue(block);
				if (num > 0)
				{
					OresFound++;
					SessionScore += (float)num * 0.5f;
				}
			}
		}

		private void SenseAll(VoxelAI.World.World w, AISystem sys)
		{
			SenseRadius = 8f + G[10] * 9f;
			bestResourceScore = -1f;
			bestResource = Vec3i.Zero;
			bestResourceBlock = 0;
			hazardLavaNear = false;
			nearestLava = Vec3i.Zero;
			waterNear = false;
			cliffAhead = false;
			nearestDropDist = float.MaxValue;
			nearestDropIndex = -1;
			nearestAgent = null;
			nearestAgentDist = float.MaxValue;
			hasFood = false;
			foodBlock = 0;
			int num = (int)Math.Floor(Body.Position.X);
			int num2 = (int)Math.Floor(Body.Position.Y);
			int num3 = (int)Math.Floor(Body.Position.Z);
			darkHere = w.LightAt(num, num2, num3) < 6;
			depthBelowSurface = w.GroundY(num, num3) - num2;
			int num4 = (int)SenseRadius;
			for (int i = -num4; i <= num4; i++)
			{
				for (int j = -num4; j <= num4; j++)
				{
					if (i * i + j * j > num4 * num4)
					{
						continue;
					}
					int x = num + i;
					int z = num3 + j;
					int num5 = w.GroundY(x, z);
					if (num5 <= 0)
					{
						continue;
					}
					if (i * i + j * j <= 81)
					{
						for (int k = Math.Max(1, num5 - 3); k <= num5 + 2; k++)
						{
							switch (w.GetBlock(x, k, z))
							{
							case 13:
								hazardLavaNear = true;
								if (nearestLava == Vec3i.Zero || Math.Abs(i) + Math.Abs(j) < Math.Abs(nearestLava.X - num) + Math.Abs(nearestLava.Z - num3))
								{
									nearestLava = new Vec3i(x, k, z);
								}
								break;
							case 12:
								waterNear = true;
								break;
							}
						}
					}
					byte block = w.GetBlock(x, num5, z);
					int num6 = num5;
					if (block == 7)
					{
						while (num6 > 1 && w.GetBlock(x, num6 - 1, z) == 7)
						{
							num6--;
						}
					}
					int num7 = ResourcePriority(block);
					if (num7 <= 0 || !IsExposed(w, x, num6, z))
					{
						continue;
					}
					num5 = num6;
					if (num5 - num2 <= 5 && num5 - num2 >= -16)
					{
						float num8 = (float)Math.Sqrt(i * i + j * j) + (float)Math.Abs(num5 - num2) * 0.6f;
						float num9 = (float)num7 * 10f - num8;
						num9 -= (float)Math.Max(0, num5 - num2 - 2) * 9f;
						num9 -= Brain.DangerAt(x, num5, z) * 22f;
						num9 -= CrowdPenalty(sys, x, num5, z);
						if (num8 < 3.6f)
						{
							num9 += 4f;
						}
						num9 -= (float)Math.Max(0, num2 - num5 - 2) * 4f;
						bool flag = CountOf(7) + CountOf(9) < 4;
						if (num5 > num2)
						{
							num9 += ((!(depthBelowSurface > 8f)) ? 0f : 8f) + ((!flag) ? 0f : 6f);
						}
						if (num9 > bestResourceScore)
						{
							bestResourceScore = num9;
							bestResource = new Vec3i(x, num5, z);
							bestResourceBlock = block;
						}
					}
				}
			}
			for (int l = 0; l < w.Items.Count; l++)
			{
				ItemEntity itemEntity = w.Items[l];
				float num10 = Vec3.Distance(itemEntity.Position, Body.Position);
				if (num10 < nearestDropDist && num10 < 26f)
				{
					nearestDropDist = num10;
					nearestDropIndex = l;
				}
			}
			for (int m = 0; m < sys.Agents.Count; m++)
			{
				Agent agent = sys.Agents[m];
				if (agent != this && !agent.Dead)
				{
					float num11 = Vec3.Distance(agent.Body.Position, Body.Position);
					if (num11 < nearestAgentDist)
					{
						nearestAgentDist = num11;
						nearestAgent = agent;
					}
				}
			}
			float yaw = Body.Yaw;
			int x2 = (int)Math.Floor((double)Body.Position.X + Math.Sin(yaw) * 2.0);
			int z2 = (int)Math.Floor((double)Body.Position.Z + Math.Cos(yaw) * 2.0);
			int num12 = w.GroundY(x2, z2);
			if (num2 - num12 > 4)
			{
				cliffAhead = true;
			}
			hasFood = HealthFrac < 0.78f && Age - LastDamageAge > 5f && !hazardLavaNear;
		}

		private float CrowdPenalty(AISystem sys, int x, int y, int z)
		{
			float num = 0f;
			for (int i = 0; i < sys.Agents.Count; i++)
			{
				Agent agent = sys.Agents[i];
				if (agent == this || agent.Dead)
				{
					continue;
				}
				if (agent.HasBlockTarget && agent.TargetBlock.X == x && agent.TargetBlock.Y == y && agent.TargetBlock.Z == z)
				{
					num += 8f;
					continue;
				}
				float num2 = agent.Body.Position.X - ((float)x + 0.5f);
				float num3 = agent.Body.Position.Z - ((float)z + 0.5f);
				float num4 = (float)Math.Sqrt(num2 * num2 + num3 * num3);
				if (num4 < 2.4f)
				{
					num += (2.4f - num4) * 3f;
				}
			}
			return num;
		}

		private void StartUnstick(VoxelAI.World.World w, AISystem sys)
		{
			MotionTimer = 0f;
			float num;
			if (HasMoveTarget || HasBlockTarget)
			{
				Vec3 a = ((!HasBlockTarget) ? MoveTarget : new Vec3((float)TargetBlock.X + 0.5f, (float)TargetBlock.Y + 0.5f, (float)TargetBlock.Z + 0.5f));
				Vec3 vec = a - Body.Position;
				vec.Y = 0f;
				num = ((!(vec.Length > 0.01f)) ? Body.Yaw : ((float)Math.Atan2(vec.X, vec.Z)));
			}
			else
			{
				num = Body.Yaw;
			}
			float[] array = new float[8]
			{
				0f,
				0.79f,
				-0.79f,
				1.57f,
				-1.57f,
				3.14f,
				2.36f,
				-2.36f
			};
			int num2 = (int)Math.Floor(Body.Position.X);
			int num3 = (int)Math.Floor(Body.Position.Y);
			int num4 = (int)Math.Floor(Body.Position.Z);
			for (int i = 0; i < array.Length; i++)
			{
				float num5 = num + array[i];
				int num6 = (int)Math.Round(Math.Sin(num5));
				int num7 = (int)Math.Round(Math.Cos(num5));
				if (num6 != 0 || num7 != 0)
				{
					int x = num2 + num6;
					int z = num4 + num7;
					byte block = w.GetBlock(x, num3, z);
					if (block == 0 || Blocks.Info((B)block).Liquid)
					{
						StuckDir = new Vec3((float)Math.Sin(num5), 0f, (float)Math.Cos(num5));
						hasStuckDir = true;
						SetAction(Act.Stuck, "wedged: stepping out", 6f);
						HasMoveTarget = true;
						MoveTarget = new Vec3((float)(num2 + num6 * 2) + 0.5f, (float)num3 + 1.2f, (float)(num4 + num7 * 2) + 0.5f);
						return;
					}
				}
			}
			float num8 = float.MaxValue;
			int num9 = num2;
			int num10 = num4;
			bool flag = false;
			for (int j = 0; j < array.Length; j++)
			{
				float num11 = num + array[j];
				int num12 = (int)Math.Round(Math.Sin(num11));
				int num13 = (int)Math.Round(Math.Cos(num11));
				if (num12 == 0 && num13 == 0)
				{
					continue;
				}
				byte block2 = w.GetBlock(num2 + num12, num3, num4 + num13);
				if (block2 != 0)
				{
					BlockInfo blockInfo = Blocks.Info((B)block2);
					if (!blockInfo.Liquid && !(blockInfo.Hardness >= 9f) && blockInfo.Hardness < num8)
					{
						num8 = blockInfo.Hardness;
						num9 = num2 + num12;
						num10 = num4 + num13;
						flag = true;
					}
				}
			}
			if (!flag)
			{
				Brain.MarkDanger(num2, num3, num4, 0.5f);
				Award(-0.3f);
				StartExplore(w, sys);
				return;
			}
			StuckDir = new Vec3(num9 - num2, 0f, num10 - num4).Normalized;
			hasStuckDir = true;
			TargetBlock = new Vec3i(num9, num3, num10);
			HasBlockTarget = true;
			MiningProgress = 0f;
			MiningHardness = num8;
			SetAction(Act.Stuck, "wedged: tunnelling out", 7f);
			HasBlockTarget = true;
		}

		private void ExecuteUnstick(VoxelAI.World.World w, AISystem sys, float dt)
		{
			if (!hasStuckDir)
			{
				FinishAction(false);
				return;
			}
			int num = (int)Math.Floor(Body.Position.X);
			int num2 = (int)Math.Floor(Body.Position.Y);
			int num3 = (int)Math.Floor(Body.Position.Z);
			int num4 = (int)Math.Round(StuckDir.X);
			int num5 = (int)Math.Round(StuckDir.Z);
			int num6 = num + num4;
			int num7 = num3 + num5;
			byte block = w.GetBlock(num6, num2, num7);
			bool flag = block != 0 && !Blocks.Info((B)block).Liquid;
			BlockInfo blockInfo = ((!flag) ? null : Blocks.Info((B)block));
			if (flag && blockInfo.Hardness < 9f)
			{
				if (TargetBlock.X != num6 || TargetBlock.Z != num7 || TargetBlock.Y != num2)
				{
					TargetBlock = new Vec3i(num6, num2, num7);
					MiningHardness = blockInfo.Hardness;
					MiningProgress = 0f;
					HasBlockTarget = true;
				}
				MiningProgress += dt * (1.5f + Brain.SkillMining * 1.5f) * (1f + G[6] * 0.4f);
				FaceTowards(new Vec3((float)num6 + 0.5f, (float)num2 + 0.5f, (float)num7 + 0.5f));
				if (MiningProgress >= MiningHardness)
				{
					w.BreakBlock(num6, num2, num7);
					MinedTotal++;
					SessionScore += 0.4f;
					Award(0.1f);
					MiningProgress = 0f;
					MotionTimer = 0f;
				}
				return;
			}
			byte block2 = w.GetBlock(num6, num2 + 1, num7);
			bool flag2 = block2 != 0 && !Blocks.Info((B)block2).Liquid;
			if (!flag && flag2)
			{
				TargetBlock = new Vec3i(num6, num2 + 1, num7);
				MiningHardness = Blocks.Info((B)block2).Hardness;
				MiningProgress += dt * (1.5f + Brain.SkillMining * 1.5f);
				FaceTowards(new Vec3((float)num6 + 0.5f, (float)num2 + 0.5f, (float)num7 + 0.5f));
				if (MiningProgress >= MiningHardness)
				{
					w.BreakBlock(num6, num2 + 1, num7);
					MinedTotal++;
					SessionScore += 0.4f;
					MiningProgress = 0f;
				}
			}
			else
			{
				MoveTowards(w, new Vec3((float)num6 + 0.5f, Body.Position.Y, (float)num7 + 0.5f), 3.4f, dt);
				if (MotionTimer < 0.3f && ActionTime > 0.4f)
				{
					Award(0.05f);
					hasStuckDir = false;
					FinishAction(true);
				}
			}
		}

		private static bool IsExposed(VoxelAI.World.World w, int x, int y, int z)
		{
			return w.GetBlock(x + 1, y, z) == 0 || w.GetBlock(x - 1, y, z) == 0 || w.GetBlock(x, y, z + 1) == 0 || w.GetBlock(x, y, z - 1) == 0 || w.GetBlock(x, y + 1, z) == 0;
		}

		private int ResourcePriority(byte b)
		{
			if (b == 0)
			{
				return 0;
			}
			switch (b)
			{
			case 7:
				return (CountOf(7) < 4) ? 8 : ((CountOf(7) >= 10) ? 2 : 5);
			case 16:
				return (CountOf(16) >= 6) ? 4 : 7;
			case 17:
				return 8;
			case 18:
				return (!(G[3] > 0.5f)) ? 5 : 9;
			case 19:
				return 10;
			case 3:
			case 4:
				if (CountOf(7) + CountOf(9) < 4)
				{
					return 2;
				}
				return (BuildMaterials < 18f) ? 4 : ((!(depthBelowSurface > 12f)) ? 1 : 2);
			case 5:
				return (CountOf(16) <= 0 || CountOf(5) >= 6) ? 1 : 5;
			case 1:
			case 2:
				return (BuildMaterials < 6f) ? 3 : 0;
			case 14:
				return 1;
			default:
				return 0;
			}
		}

		private static int OreValue(byte b)
		{
			switch (b)
			{
			case 16:
				return 2;
			case 17:
				return 4;
			case 18:
				return 5;
			case 19:
				return 8;
			default:
				return 0;
			}
		}

		private float Utility(Act a, VoxelAI.World.World w, AISystem sys)
		{
			float num = Brain.Q[(int)a];
			float num2;
			switch (a)
			{
			case Act.Mine:
				num2 = 0.15f + G[6] * 0.5f;
				num2 += ((bestResourceBlock == 0) ? (-0.45f) : 0.55f);
				if (IsNight(w))
				{
					num2 += 0.2f;
				}
				num2 += Brain.SkillMining * 0.25f;
				break;
			case Act.Collect:
				num2 = ((nearestDropIndex < 0) ? (-0.6f) : (0.75f + G[3] * 0.25f));
				break;
			case Act.Craft:
				num2 = -1f;
				if (CountOf(7) >= 4 && CountOf(9) < 12)
				{
					num2 = 1.95f;
				}
				if (CountOf(5) >= 2 && CountOf(16) >= 1 && CountOf(10) < 8)
				{
					num2 = 1.85f;
				}
				if (CountOf(9) >= 4 && CountOf(21) == 0)
				{
					num2 = 1.7f;
				}
				else if (CountOf(4) >= 8 && CountOf(22) == 0)
				{
					num2 = 1.6f;
				}
				break;
			case Act.Build:
				num2 = -0.15f + G[7] * 1.2f;
				num2 += ((BuildMaterials >= 12f) ? 0.85f : ((!(BuildMaterials >= 8f)) ? (-0.8f) : 0.45f));
				num2 += ((StructuresBuilt != 0) ? 0.1f : 0.35f);
				if (IsNight(w) && BuildMaterials >= 8f)
				{
					num2 += 0.5f + Species.NightShelter * 0.8f;
				}
				break;
			case Act.Fight:
				num2 = -0.4f + G[0] * 1f;
				if (nearestAgent != null && nearestAgentDist < 4.5f)
				{
					float num3 = (HealthFrac - nearestAgent.HealthFrac) * 1.2f;
					num2 += 0.35f + num3;
					num2 += Brain.SkillCombat * 0.3f;
				}
				else
				{
					num2 -= 0.8f;
				}
				if (HealthFrac < Species.CombatThreshold)
				{
					num2 -= 2.2f;
				}
				break;
			case Act.Flee:
				num2 = ((!(HealthFrac < 0.45f)) ? (-1.1f) : 0.9f);
				if (LastDamageAge > Age - 2.5f)
				{
					num2 += 0.8f;
				}
				if (hazardLavaNear)
				{
					num2 += 0.4f;
				}
				break;
			case Act.Explore:
				num2 = 0.1f + G[1] * 0.7f;
				if (bestResourceBlock == 0 && nearestDropIndex < 0)
				{
					num2 += 0.35f;
				}
				if (IsNight(w))
				{
					num2 -= G[4] * 0.4f;
				}
				if (depthBelowSurface > 8f)
				{
					num2 += 0.65f;
				}
				break;
			case Act.Socialize:
				num2 = -0.1f + G[2] * 0.8f;
				num2 += ((nearestAgent == null || !(nearestAgentDist < 14f)) ? (-0.4f) : 0.25f);
				break;
			case Act.PlaceLight:
				num2 = ((!darkHere || CountOf(23) <= 0) ? (-1f) : 1.1f);
				break;
			case Act.DigDown:
				num2 = -0.45f + G[6] * 0.8f;
				num2 += ((bestResourceBlock != 0) ? (-0.7f) : 0.3f);
				num2 -= G[4] * 0.35f;
				if (depthBelowSurface > 10f)
				{
					num2 -= 0.6f;
				}
				break;
			case Act.Eat:
				num2 = ((!hasFood) ? (-2.2f) : (1.05f + (1f - HealthFrac) * 1.5f + Brain.SkillSurvival * 0.35f));
				if (Action == Act.Eat && ActionTime < ActionLimit)
				{
					num2 += 0.8f;
				}
				break;
			case Act.SwimUp:
				num2 = ((!Body.InWaterNow) ? (-1.5f) : ((Breath < 4.5f) ? 2.8f : 0.2f));
				break;
			case Act.Stuck:
				num2 = -2f;
				break;
			default:
				num2 = 0.05f;
				break;
			}
			if (a == Act.Explore || a == Act.DigDown)
			{
				num2 -= ((!hazardLavaNear) ? 0f : 0.5f) * (0.5f + G[4]);
				num2 -= ((!waterNear) ? 0f : (Species.WaterCaution * 0.6f));
				num2 -= ((!cliffAhead) ? 0f : (Species.CliffCaution * 0.15f));
			}
			if (a == Act.Mine && hazardLavaNear)
			{
				num2 -= 0.35f * G[4];
			}
			if (a == Act.Mine || a == Act.Collect)
			{
				Vec3i vec3i = ((a != Act.Mine) ? Body.Position.ToVec3i() : bestResource);
				num2 = ((a != Act.Mine) ? num2 : (num2 - Brain.DangerAt(vec3i.X, vec3i.Y, vec3i.Z) * (0.6f + G[4])));
			}
			return num2 + num;
		}

		public string UtilityDump(VoxelAI.World.World w, AISystem sys)
		{
			SenseAll(w, sys);
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < 14; i++)
			{
				Act act = (Act)i;
				if (act != Act.Count)
				{
					stringBuilder.Append(act).Append('=').Append(Utility(act, w, sys).ToString("0.00"))
						.Append(' ');
				}
			}
			return stringBuilder.ToString();
		}

		private void Decide(VoxelAI.World.World w, AISystem sys)
		{
			if (hazardLavaNear && Math.Abs((float)nearestLava.X - Body.Position.X) + Math.Abs((float)nearestLava.Z - Body.Position.Z) < Species.LavaMargin)
			{
				StartFleeFrom(new Vec3((float)nearestLava.X + 0.5f, nearestLava.Y, (float)nearestLava.Z + 0.5f), "lava too close");
				return;
			}
			if (MotionTimer > 2.2f && Body.OnGround && !Body.InWaterNow && Action != Act.Build && Action != Act.Stuck && Action != Act.Craft && Action != Act.PlaceLight)
			{
				StartUnstick(w, sys);
				return;
			}
			Act act = Act.Idle;
			float num = float.MinValue;
			for (int i = 0; i < 14; i++)
			{
				Act act2 = (Act)i;
				if (act2 != Act.Count && act2 != 0)
				{
					float num2 = Utility(act2, w, sys);
					num2 += (sys.Rng.NextFloat() - 0.5f) * 0.18f;
					num2 += 0.22f / (1f + (float)Brain.Tries[i] * 0.045f);
					if (num2 > num)
					{
						num = num2;
						act = act2;
					}
				}
			}
			bool flag = Action == Act.Mine || Action == Act.DigDown || Action == Act.Build || Action == Act.Collect || Action == Act.Explore || Action == Act.Socialize || Action == Act.Fight || Action == Act.Flee || Action == Act.Eat;
			bool flag2 = act == Act.Craft || act == Act.PlaceLight;
			if (flag && ActionTime < ActionLimit * 0.85f && !flag2)
			{
				float num3 = Utility(Action, w, sys);
				if (num3 >= num - 0.3f)
				{
					if (Action == Act.Mine)
					{
						HasBlockTarget = HasBlockTarget && w.GetBlock(TargetBlock.X, TargetBlock.Y, TargetBlock.Z) != 0;
					}
					if (HasBlockTarget || Action == Act.Build || HasMoveTarget || Action == Act.Eat)
					{
						return;
					}
				}
			}
			switch (act)
			{
			case Act.Mine:
				StartMine(w, sys);
				break;
			case Act.Collect:
				StartCollect(w, sys);
				break;
			case Act.Craft:
				StartCraft(w, sys);
				break;
			case Act.Build:
				StartBuild(w, sys);
				break;
			case Act.Fight:
				StartFight(w, sys);
				break;
			case Act.Flee:
				StartFleeFrom((nearestAgent == null) ? Body.Position : nearestAgent.Body.Position, "danger");
				break;
			case Act.Eat:
				SetAction(Act.Eat, "resting to heal", 8f);
				break;
			case Act.SwimUp:
				SetAction(Act.SwimUp, "swimming to the surface", 3f);
				break;
			case Act.Explore:
				StartExplore(w, sys);
				break;
			case Act.Socialize:
				StartSocialize(w, sys);
				break;
			case Act.PlaceLight:
				StartPlaceLight(w, sys);
				break;
			case Act.DigDown:
				StartDigDown(w, sys);
				break;
			case Act.Stuck:
				StartUnstick(w, sys);
				break;
			default:
				SetAction(Act.Idle, "watching the world", 2f);
				break;
			}
		}

		private void SetAction(Act a, string status, float limit)
		{
			if (Action != a && ActionReward != 0f)
			{
				Brain.Reward(Action, ActionReward);
				ActionReward = 0f;
			}
			Action = a;
			Status = status;
			ActionTime = 0f;
			ActionLimit = limit;
			HasBlockTarget = false;
			HasMoveTarget = false;
			MiningProgress = 0f;
		}

		public void Award(float r)
		{
			ActionReward += r;
		}

		private void StartMine(VoxelAI.World.World w, AISystem sys)
		{
			if (bestResourceBlock == 0)
			{
				byte[] array = new byte[7]
				{
					7,
					16,
					17,
					3,
					5,
					18,
					19
				};
				for (int i = 0; i < array.Length; i++)
				{
					Vec3i found;
					if (sys.TryGetKnownResource(array[i], Body.Position, out found))
					{
						float num = Vec3.Distance(found.ToVec3(), Body.Position);
						if (!(num > 48f))
						{
							TargetBlock = found;
							bestResourceBlock = array[i];
							bestResource = found;
							HasBlockTarget = true;
							MiningHardness = Blocks.Info((B)array[i]).Hardness;
							SetAction(Act.Mine, "heading to a known " + Blocks.Name((B)array[i]), 40f);
							HasBlockTarget = true;
							return;
						}
					}
				}
				StartExplore(w, sys);
				return;
			}
			bool flag = Action == Act.Mine && HasBlockTarget && TargetBlock == bestResource;
			TargetBlock = bestResource;
			HasBlockTarget = true;
			MiningHardness = Blocks.Info((B)bestResourceBlock).Hardness;
			sys.NoteResource(bestResourceBlock, bestResource);
			if (flag)
			{
				Status = "mining " + Blocks.Name((B)bestResourceBlock);
				if (ActionTime > ActionLimit)
				{
					ActionTime = 0f;
				}
			}
			else
			{
				SetAction(Act.Mine, "mining " + Blocks.Name((B)bestResourceBlock), 18f);
				HasBlockTarget = true;
			}
		}

		private void StartCollect(VoxelAI.World.World w, AISystem sys)
		{
			if (nearestDropIndex < 0 || nearestDropIndex >= w.Items.Count)
			{
				StartExplore(w, sys);
				return;
			}
			float num = float.MaxValue;
			Vec3 position = Body.Position;
			for (int i = 0; i < w.Items.Count; i++)
			{
				float num2 = Vec3.Distance(w.Items[i].Position, Body.Position);
				if (num2 < num)
				{
					num = num2;
					position = w.Items[i].Position;
				}
			}
			MoveTarget = position;
			HasMoveTarget = true;
			SetAction(Act.Collect, "collecting drops", 14f);
			HasMoveTarget = true;
		}

		private void StartCraft(VoxelAI.World.World w, AISystem sys)
		{
			if (CountOf(7) >= 4 && CountOf(9) < 12)
			{
				TakeItem(7, 4);
				AddItem(9, 12);
				CraftsTotal++;
				SessionScore += 2f;
				SetAction(Act.Craft, "crafted planks", 1.2f);
				Brain.SkillCrafting = M.Clamp(Brain.SkillCrafting + 0.05f, 0f, 1f);
				Award(0.6f);
			}
			else if (CountOf(5) >= 2 && CountOf(16) >= 1 && CountOf(10) < 8)
			{
				TakeItem(5, 2);
				TakeItem(16, 1);
				AddItem(10, 4);
				CraftsTotal++;
				SessionScore += 3f;
				SetAction(Act.Craft, "smelted glass", 1.2f);
				Award(0.8f);
			}
			else if (CountOf(9) >= 4 && CountOf(21) == 0)
			{
				TakeItem(9, 4);
				AddItem(21, 1);
				CraftsTotal++;
				SessionScore += 3f;
				SetAction(Act.Craft, "built a crafting table", 1.2f);
				Award(0.9f);
			}
			else if (CountOf(4) >= 8 && CountOf(22) == 0)
			{
				TakeItem(4, 8);
				AddItem(22, 1);
				CraftsTotal++;
				SessionScore += 3f;
				Brain.SkillCrafting = M.Clamp(Brain.SkillCrafting + 0.05f, 0f, 1f);
				SetAction(Act.Craft, "built a furnace", 1.2f);
				Award(0.9f);
			}
			else
			{
				StartExplore(w, sys);
			}
		}

		private void StartBuild(VoxelAI.World.World w, AISystem sys)
		{
			if (BuildMaterials < 8f)
			{
				StartMine(w, sys);
				return;
			}
			Vec3i vec3i = Body.Position.ToVec3i();
			Rng rng = sys.Rng;
			for (int i = 0; i < 12; i++)
			{
				int num = (int)Body.Position.X + rng.Range(-6, 7);
				int num2 = (int)Body.Position.Z + rng.Range(-6, 7);
				int num3 = w.GroundY(num, num2);
				if (num3 <= 1)
				{
					continue;
				}
				byte block = w.GetBlock(num, num3, num2);
				if (block == 0 || block == 12 || block == 13)
				{
					continue;
				}
				bool flag = true;
				for (int j = -1; j <= 1; j++)
				{
					if (!flag)
					{
						break;
					}
					for (int k = -1; k <= 1; k++)
					{
						if (!flag)
						{
							break;
						}
						if (Math.Abs(w.GroundY(num + j, num2 + k) - num3) > 1)
						{
							flag = false;
						}
					}
				}
				if (flag)
				{
					vec3i = new Vec3i(num, num3 + 1, num2);
					break;
				}
			}
			PlanOrigin = vec3i;
			byte[] materials = ChooseBuildingMaterials();
			if (BuildMaterials < 20f)
			{
				Plan = BuildPlan.CreateNook(vec3i, materials);
			}
			else
			{
				Plan = BuildPlan.CreateSmallHut(vec3i, materials);
			}
			PlanIndex = 0;
			SetAction(Act.Build, (Plan.Count <= 20) ? "building a nook" : "building a shelter", 110f);
		}

		private byte[] ChooseBuildingMaterials()
		{
			List<byte> list = new List<byte>();
			AddN(list, 4, CountOf(4));
			AddN(list, 3, CountOf(3));
			AddN(list, 9, CountOf(9));
			AddN(list, 11, CountOf(11));
			AddN(list, 2, CountOf(2));
			AddN(list, 5, CountOf(5));
			AddN(list, 7, CountOf(7));
			if (list.Count == 0)
			{
				list.Add(2);
			}
			byte[] array = new byte[Math.Min(list.Count, 4)];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = list[i];
			}
			return array;
		}

		private static void AddN(List<byte> l, byte b, int n)
		{
			for (int i = 0; i < n && i < 12; i++)
			{
				l.Add(b);
			}
		}

		private void StartFight(VoxelAI.World.World w, AISystem sys)
		{
			if (nearestAgent == null)
			{
				StartExplore(w, sys);
				return;
			}
			SetAction(Act.Fight, "fighting " + nearestAgent.Name, 12f);
			MoveTarget = nearestAgent.Body.Position;
			HasMoveTarget = true;
		}

		private void StartFleeFrom(Vec3 threat, string why)
		{
			Vec3 b = Body.Position - threat;
			b.Y = 0f;
			if (b.LengthSq < 0.01f)
			{
				b = new Vec3(1f, 0f, 0f);
			}
			b = b.Normalized * 14f;
			MoveTarget = Body.Position + b;
			HasMoveTarget = true;
			SetAction(Act.Flee, "fleeing " + why, 6f);
			HasMoveTarget = true;
		}

		private void StartExplore(VoxelAI.World.World w, AISystem sys)
		{
			Rng rng = sys.Rng;
			float num = 12f + G[1] * (Species.ExplorationRadius - 12f);
			float num2 = rng.RangeF(0f, (float)Math.PI * 2f);
			Vec3 vec = Body.Position + new Vec3((float)Math.Cos(num2) * num, 0f, (float)Math.Sin(num2) * num);
			if (nearestAgent != null && G[2] > 0.6f)
			{
				vec = Vec3.Lerp(vec, nearestAgent.Body.Position, 0.35f);
			}
			MoveTarget = vec;
			HasMoveTarget = true;
			SetAction(Act.Explore, "exploring", 18f);
			HasMoveTarget = true;
		}

		private void StartSocialize(VoxelAI.World.World w, AISystem sys)
		{
			if (nearestAgent == null)
			{
				StartExplore(w, sys);
				return;
			}
			MoveTarget = nearestAgent.Body.Position;
			HasMoveTarget = true;
			SetAction(Act.Socialize, "visiting " + nearestAgent.Name, 14f);
			HasMoveTarget = true;
		}

		private void StartPlaceLight(VoxelAI.World.World w, AISystem sys)
		{
			if (!TakeItem(23, 1))
			{
				StartExplore(w, sys);
				return;
			}
			Vec3i vec3i = Body.Position.ToVec3i();
			for (int i = 0; i < 6; i++)
			{
				int x = vec3i.X;
				int num;
				switch (i)
				{
				case 0:
					num = 1;
					break;
				case 1:
					num = -1;
					break;
				default:
					num = 0;
					break;
				}
				int x2 = x + num;
				int z = vec3i.Z;
				int num2;
				switch (i)
				{
				case 2:
					num2 = 1;
					break;
				case 3:
					num2 = -1;
					break;
				default:
					num2 = 0;
					break;
				}
				int z2 = z + num2;
				if (w.CanPlaceAt(x2, vec3i.Y, z2))
				{
					w.PlaceBlock(x2, vec3i.Y, z2, 23);
					PlacementsTotal++;
					SessionScore += 2f;
					SetAction(Act.PlaceLight, "placed a light", 1f);
					Award(0.7f);
					return;
				}
			}
			AddItem(23, 1);
			StartExplore(w, sys);
		}

		private void StartDigDown(VoxelAI.World.World w, AISystem sys)
		{
			int x = (int)Math.Floor(Body.Position.X);
			int z = (int)Math.Floor(Body.Position.Z);
			int num = (int)Math.Floor(Body.Position.Y);
			if (num <= 12)
			{
				StartExplore(w, sys);
				return;
			}
			if (Brain.DangerAt(x, num - 1, z) > 0.2f)
			{
				StartExplore(w, sys);
				return;
			}
			TargetBlock = new Vec3i(x, num - 1, z);
			HasBlockTarget = true;
			MiningHardness = Blocks.Info((B)w.GetBlock(x, num - 1, z)).Hardness;
			SetAction(Act.DigDown, "digging a shaft", 16f);
			HasBlockTarget = true;
		}

		private void Execute(VoxelAI.World.World w, AISystem sys, float dt)
		{
			ActionTime += dt;
			switch (Action)
			{
			case Act.Mine:
			case Act.DigDown:
				ExecuteMine(w, sys, dt);
				break;
			case Act.Stuck:
				ExecuteUnstick(w, sys, dt);
				break;
			case Act.Eat:
				ExecuteRest(w, dt);
				break;
			case Act.SwimUp:
				Body.Velocity.Y = Math.Max(Body.Velocity.Y, 3.6f);
				Body.Velocity.X *= 0.88f;
				Body.Velocity.Z *= 0.88f;
				if (!Body.InWaterNow || Breath > 7.5f || ActionTime > 2.5f)
				{
					FinishAction(true);
				}
				break;
			case Act.Explore:
			case Act.Collect:
			case Act.Fight:
			case Act.Flee:
			case Act.Socialize:
				ExecuteTravel(w, sys, dt);
				break;
			default:
				if (ActionTime > ActionLimit)
				{
					FinishAction(true);
				}
				break;
			}
			if (ActionTime > ActionLimit)
			{
				FinishAction(false);
			}
		}

		private void ExecuteRest(VoxelAI.World.World w, float dt)
		{
			Body.Velocity.X *= 0.7f;
			Body.Velocity.Z *= 0.7f;
			Health = Math.Min(100f, Health + dt * (1.8f + Brain.SkillSurvival * 1.6f + G[11] * 0.8f));
			RestTimer += dt;
			if (Health >= 96f || ActionTime > 7f || hazardLavaNear)
			{
				if (Health >= 96f)
				{
					Award(0.5f);
					Brain.Note("rested and recovered");
					Brain.SkillSurvival = M.Clamp(Brain.SkillSurvival + 0.03f, 0f, 1f);
					FinishAction(true);
				}
				else
				{
					FinishAction(false);
				}
			}
		}

		private void ExecuteMine(VoxelAI.World.World w, AISystem sys, float dt)
		{
			if (!HasBlockTarget)
			{
				FinishAction(false);
				return;
			}
			Vec3 vec = new Vec3((float)TargetBlock.X + 0.5f, (float)TargetBlock.Y + 0.5f, (float)TargetBlock.Z + 0.5f);
			float num = Vec3.Distance(vec, Body.Eye);
			byte block = w.GetBlock(TargetBlock.X, TargetBlock.Y, TargetBlock.Z);
			if (block == 0 || block == 12 || block == 13)
			{
				Award(0.05f);
				StartExplore(w, sys);
				return;
			}
			if (num > 3.6f)
			{
				MoveTowards(w, vec, 3.6f * (0.8f + G[8] * 0.4f), dt);
				FaceTowards(vec);
				if (StuckCheck(num, dt))
				{
					Award(-0.08f);
					StartExplore(w, sys);
				}
				return;
			}
			LastTargetDist = float.MaxValue;
			FaceTowards(vec);
			Vec3i hit;
			Vec3i normal;
			B block2;
			bool flag = w.RaycastBlock(Body.Eye, (vec - Body.Eye).Normalized, num + 0.8f, out hit, out normal, out block2);
			if (!flag || hit != TargetBlock)
			{
				if (flag && block2 != B.Bedrock && Blocks.Info(block2).Hardness < 4f)
				{
					TargetBlock = hit;
					MiningHardness = Blocks.Info(block2).Hardness;
					MiningProgress = 0f;
				}
				else if (ActionTime > 6f + (1f - G[5]) * 8f)
				{
					StartExplore(w, sys);
				}
				else
				{
					MoveTowards(w, vec, 3.4f, dt);
				}
				return;
			}
			float num2 = (1.7f + Brain.SkillMining * 1.9f) * (1f + G[6] * 0.5f);
			MiningProgress += dt * num2;
			Body.Velocity.X *= 0.7f;
			Body.Velocity.Z *= 0.7f;
			if (!(MiningProgress >= MiningHardness))
			{
				return;
			}
			int num3 = OreValue(block);
			w.BreakBlock(TargetBlock.X, TargetBlock.Y, TargetBlock.Z);
			MinedTotal++;
			Brain.SkillMining = M.Clamp(Brain.SkillMining + 0.02f, 0f, 1f);
			SessionScore += 0.6f + (float)num3 * 2f;
			Award(0.35f + (float)num3 * 0.35f);
			Brain.Note("mined " + Blocks.Name((B)block));
			MiningProgress = 0f;
			if (Action == Act.DigDown)
			{
				int num4 = (int)Math.Floor(Body.Position.Y);
				TargetBlock = new Vec3i((int)Math.Floor(Body.Position.X), num4 - 1, (int)Math.Floor(Body.Position.Z));
				MiningHardness = Blocks.Info((B)w.GetBlock(TargetBlock.X, TargetBlock.Y, TargetBlock.Z)).Hardness;
				if (MiningHardness <= 0f)
				{
					Award(0.05f);
					StartExplore(w, sys);
				}
				else
				{
					Body.Velocity.Y = -1.5f;
				}
			}
			else
			{
				SenseAll(w, sys);
				if (bestResourceBlock != 0 && bestResourceScore > 0f)
				{
					StartMine(w, sys);
				}
				else
				{
					StartCollect(w, sys);
				}
			}
		}

		private void ExecuteTravel(VoxelAI.World.World w, AISystem sys, float dt)
		{
			if (!HasMoveTarget)
			{
				FinishAction(false);
				return;
			}
			float num = Vec3.Distance(Body.Position, MoveTarget);
			if (Action == Act.Fight)
			{
				if (nearestAgent == null || nearestAgent.Dead)
				{
					FinishAction(true);
					return;
				}
				MoveTarget = nearestAgent.Body.Position;
				num = Vec3.Distance(Body.Position, MoveTarget);
				if (num < 2.4f)
				{
					FaceTowards(nearestAgent.Body.Position);
					if (ActionTime > 0.6f)
					{
						float amount = 6f + G[0] * 10f + Brain.SkillCombat * 8f;
						sys.Damage(nearestAgent, amount, this, "combat");
						Brain.SkillCombat = M.Clamp(Brain.SkillCombat + 0.04f, 0f, 1f);
						ActionTime = 0f;
						Award(0.3f);
						if (nearestAgent.Dead)
						{
							Kills++;
							SessionScore += 12f;
							Award(1.4f);
							FinishAction(true);
						}
					}
					return;
				}
			}
			if (Action == Act.Flee && LastDamageAge < Age - 3f && !hazardLavaNear && num > 10f)
			{
				FinishAction(true);
			}
			else if (Action == Act.Collect && num < 1.6f)
			{
				FinishAction(true);
			}
			else if (Action == Act.Socialize && nearestAgent != null && num < 2.5f)
			{
				if (G[2] > 0.55f && CountOf(9) > 4)
				{
					TakeItem(9, 2);
					w.SpawnDrop((int)Body.Position.X, (int)Body.Position.Y + 1, (int)Body.Position.Z, 9, 2);
					Award(0.4f);
					Brain.Note("shared planks");
				}
				FinishAction(true);
			}
			else
			{
				float speed = 3f + G[8] * 1.8f + Brain.SkillNavigation * 0.8f;
				MoveTowards(w, MoveTarget, speed, dt);
				if (Action != Act.Explore && StuckCheck(num, dt))
				{
					Award(-0.06f);
					FinishAction(false);
				}
			}
		}

		private bool StuckCheck(float distance, float dt)
		{
			if (distance < LastTargetDist - 0.05f)
			{
				LastTargetDist = distance;
				StillTimer = 0f;
				return false;
			}
			LastTargetDist = distance;
			StillTimer += dt;
			float num = 2.5f + (1f - G[5]) * 4f + Brain.SkillNavigation * 3f;
			if (StillTimer > num)
			{
				StillTimer = 0f;
				LastTargetDist = float.MaxValue;
				return true;
			}
			return false;
		}

		private void ExecuteBuild(VoxelAI.World.World w, AISystem sys, float dt)
		{
			if (Plan == null)
			{
				Action = Act.Idle;
				return;
			}
			ActionTime += dt;
			if (PlanIndex >= Plan.Count)
			{
				StructuresBuilt++;
				SessionScore += 10f;
				Award(2.2f);
				Brain.SkillBuilding = M.Clamp(Brain.SkillBuilding + 0.12f, 0f, 1f);
				Brain.Note("finished a shelter");
				if (Species != null)
				{
					Species.NightShelter = M.Clamp(Species.NightShelter + 0.03f, 0f, 1f);
				}
				Plan = null;
				FinishAction(true);
				return;
			}
			int num = 0;
			Placement placement;
			do
			{
				if (Plan[PlanIndex].Done)
				{
					PlanIndex++;
				}
				if (PlanIndex >= Plan.Count)
				{
					ExecuteBuild(w, sys, dt);
					return;
				}
				placement = Plan[PlanIndex];
			}
			while (placement.Done && num++ < Plan.Count);
			Vec3 vec = new Vec3((float)placement.Pos.X + 0.5f, (float)placement.Pos.Y + 0.5f, (float)placement.Pos.Z + 0.5f);
			float num2 = Vec3.Distance(vec, Body.Eye);
			if (num2 > 3.8f)
			{
				MoveTowards(w, vec, 3.4f, dt);
				FaceTowards(vec);
				if (ActionTime > ActionLimit)
				{
					Action = Act.Idle;
					Plan = null;
				}
				return;
			}
			FaceTowards(vec);
			if (w.CanPlaceAt(placement.Pos.X, placement.Pos.Y, placement.Pos.Z))
			{
				int num3 = CountOf(placement.Block);
				if (num3 > 0)
				{
					w.PlaceBlock(placement.Pos.X, placement.Pos.Y, placement.Pos.Z, placement.Block);
					TakeItem(placement.Block, 1);
					Placement value = placement;
					value.Done = true;
					Plan[PlanIndex] = value;
					PlanIndex++;
					PlacementsTotal++;
					SessionScore += 1.2f;
					Award(0.45f);
				}
				else
				{
					bool flag = false;
					foreach (KeyValuePair<byte, int> item in Inventory)
					{
						if (item.Value <= 0 || (item.Key == 23 && CountOf(item.Key) <= 1))
						{
							continue;
						}
						w.PlaceBlock(placement.Pos.X, placement.Pos.Y, placement.Pos.Z, item.Key);
						TakeItem(item.Key, 1);
						flag = true;
						PlacementsTotal++;
						Award(0.3f);
						break;
					}
					if (!flag)
					{
						Plan = null;
						FinishAction(false);
						return;
					}
					Placement value2 = placement;
					value2.Done = true;
					Plan[PlanIndex] = value2;
					PlanIndex++;
				}
			}
			else
			{
				Placement value3 = placement;
				value3.Done = true;
				Plan[PlanIndex] = value3;
				PlanIndex++;
			}
			if (ActionTime > ActionLimit)
			{
				Plan = null;
				FinishAction(false);
			}
		}

		private void FinishAction(bool good)
		{
			float num = ActionReward;
			if (Action == Act.Explore)
			{
				num += 0.15f;
				SessionScore += 0.8f;
			}
			if (num != 0f || good)
			{
				Brain.Reward(Action, num + ((!good) ? (-0.05f) : 0.12f));
			}
			ActionReward = 0f;
			Action = Act.Idle;
			Status = "idle";
			ActionTime = 0f;
			HasMoveTarget = false;
			HasBlockTarget = false;
			StillTimer = 0f;
			LastTargetDist = float.MaxValue;
		}

		public void MoveTowards(VoxelAI.World.World w, Vec3 target, float speed, float dt)
		{
			Vec3 a = target - Body.Position;
			a.Y = 0f;
			float length = a.Length;
			if (length < 0.35f)
			{
				Body.Velocity.X *= 0.6f;
				Body.Velocity.Z *= 0.6f;
				return;
			}
			Vec3 a2 = a / length;
			int x = (int)Math.Floor(Body.Position.X + a2.X * 1f);
			int z = (int)Math.Floor(Body.Position.Z + a2.Z * 1f);
			int num = (int)Math.Floor(Body.Position.Y);
			byte block = w.GetBlock(x, num, z);
			byte block2 = w.GetBlock(x, num - 1, z);
			if (block == 13 || block2 == 13)
			{
				float num2 = ((!(Body.Yaw % 2f < 1f)) ? (-1f) : 1f);
				Vec3 b = new Vec3((0f - a2.Z) * num2, 0f, a2.X * num2);
				a2 = (a2 * 0.4f + b).Normalized;
				Brain.MarkDanger(x, num, z, 0.25f);
			}
			if (waterNear && Species.WaterCaution > 0.35f)
			{
				byte block3 = w.GetBlock(x, num - 1, z);
				if (block3 == 12 && block == 0)
				{
					a2 *= 0.5f;
				}
			}
			float num3 = speed;
			if (Body.InWaterNow)
			{
				num3 *= 0.65f;
			}
			if (G[8] > 0.6f && (int)ActionTime % 6 == 0)
			{
				num3 *= 1.25f;
			}
			Vec3 vec = a2 * num3;
			Body.Velocity.X = M.ExpSmooth(Body.Velocity.X, vec.X, 9f, dt);
			Body.Velocity.Z = M.ExpSmooth(Body.Velocity.Z, vec.Z, 9f, dt);
			if (block != 0 && block != 12 && Blocks.Info((B)block).Solid && Body.OnGround)
			{
				if (Blocks.Info((B)w.GetBlock(x, num + 1, z)).Solid)
				{
					for (int i = 0; i < SteerAngles.Length; i++)
					{
						float num5 = (float)Math.Cos(SteerAngles[i]);
						float num6 = (float)Math.Sin(SteerAngles[i]);
						Vec3 vec2 = new Vec3(a2.X * num5 - a2.Z * num6, 0f, a2.X * num6 + a2.Z * num5);
						int x2 = (int)Math.Floor(Body.Position.X + vec2.X * 1f);
						int z2 = (int)Math.Floor(Body.Position.Z + vec2.Z * 1f);
						byte block4 = w.GetBlock(x2, num, z2);
						byte block5 = w.GetBlock(x2, num + 1, z2);
						if ((block4 == 0 || block4 == 12) && !Blocks.Info((B)block5).Solid)
						{
							a2 = vec2.Normalized;
							BlockInfo blockInfo = Blocks.Info((B)block5);
							if (!blockInfo.Solid)
							{
								Body.Velocity.X = a2.X * num3;
								Body.Velocity.Z = a2.Z * num3;
							}
							break;
						}
					}
				}
				Body.Velocity.Y = 8.4f;
			}
			int num7 = w.GroundY(x, z);
			int num8 = (int)Math.Floor(Body.Position.Y) - num7;
			if (num8 > 3 && Body.OnGround)
			{
				float cliffCaution = Species.CliffCaution;
				Body.Velocity.X *= Math.Max(0f, 1f - cliffCaution * 0.25f);
				Body.Velocity.Z *= Math.Max(0f, 1f - cliffCaution * 0.25f);
			}
			if (Body.InWaterNow)
			{
				Body.Velocity.Y = Math.Max(Body.Velocity.Y, 1.6f);
			}
			float target2 = (float)Math.Atan2(a2.X, a2.Z);
			Body.Yaw = M.ApproachAngle(Body.Yaw, target2, 6f * dt);
			Body.Pitch = M.ExpSmooth(Body.Pitch, 0f, 5f, dt);
		}

		public void FaceTowards(Vec3 target)
		{
			Vec3 vec = target - Body.Eye;
			float num = (float)Math.Sqrt(vec.X * vec.X + vec.Z * vec.Z);
			float target2 = (float)Math.Atan2(vec.X, vec.Z);
			float target3 = (float)Math.Atan2(vec.Y, num);
			Body.Yaw = M.ApproachAngle(Body.Yaw, target2, 0.5f);
			Body.Pitch = M.ApproachAngle(Body.Pitch, target3, 0.35f);
		}

		public void Damage(float amount, string cause, VoxelAI.World.World w)
		{
			if (!Dead && !(amount <= 0f))
			{
				Health -= amount;
				DamageTaken += amount;
				LastDamageAge = Age;
				LastDamageCause = cause;
				if (cause == "combat")
				{
					Brain.SkillSurvival = M.Clamp(Brain.SkillSurvival + 0.01f, 0f, 1f);
				}
				if (Health <= 0f)
				{
					Kill(cause, w);
				}
			}
		}

		public void Kill(string cause, VoxelAI.World.World w)
		{
			if (Dead)
			{
				return;
			}
			Dead = true;
			Deaths++;
			DeathCause = cause;
			RespawnTimer = 4f;
			Brain.Note("died: " + cause);
			if (cause == "lava")
			{
				Vec3i vec3i = Body.Position.ToVec3i();
				for (int i = -2; i <= 2; i++)
				{
					for (int j = -1; j <= 1; j++)
					{
						for (int k = -2; k <= 2; k++)
						{
							Brain.MarkDanger(vec3i.X + i, vec3i.Y + j, vec3i.Z + k, 0.8f);
						}
					}
				}
			}
			Species.RecordDeath(cause);
			foreach (KeyValuePair<byte, int> item in Inventory)
			{
				w.SpawnDrop((int)Math.Floor(Body.Position.X), (int)Math.Floor(Body.Position.Y) + 1, (int)Math.Floor(Body.Position.Z), item.Key, item.Value);
			}
			Inventory.Clear();
		}

		public bool IsNight(VoxelAI.World.World w)
		{
			return w.Daylight < 0.45f;
		}
	}
}
