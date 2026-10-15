using VoxelAI.Core;
using VoxelAI.World;

namespace VoxelAI.Entities
{
	public abstract class Entity
	{
		public Vec3 Position;

		public Vec3 Velocity;

		public Vec3 Size = new Vec3(0.6f, 1.8f, 0.6f);

		public bool Alive = true;

		public bool OnGround;

		public float Age;

		public bool InWaterNow;

		public bool InLavaNow;

		public bool OnGroundPrev;

		public Aabb Bounds
		{
			get
			{
				return Aabb.FromCenter(Position, Size);
			}
		}

		public virtual void Update(VoxelAI.World.World w, float dt)
		{
			Age += dt;
			OnGroundPrev = OnGround;
			InWaterNow = Physics.InLiquid(w, Position, Size * 0.5f, out InLavaNow);
			float num = ((!InWaterNow) ? (-26f) : (-6.5f));
			Velocity.Y += num * dt;
			if (InWaterNow)
			{
				Velocity.Y *= 0.82f;
				Velocity.X *= 0.86f;
				Velocity.Z *= 0.86f;
			}
			if (Velocity.Y < -58f)
			{
				Velocity.Y = -58f;
			}
			Vec3 pos = Position;
			Vec3 vel = Velocity;
			bool onGround;
			bool hitWall;
			Physics.Move(w, ref pos, ref vel, Size, dt, out onGround, out hitWall);
			Position = pos;
			Velocity = vel;
			OnGround = onGround;
		}
	}
}
