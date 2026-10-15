using System;
using System.Collections.Generic;
using System.Windows.Forms;
using VoxelAI.Core;
using VoxelAI.Entities;
using VoxelAI.World;

namespace VoxelAI.Game
{
	public class Player
	{
		public Vec3 Position = new Vec3(0f, 80f, 0f);

		public Vec3 Velocity;

		public float Yaw;

		public float Pitch;

		public float Health = 100f;

		public bool OnGround;

		public bool InWater;

		public bool Creative = true;

		public bool Flying;

		public float WalkPhase;

		public float Bob;

		public float Reach = 5.2f;

		public Vec3i TargetBlock;

		public Vec3i TargetNormal;

		public bool HasTarget;

		public float MineProgress;

		public float Swing;

		public float FallStart = -1f;

		public float DrownTimer;

		public float HurtTimer;

		public int Deaths;

		public byte[] Hotbar = new byte[9];

		public int HotbarIndex;

		public Dictionary<byte, int> Inventory = new Dictionary<byte, int>();

		public Vec3 Eye
		{
			get
			{
				return Position.Add(0f, 1.62f, 0f);
			}
		}

		public Vec3 LookDir
		{
			get
			{
				float num = (float)Math.Cos(Pitch);
				return new Vec3((float)Math.Sin(Yaw) * num, (float)Math.Sin(Pitch), (float)Math.Cos(Yaw) * num).Normalized;
			}
		}

		public byte SelectedBlock
		{
			get
			{
				return Hotbar[M.Clamp(HotbarIndex, 0, 8)];
			}
		}

		public float MiningSpeed
		{
			get
			{
				return (!Creative) ? 1.35f : 4f;
			}
		}

		public event Action<string> Died;

		public Player()
		{
			Hotbar[0] = 1;
			Hotbar[1] = 2;
			Hotbar[2] = 3;
			Hotbar[3] = 4;
			Hotbar[4] = 9;
			Hotbar[5] = 5;
			Hotbar[6] = 10;
			Hotbar[7] = 23;
			Hotbar[8] = 11;
		}

		public int CountOf(byte b)
		{
			int value;
			return Inventory.TryGetValue(b, out value) ? value : 0;
		}

		public void AddItem(byte b, int n)
		{
			if (b == 0 || n <= 0)
			{
				return;
			}
			Inventory[b] = CountOf(b) + n;
			if (Creative)
			{
				return;
			}
			for (int i = 0; i < 9; i++)
			{
				if (Hotbar[i] == 0)
				{
					Hotbar[i] = b;
					break;
				}
			}
		}

		public bool Consume(byte b, int n)
		{
			if (Creative)
			{
				return true;
			}
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
			if (CountOf(b) == 0)
			{
				for (int i = 0; i < 9; i++)
				{
					if (Hotbar[i] == b)
					{
						Hotbar[i] = 0;
					}
				}
			}
			return true;
		}

		public void DropOne(VoxelAI.World.World w)
		{
			byte selectedBlock = SelectedBlock;
			if (selectedBlock != 0 && !Creative)
			{
				Consume(selectedBlock, 1);
				w.SpawnDrop((int)Math.Floor(Position.X), (int)Math.Floor(Position.Y) + 1, (int)Math.Floor(Position.Z), selectedBlock, 1);
			}
		}

		public void HandleInput(VoxelAI.World.World w, InputState input, float dt, out bool jump)
		{
			jump = false;
			float num = (Flying ? 12f : ((!input.IsDown(Keys.ShiftKey)) ? 5.2f : 8.5f));
			if (InWater)
			{
				num *= 0.6f;
			}
			float num2 = (float)Math.Sin(Yaw);
			float num3 = (float)Math.Cos(Yaw);
			float num4 = num3;
			float num5 = 0f - num2;
			float num6 = 0f;
			float num7 = 0f;
			if (input.IsDown(Keys.W))
			{
				num6 += num2;
				num7 += num3;
			}
			if (input.IsDown(Keys.S))
			{
				num6 -= num2;
				num7 -= num3;
			}
			if (input.IsDown(Keys.D))
			{
				num6 += num4;
				num7 += num5;
			}
			if (input.IsDown(Keys.A))
			{
				num6 -= num4;
				num7 -= num5;
			}
			float num8 = (float)Math.Sqrt(num6 * num6 + num7 * num7);
			if (num8 > 0.001f)
			{
				num6 /= num8;
				num7 /= num8;
			}
			Vec3 vec = new Vec3(num6 * num, Velocity.Y, num7 * num);
			Velocity.X = M.ExpSmooth(Velocity.X, vec.X, (!Flying) ? 14f : 12f, dt);
			Velocity.Z = M.ExpSmooth(Velocity.Z, vec.Z, (!Flying) ? 14f : 12f, dt);
			if (input.IsDown(Keys.Space) || input.Pressed(Keys.Space))
			{
				if (Flying)
				{
					Velocity.Y = 8f;
				}
				else if (OnGround)
				{
					Velocity.Y = 8.6f;
					jump = true;
				}
				else if (InWater)
				{
					Velocity.Y = 4.2f;
				}
			}
			if (input.IsDown(Keys.ControlKey) && Flying)
			{
				Velocity.Y = -8f;
			}
			if (Flying)
			{
				Velocity.Y = M.ExpSmooth(Velocity.Y, input.IsDown(Keys.Space) ? 10f : ((!input.IsDown(Keys.ControlKey)) ? 0f : (-10f)), 10f, dt);
				Velocity.Y = M.Clamp(Velocity.Y, -14f, 14f);
			}
		}

		public void StepPhysics(VoxelAI.World.World w, float dt)
		{
			Vec3 vec = new Vec3(0.6f, 1.8f, 0.6f);
			bool lava;
			InWater = Physics.InLiquid(w, Position, vec * 0.5f, out lava);
			if (!Flying)
			{
				if (InWater)
				{
					Velocity.Y += -9f * dt;
					Velocity.Y *= 0.9f;
					if (Velocity.Y < -6f)
					{
						Velocity.Y = -6f;
					}
				}
				else
				{
					Velocity.Y += -26f * dt;
					if (Velocity.Y < -58f)
					{
						Velocity.Y = -58f;
					}
				}
			}
			bool onGround = OnGround;
			float y = Velocity.Y;
			Vec3 pos = Position;
			Vec3 vel = Velocity;
			if (Flying)
			{
				pos += vel * dt;
				OnGround = false;
				Position = pos;
			}
			else
			{
				bool onGround2;
				bool hitWall;
				Physics.Move(w, ref pos, ref vel, vec, dt, out onGround2, out hitWall);
				Position = pos;
				Velocity = vel;
				OnGround = onGround2;
			}
			if (!Flying && !InWater)
			{
				if (!onGround && OnGround && y < -13f && FallStart < 0f)
				{
					Hurt((0f - y - 13f) * 2.2f, "fall");
				}
				if (!OnGround && Velocity.Y < -0.1f && FallStart < 0f)
				{
					FallStart = Position.Y;
				}
				if (OnGround)
				{
					FallStart = -1f;
				}
			}
			if (lava)
			{
				Hurt(22f * dt, "lava");
			}
			if (InWater)
			{
				float num = Position.Y + 1.5f;
				if (w.GetBlock((int)Math.Floor(Position.X), (int)Math.Floor(num), (int)Math.Floor(Position.Z)) == 12)
				{
					DrownTimer += dt;
					if (DrownTimer > 12f)
					{
						Hurt(6f * dt, "water");
					}
				}
				else
				{
					DrownTimer = 0f;
				}
			}
			else
			{
				DrownTimer = 0f;
			}
			Aabb aabb = Aabb.FromCenter(Position, vec);
			for (int i = -1; i <= 1; i++)
			{
				for (int j = -1; j <= 1; j++)
				{
					int x = (int)Math.Floor(Position.X) + i;
					int z = (int)Math.Floor(Position.Z) + j;
					for (int k = (int)Math.Floor(aabb.Min.Y); k <= (int)Math.Floor(aabb.Max.Y); k++)
					{
						if (w.GetB(x, k, z) == B.Cactus)
						{
							Hurt(4f * dt, "cactus");
						}
					}
				}
			}
			if (HurtTimer > 0f)
			{
				HurtTimer -= dt;
			}
			float num2 = (float)Math.Sqrt(Velocity.X * Velocity.X + Velocity.Z * Velocity.Z);
			WalkPhase += num2 * dt * 2f;
			Bob = (float)Math.Sin(WalkPhase * 2f) * Math.Min(1f, num2 / 4.5f) * 0.05f;
		}

		public void Look(InputState input, float sensitivity)
		{
			Yaw -= (float)input.MouseDeltaX * sensitivity;
			Pitch -= (float)input.MouseDeltaY * sensitivity;
			Pitch = M.Clamp(Pitch, -1.55f, 1.55f);
			Yaw = M.WrapAngle(Yaw);
		}

		public void UpdateTarget(VoxelAI.World.World w, float dt, bool mining)
		{
			Vec3i hit;
			Vec3i normal;
			B block;
			if (w.RaycastBlock(Eye, LookDir, Reach, out hit, out normal, out block))
			{
				TargetBlock = hit;
				TargetNormal = normal;
				HasTarget = true;
				if (!mining || block == B.Bedrock)
				{
					MineProgress = 0f;
					return;
				}
				if (Creative)
				{
					w.BreakBlock(hit.X, hit.Y, hit.Z);
					MineProgress = 0f;
					Swing = 1f;
					return;
				}
				MineProgress += dt * MiningSpeed;
				if (MineProgress >= Blocks.Info(block).Hardness)
				{
					w.BreakBlock(hit.X, hit.Y, hit.Z);
					MineProgress = 0f;
					Swing = 1f;
				}
			}
			else
			{
				HasTarget = false;
				MineProgress = 0f;
			}
		}

		public void PlaceAtTarget(VoxelAI.World.World w)
		{
			if (!HasTarget)
			{
				return;
			}
			byte selectedBlock = SelectedBlock;
			if (selectedBlock == 0)
			{
				return;
			}
			int num = TargetBlock.X + TargetNormal.X;
			int num2 = TargetBlock.Y + TargetNormal.Y;
			int num3 = TargetBlock.Z + TargetNormal.Z;
			if (w.CanPlaceAt(num, num2, num3) && Consume(selectedBlock, 1))
			{
				w.PlaceBlock(num, num2, num3, selectedBlock);
				Swing = 1f;
				if (Aabb.FromCenter(Position, new Vec3(0.6f, 1.8f, 0.6f)).Intersects(new Aabb(new Vec3(num, num2, num3), new Vec3(num + 1, num2 + 1, num3 + 1))))
				{
					Position.Y += 1.2f;
				}
			}
		}

		public void Hurt(float amount, string cause)
		{
			if (!(amount <= 0f))
			{
				Health -= amount;
				HurtTimer = 0.35f;
				if (Health <= 0f)
				{
					Die(cause);
				}
			}
		}

		private void Die(string cause)
		{
			Health = 100f;
			Deaths++;
			if (this.Died != null)
			{
				this.Died(cause);
			}
		}

		public void Respawn(VoxelAI.World.World w)
		{
			Vec3 vec = (Position = w.FindSpawnPoint());
			Velocity = Vec3.Zero;
			Health = 100f;
			Flying = false;
		}
	}
}
