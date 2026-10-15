using System;
using VoxelAI.Core;
using VoxelAI.World;

namespace VoxelAI.Entities
{
	public class Creature : Entity
	{
		public string Name = "Creature";

		public byte Tint = 200;

		public float BodyR = 0.4f;

		public float BodyG = 0.7f;

		public float BodyB = 0.5f;

		public float HeadR = 0.7f;

		public float HeadG = 0.85f;

		public float HeadB = 0.7f;

		public float Yaw;

		public float Pitch;

		public float HeadBob;

		public float WalkPhase;

		public Vec3 LookDir
		{
			get
			{
				float num = (float)Math.Cos(Pitch);
				return new Vec3((float)Math.Sin(Yaw) * num, (float)Math.Sin(Pitch), (float)Math.Cos(Yaw) * num).Normalized;
			}
		}

		public Vec3 Eye
		{
			get
			{
				return Position.Add(0f, Size.Y * 0.42f, 0f);
			}
		}

		public void PickColors(Rng rng, float r, float g, float b)
		{
			BodyR = r;
			BodyG = g;
			BodyB = b;
			HeadR = M.Clamp(r * 1.3f + 0.1f, 0f, 1f);
			HeadG = M.Clamp(g * 1.3f + 0.1f, 0f, 1f);
			HeadB = M.Clamp(b * 1.3f + 0.1f, 0f, 1f);
			Tint = (byte)rng.Range(180, 255);
		}

		public override void Update(VoxelAI.World.World w, float dt)
		{
			base.Update(w, dt);
			float num = (float)Math.Sqrt(Velocity.X * Velocity.X + Velocity.Z * Velocity.Z);
			WalkPhase += num * dt * 2.2f;
			HeadBob = (float)Math.Sin(WalkPhase * 2f) * Math.Min(1f, num / 4f) * 0.04f;
		}
	}
}
