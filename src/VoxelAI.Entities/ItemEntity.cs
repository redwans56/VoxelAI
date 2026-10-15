using VoxelAI.Core;
using VoxelAI.World;

namespace VoxelAI.Entities
{
	public class ItemEntity : Entity
	{
		public byte Block;

		public int Count = 1;

		public float Life = 300f;

		public float Spin;

		public ItemEntity(byte block, Vec3 pos, int count)
		{
			Block = block;
			Count = count;
			Position = pos;
			Size = new Vec3(0.35f, 0.35f, 0.35f);
			Rng rng = new Rng((int)(pos.X * 31.7f) ^ (int)(pos.Z * 17.3f) ^ (block * 7919));
			Velocity = new Vec3(rng.RangeF(-0.8f, 0.8f), 2.2f, rng.RangeF(-0.8f, 0.8f));
		}

		public override void Update(VoxelAI.World.World w, float dt)
		{
			Life -= dt;
			Spin += dt * 2.4f;
			base.Update(w, dt);
			if (OnGround)
			{
				Velocity.X *= 0.62f;
				Velocity.Z *= 0.62f;
			}
			if (Life <= 0f || Position.Y < -20f)
			{
				Alive = false;
			}
		}
	}
}
