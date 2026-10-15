using System;
using System.Collections.Generic;
using System.Threading;
using VoxelAI.Core;
using VoxelAI.Entities;

namespace VoxelAI.World
{
	public class World : ILightVolume
	{
		public struct Particle
		{
			public Vec3 Pos;

			public Vec3 Vel;

			public float Life;

			public float MaxLife;

			public float R;

			public float G;

			public float B;

			public float Size;
		}

		public const int SEA_LEVEL = 62;

		public readonly int Seed;

		public readonly TerrainGenerator Terrain;

		public readonly LightEngine Light = new LightEngine();

		public string SaveDirectory;

		public int ChunkRadius = 7;

		private readonly Dictionary<long, Chunk> chunks = new Dictionary<long, Chunk>();

		private readonly Queue<Chunk> genQueue = new Queue<Chunk>();

		private readonly Queue<Chunk> lightQueue = new Queue<Chunk>();

		private readonly List<Chunk> lightDone = new List<Chunk>();

		private readonly object workLock = new object();

		private Thread[] workers;

		private volatile bool shutdown;

		private readonly List<ChunkStaging> stagingPool = new List<ChunkStaging>();

		private readonly object stagingLock = new object();

		private readonly List<Chunk> dirtyMeshes = new List<Chunk>();

		private readonly HashSet<long> dirtySet = new HashSet<long>();

		private readonly HashSet<long> lightTouched = new HashSet<long>();

		public readonly List<Creature> Creatures = new List<Creature>();

		public readonly List<ItemEntity> Items = new List<ItemEntity>();

		public readonly List<Particle> Particles = new List<Particle>(512);

		public const int MaxParticles = 700;

		public float TimeOfDay = 0.3f;

		public float DayLength = 720f;

		public Weather CurrentWeather;

		private float weatherTimer = 200f;

		public float WeatherIntensity;

		public float Daylight = 1f;

		public int BakeLevel = 8;

		public float SkyR = 0.52f;

		public float SkyG = 0.72f;

		public float SkyB = 1f;

		public float FogR = 0.66f;

		public float FogG = 0.79f;

		public float FogB = 0.96f;

		public bool FrozenTime;

		public int BlocksBroken;

		public int BlocksPlaced;

		public Rng Random;

		private readonly List<int> seedScratch = new List<int>(8192);

		private float streamTimer;

		private readonly List<Chunk> lightDoneInternal = new List<Chunk>();

		private bool gravityBusy;

		private readonly Queue<Vec3i> fluidQueue = new Queue<Vec3i>();

		private readonly Dictionary<Vec3i, byte> fluidLevels = new Dictionary<Vec3i, byte>();

		private readonly HashSet<Vec3i> fluidSeen = new HashSet<Vec3i>();

		private float fluidTimer;

		private int flowBudgetUsed;

		public int LoadedChunkCount
		{
			get
			{
				return chunks.Count;
			}
		}

		public int MaxY
		{
			get
			{
				return 128;
			}
		}

		public event Action<World> DaylightChanged;

		public event Action<Chunk> ChunkUnloaded;

		public World(int seed, string saveDir)
		{
			Seed = seed;
			Terrain = new TerrainGenerator(seed);
			SaveDirectory = saveDir;
			Random = new Rng(seed ^ 0x5F3A2B1);
			StartWorkers();
		}

		public void SpawnParticles(Vec3 pos, byte block, int count, float spread, float up)
		{
			float r;
			float g;
			float bl;
			BlockColor.Get(block, out r, out g, out bl);
			for (int i = 0; i < count; i++)
			{
				if (Particles.Count >= 700)
				{
					Particles.RemoveAt(0);
				}
				Particle item = default(Particle);
				item.Pos = pos;
				item.Vel = new Vec3(Random.RangeF(0f - spread, spread), Random.RangeF(up * 0.2f, up), Random.RangeF(0f - spread, spread));
				item.MaxLife = Random.RangeF(0.35f, 1.1f);
				item.Life = item.MaxLife;
				item.R = r * Random.RangeF(0.8f, 1.15f);
				item.G = g * Random.RangeF(0.8f, 1.15f);
				item.B = bl * Random.RangeF(0.8f, 1.15f);
				item.Size = Random.RangeF(0.055f, 0.13f);
				Particles.Add(item);
			}
		}

		private void UpdateParticles(float dt)
		{
			for (int num = Particles.Count - 1; num >= 0; num--)
			{
				Particle value = Particles[num];
				value.Life -= dt;
				if (value.Life <= 0f)
				{
					Particles.RemoveAt(num);
				}
				else
				{
					value.Vel.Y -= 22f * dt;
					value.Pos += value.Vel * dt;
					if (IsSolidAt((int)Math.Floor(value.Pos.X), (int)Math.Floor(value.Pos.Y), (int)Math.Floor(value.Pos.Z)) && value.Vel.Y < 0f)
					{
						value.Vel = new Vec3(value.Vel.X * 0.4f, 0f, value.Vel.Z * 0.4f);
						value.Pos = value.Pos.Add(0f, 0.05f, 0f);
					}
					Particles[num] = value;
				}
			}
		}

		public void SpawnAmbientParticle(Vec3 pos, float r, float g, float b, float life, float size)
		{
			if (Particles.Count < 700)
			{
				Particle item = default(Particle);
				item.Pos = pos;
				item.Vel = new Vec3(0f, 0f, 0f);
				item.MaxLife = life;
				item.Life = life;
				item.R = r;
				item.G = g;
				item.B = b;
				item.Size = size;
				Particles.Add(item);
			}
		}

		public static long Key(int cx, int cz)
		{
			return ((long)cx << 32) ^ (uint)cz;
		}

		public Chunk GetChunk(int cx, int cz)
		{
			Chunk value;
			chunks.TryGetValue(Key(cx, cz), out value);
			return value;
		}

		public bool TryGetChunk(int cx, int cz, out Chunk c)
		{
			return chunks.TryGetValue(Key(cx, cz), out c);
		}

		public List<Chunk> SnapshotChunks()
		{
			return new List<Chunk>(chunks.Values);
		}

		public byte GetBlock(int x, int y, int z)
		{
			if (y < 0 || y >= 128)
			{
				return 0;
			}
			Chunk value;
			if (!chunks.TryGetValue(Key(M.FloorDiv(x, 16), M.FloorDiv(z, 16)), out value))
			{
				return 0;
			}
			return value.Blocks[Chunk.Index(x - value.BaseX, y, z - value.BaseZ)];
		}

		public B GetB(int x, int y, int z)
		{
			return (B)GetBlock(x, y, z);
		}

		public bool IsSolidAt(int x, int y, int z)
		{
			byte block = GetBlock(x, y, z);
			return block != 0 && Blocks.Info((B)block).Solid;
		}

		public bool IsOpaqueAt(int x, int y, int z)
		{
			byte block = GetBlock(x, y, z);
			return block != 0 && Blocks.Info((B)block).Opaque;
		}

		public bool IsWaterAt(int x, int y, int z)
		{
			return GetBlock(x, y, z) == 12;
		}

		public int SurfaceY(int x, int z)
		{
			Chunk chunk = GetChunk(M.FloorDiv(x, 16), M.FloorDiv(z, 16));
			if (chunk == null || !chunk.Generated)
			{
				return 62;
			}
			int x2 = x - chunk.BaseX;
			int z2 = z - chunk.BaseZ;
			for (int num = 127; num >= 0; num--)
			{
				byte b = chunk.Blocks[Chunk.Index(x2, num, z2)];
				if (b != 0 && b != 12)
				{
					return num;
				}
			}
			return 0;
		}

		public int GroundY(int x, int z)
		{
			Chunk chunk = GetChunk(M.FloorDiv(x, 16), M.FloorDiv(z, 16));
			if (chunk == null || !chunk.Generated)
			{
				return 62;
			}
			int x2 = x - chunk.BaseX;
			int z2 = z - chunk.BaseZ;
			for (int num = 127; num >= 0; num--)
			{
				byte b = chunk.Blocks[Chunk.Index(x2, num, z2)];
				if (b != 0 && b != 12 && b != 8 && b != 13)
				{
					return num;
				}
			}
			return 0;
		}

		public Biome BiomeAt(int x, int z)
		{
			return Terrain.BiomeAt(x, z);
		}

		public Vec3 FindSpawnPoint()
		{
			for (int i = 0; i < 260; i += 4)
			{
				for (int j = 0; j < 16; j++)
				{
					double num = (double)((float)j * ((float)Math.PI * 2f)) / 16.0;
					int num2 = (int)(Math.Cos(num) * (double)i);
					int num3 = (int)(Math.Sin(num) * (double)i);
					EnsureChunkLoaded(M.FloorDiv(num2, 16), M.FloorDiv(num3, 16), true);
					int num4 = SurfaceY(num2, num3);
					if (num4 > 63)
					{
						B b = GetB(num2, num4, num3);
						if ((b == B.Grass || b == B.Sand || b == B.Snow || b == B.Stone) && (GetBlock(num2, num4 + 1, num3) == 0 || GetB(num2, num4 + 1, num3) == B.Leaves))
						{
							return new Vec3((float)num2 + 0.5f, (float)num4 + 1.3f, (float)num3 + 0.5f);
						}
					}
				}
			}
			return new Vec3(0.5f, 74f, 0.5f);
		}

		private void StartWorkers()
		{
			int num = Math.Max(1, Environment.ProcessorCount - 1);
			workers = new Thread[num];
			for (int i = 0; i < num; i++)
			{
				workers[i] = new Thread(WorkerLoop);
				workers[i].IsBackground = true;
				workers[i].Name = "VoxelWorker" + i;
				workers[i].Start();
			}
		}

		public void Shutdown()
		{
			shutdown = true;
			lock (workLock)
			{
				Monitor.PulseAll(workLock);
			}
			if (workers == null)
			{
				return;
			}
			Thread[] array = workers;
			foreach (Thread thread in array)
			{
				if (thread != null)
				{
					thread.Join(400);
				}
			}
		}

		private ChunkStaging RentStaging()
		{
			lock (stagingLock)
			{
				if (stagingPool.Count > 0)
				{
					ChunkStaging result = stagingPool[stagingPool.Count - 1];
					stagingPool.RemoveAt(stagingPool.Count - 1);
					return result;
				}
			}
			return new ChunkStaging();
		}

		private void ReturnStaging(ChunkStaging s)
		{
			lock (stagingLock)
			{
				stagingPool.Add(s);
			}
		}

		private void WorkerLoop()
		{
			while (!shutdown)
			{
				Chunk chunk = null;
				Chunk chunk2 = null;
				lock (workLock)
				{
					if (genQueue.Count > 0)
					{
						chunk = genQueue.Dequeue();
						chunk.InGenQueue = false;
					}
					else if (lightQueue.Count > 0)
					{
						chunk2 = lightQueue.Dequeue();
						chunk2.InLightQueue = false;
					}
					else
					{
						Monitor.Wait(workLock, 50);
					}
				}
				if (shutdown)
				{
					break;
				}
				try
				{
					if (chunk != null)
					{
						Terrain.Generate(chunk);
						chunk.Generated = true;
						chunk.Dirty = true;
					}
					else if (chunk2 != null)
					{
						ChunkStaging chunkStaging = RentStaging();
						chunkStaging.Pull(this, chunk2);
						chunkStaging.Compute(chunk2);
						chunkStaging.CopyTo(chunk2);
						chunk2.BoundarySeeds.Clear();
						chunkStaging.CollectBoundarySeeds(chunk2, chunk2.BoundarySeeds);
						ReturnStaging(chunkStaging);
						Thread.MemoryBarrier();
						chunk2.Lit = true;
						chunk2.Dirty = true;
						lock (workLock)
						{
							lightDoneInternal.Add(chunk2);
						}
					}
				}
				catch (Exception)
				{
					if (chunk != null)
					{
						chunk.Generated = true;
					}
					if (chunk2 != null)
					{
						chunk2.Lit = true;
					}
				}
			}
		}

		public Chunk EnsureChunkLoaded(int cx, int cz, bool generateNow)
		{
			Chunk value;
			if (chunks.TryGetValue(Key(cx, cz), out value))
			{
				return value;
			}
			value = new Chunk(cx, cz);
			chunks[Key(cx, cz)] = value;
			byte[] blocks;
			if (SaveDirectory != null && WorldSave.TryLoadChunk(SaveDirectory, cx, cz, out blocks))
			{
				Buffer.BlockCopy(blocks, 0, value.Blocks, 0, Math.Min(blocks.Length, value.Blocks.Length));
				value.ComputeSurface();
				value.Generated = true;
				value.Dirty = true;
				QueueLight(value);
			}
			else
			{
				if (!generateNow)
				{
					value.Generating = true;
					value.InGenQueue = true;
					lock (workLock)
					{
						genQueue.Enqueue(value);
						Monitor.Pulse(workLock);
						return value;
					}
				}
				Terrain.Generate(value);
				value.Generated = true;
				value.Dirty = true;
				QueueLight(value);
			}
			return value;
		}

		private void QueueLight(Chunk c)
		{
			c.InLightQueue = true;
			lock (workLock)
			{
				lightQueue.Enqueue(c);
				Monitor.Pulse(workLock);
			}
		}

		public void UpdateStreaming(Vec3 focus, float dt)
		{
			streamTimer += dt;
			if (streamTimer < 0.2f)
			{
				ApplyLightResults();
				return;
			}
			streamTimer = 0f;
			int num = M.FloorDiv((int)Math.Floor(focus.X), 16);
			int num2 = M.FloorDiv((int)Math.Floor(focus.Z), 16);
			for (int i = 0; i <= ChunkRadius; i++)
			{
				for (int j = -i; j <= i; j++)
				{
					for (int k = -i; k <= i; k++)
					{
						if (Math.Max(Math.Abs(j), Math.Abs(k)) == i)
						{
							EnsureChunkLoaded(num + j, num2 + k, false);
						}
					}
				}
			}
			List<Chunk> list = null;
			foreach (KeyValuePair<long, Chunk> chunk in chunks)
			{
				Chunk value = chunk.Value;
				if (value.InGenQueue || value.InLightQueue)
				{
					continue;
				}
				int num3 = Math.Abs(value.Cx - num);
				int num4 = Math.Abs(value.Cz - num2);
				if (num3 > ChunkRadius + 2 || num4 > ChunkRadius + 2)
				{
					if (list == null)
					{
						list = new List<Chunk>();
					}
					list.Add(value);
				}
			}
			if (list != null)
			{
				foreach (Chunk item in list)
				{
					if (SaveDirectory != null && item.Generated)
					{
						WorldSave.SaveChunk(SaveDirectory, item);
					}
					chunks.Remove(Key(item.Cx, item.Cz));
					item.PendingVboDelete = true;
					dirtySet.Remove(Key(item.Cx, item.Cz));
					if (this.ChunkUnloaded != null)
					{
						this.ChunkUnloaded(item);
					}
				}
			}
			ApplyLightResults();
			if (!FrozenTime)
			{
				TimeOfDay += dt / DayLength;
			}
			TimeOfDay -= (float)Math.Floor(TimeOfDay);
			UpdateSkyColors();
			UpdateWeather(dt);
		}

		private void ApplyLightResults()
		{
			lightDone.Clear();
			lock (workLock)
			{
				lightDone.AddRange(lightDoneInternal);
				lightDoneInternal.Clear();
			}
			foreach (Chunk item in lightDone)
			{
				List<int> boundarySeeds = item.BoundarySeeds;
				if (boundarySeeds.Count > 0)
				{
					seedScratch.Clear();
					seedScratch.AddRange(boundarySeeds);
					Light.ResetQueue();
					for (int i = 0; i + 4 < seedScratch.Count; i += 5)
					{
						if (seedScratch[i + 4] == 0)
						{
							Light.SeedSky(this, seedScratch[i], seedScratch[i + 1], seedScratch[i + 2], seedScratch[i + 3]);
						}
					}
					Light.Solve(this, true);
					Light.ResetQueue();
					for (int j = 0; j + 4 < seedScratch.Count; j += 5)
					{
						if (seedScratch[j + 4] == 1)
						{
							Light.SeedBlock(this, seedScratch[j], seedScratch[j + 1], seedScratch[j + 2], seedScratch[j + 3]);
						}
					}
					Light.Solve(this, false);
					boundarySeeds.Clear();
				}
				MarkDirty(item);
			}
			FlushLightDirty();
		}

		private void FlushLightDirty()
		{
			if (lightTouched.Count == 0)
			{
				return;
			}
			foreach (long item in lightTouched)
			{
				int cx = (int)(item >> 32);
				int cz = (int)item;
				Chunk chunk = GetChunk(cx, cz);
				if (chunk != null)
				{
					MarkDirty(chunk);
				}
			}
			lightTouched.Clear();
		}

		private void UpdateSkyColors()
		{
			float num = (float)Math.Sin((TimeOfDay - 0.25f) * ((float)Math.PI * 2f));
			float num2 = M.Clamp((num + 0.18f) / 0.45f, 0f, 1f);
			Daylight = M.Clamp(0.18f + 0.82f * num2, 0f, 1f);
			float num3 = M.Clamp(1f - Math.Abs(num) * 3.2f, 0f, 1f);
			float a = M.Lerp(0.045f, 0.5f, num2);
			float a2 = M.Lerp(0.06f, 0.71f, num2);
			float a3 = M.Lerp(0.14f, 1f, num2);
			a = M.Lerp(a, 0.95f, num3 * 0.55f * num2);
			a2 = M.Lerp(a2, 0.52f, num3 * 0.42f * num2);
			a3 = M.Lerp(a3, 0.3f, num3 * 0.3f * num2);
			if (WeatherIntensity > 0.01f)
			{
				a = M.Lerp(a, 0.34f, WeatherIntensity * 0.8f);
				a2 = M.Lerp(a2, 0.36f, WeatherIntensity * 0.8f);
				a3 = M.Lerp(a3, 0.4f, WeatherIntensity * 0.8f);
			}
			SkyR = a;
			SkyG = a2;
			SkyB = a3;
			FogR = M.Lerp(a, 1f, 0.2f);
			FogG = M.Lerp(a2, 1f, 0.2f);
			FogB = M.Lerp(a3, 1f, 0.2f);
			int num4 = M.Clamp((int)Math.Round(Daylight * 8f), 1, 8);
			if (num4 != BakeLevel)
			{
				BakeLevel = num4;
				if (this.DaylightChanged != null)
				{
					this.DaylightChanged(this);
				}
			}
		}

		private void UpdateWeather(float dt)
		{
			weatherTimer -= dt;
			if (weatherTimer <= 0f)
			{
				weatherTimer = Random.RangeF(150f, 420f);
				float num = Random.NextFloat();
				if (num < 0.58f)
				{
					CurrentWeather = Weather.Clear;
				}
				else if (num < 0.84f)
				{
					CurrentWeather = Weather.Rain;
				}
				else if (num < 0.95f)
				{
					CurrentWeather = Weather.Storm;
				}
				else
				{
					CurrentWeather = Weather.Snowfall;
				}
			}
			float target = ((CurrentWeather == Weather.Clear) ? 0f : ((CurrentWeather == Weather.Rain) ? 0.7f : ((CurrentWeather != Weather.Storm) ? 0.8f : 1f)));
			WeatherIntensity = M.ExpSmooth(WeatherIntensity, target, 0.35f, dt);
		}

		public void SetBlock(int x, int y, int z, byte b, bool spawnDrop, bool physics)
		{
			if (y < 0 || y >= 128)
			{
				return;
			}
			Chunk chunk = GetChunk(M.FloorDiv(x, 16), M.FloorDiv(z, 16));
			if (chunk == null || !chunk.Generated)
			{
				return;
			}
			int num = Chunk.Index(x - chunk.BaseX, y, z - chunk.BaseZ);
			byte b2 = chunk.Blocks[num];
			if (b2 == b)
			{
				return;
			}
			chunk.Blocks[num] = b;
			chunk.Dirty = true;
			MarkMeshDirtyAt(x, y, z);
			UpdateLightAt(x, y, z, b2, b);
			FlushLightDirty();
			if (spawnDrop && b2 != 0 && b2 != 12 && b2 != 13)
			{
				SpawnDrop(x, y, z, b2, 1);
				SpawnParticles(new Vec3((float)x + 0.5f, (float)y + 0.5f, (float)z + 0.5f), b2, 8, 1.6f, 3.2f);
			}
			if (physics)
			{
				if (b == 12 || b2 == 12)
				{
					EnqueueFluid(x, y, z);
				}
				SettleGravity(x, y + 1, z);
				SettleGravity(x, y, z);
			}
			if (b == 0)
			{
				BlocksBroken++;
			}
			else
			{
				BlocksPlaced++;
			}
		}

		public void SetBlock(int x, int y, int z, B b)
		{
			SetBlock(x, y, z, (byte)b, false, true);
		}

		public void BreakBlock(int x, int y, int z)
		{
			byte block = GetBlock(x, y, z);
			if (block != 0 && block != 20)
			{
				SetBlock(x, y, z, 0, true, true);
			}
		}

		public bool CanPlaceAt(int x, int y, int z)
		{
			byte block = GetBlock(x, y, z);
			return block == 0 || block == 12;
		}

		public void PlaceBlock(int x, int y, int z, byte b)
		{
			if (CanPlaceAt(x, y, z))
			{
				SetBlock(x, y, z, b, false, true);
			}
		}

		private void SettleGravity(int x, int y, int z)
		{
			if (gravityBusy)
			{
				return;
			}
			gravityBusy = true;
			try
			{
				int num = y;
				int num2 = 0;
				while (num2++ < 80)
				{
					byte block = GetBlock(x, num, z);
					if ((block != 5 && block != 6) || num - 1 < 0)
					{
						break;
					}
					byte block2 = GetBlock(x, num - 1, z);
					if (block2 != 0 && block2 != 12)
					{
						break;
					}
					SetBlock(x, num, z, 0, false, false);
					SetBlock(x, num - 1, z, block, false, false);
					num--;
				}
			}
			finally
			{
				gravityBusy = false;
			}
		}

		public void EnqueueFluid(int x, int y, int z)
		{
			if (y >= 0 && y < 128)
			{
				Vec3i item = new Vec3i(x, y, z);
				if (fluidSeen.Add(item))
				{
					fluidQueue.Enqueue(item);
				}
			}
		}

		private void UpdateFluids()
		{
			flowBudgetUsed = 0;
			int num = Math.Min(fluidQueue.Count, 300);
			for (int i = 0; i < num; i++)
			{
				if (fluidQueue.Count == 0)
				{
					break;
				}
				Vec3i vec3i = fluidQueue.Dequeue();
				fluidSeen.Remove(vec3i);
				StepFluid(vec3i);
				if (flowBudgetUsed > 90)
				{
					break;
				}
			}
		}

		private void StepFluid(Vec3i p)
		{
			byte block = GetBlock(p.X, p.Y, p.Z);
			if (block != 12)
			{
				fluidLevels.Remove(p);
				return;
			}
			byte value;
			if (!fluidLevels.TryGetValue(p, out value))
			{
				value = 8;
			}
			if (p.Y > 0 && GetBlock(p.X, p.Y - 1, p.Z) == 0)
			{
				SetBlock(p.X, p.Y, p.Z, 0, false, false);
				SetBlock(p.X, p.Y - 1, p.Z, 12, false, false);
				fluidLevels[new Vec3i(p.X, p.Y - 1, p.Z)] = 8;
				EnqueueFluid(p.X, p.Y - 1, p.Z);
				EnqueueFluid(p.X, p.Y, p.Z);
				flowBudgetUsed++;
				return;
			}
			int num = 0;
			if (value > 1)
			{
				for (int i = 0; i < 4; i++)
				{
					int x = p.X;
					int num2;
					switch (i)
					{
					case 0:
						num2 = 1;
						break;
					case 1:
						num2 = -1;
						break;
					default:
						num2 = 0;
						break;
					}
					int x2 = x + num2;
					int z = p.Z;
					int num3;
					switch (i)
					{
					case 2:
						num3 = 1;
						break;
					case 3:
						num3 = -1;
						break;
					default:
						num3 = 0;
						break;
					}
					int z2 = z + num3;
					if (GetBlock(x2, p.Y, z2) == 0)
					{
						num++;
					}
				}
			}
			if (num > 0)
			{
				byte value2 = (byte)((num <= 1) ? (value - 1) : Math.Max(1, value - 2));
				for (int j = 0; j < 4; j++)
				{
					int x3 = p.X;
					int num4;
					switch (j)
					{
					case 0:
						num4 = 1;
						break;
					case 1:
						num4 = -1;
						break;
					default:
						num4 = 0;
						break;
					}
					int x4 = x3 + num4;
					int z3 = p.Z;
					int num5;
					switch (j)
					{
					case 2:
						num5 = 1;
						break;
					case 3:
						num5 = -1;
						break;
					default:
						num5 = 0;
						break;
					}
					int z4 = z3 + num5;
					if (GetBlock(x4, p.Y, z4) == 0)
					{
						SetBlock(x4, p.Y, z4, 12, false, false);
						fluidLevels[new Vec3i(x4, p.Y, z4)] = value2;
						EnqueueFluid(x4, p.Y, z4);
						flowBudgetUsed++;
					}
				}
			}
			else
			{
				if (value >= 8)
				{
					return;
				}
				bool flag = false;
				if (p.Y > 0 && GetBlock(p.X, p.Y - 1, p.Z) == 12)
				{
					flag = true;
				}
				for (int k = 0; k < 4; k++)
				{
					if (flag)
					{
						break;
					}
					int x5 = p.X;
					int num6;
					switch (k)
					{
					case 0:
						num6 = 1;
						break;
					case 1:
						num6 = -1;
						break;
					default:
						num6 = 0;
						break;
					}
					int x6 = x5 + num6;
					int z5 = p.Z;
					int num7;
					switch (k)
					{
					case 2:
						num7 = 1;
						break;
					case 3:
						num7 = -1;
						break;
					default:
						num7 = 0;
						break;
					}
					int z6 = z5 + num7;
					byte value3;
					if (GetBlock(x6, p.Y, z6) == 12 && (!fluidLevels.TryGetValue(new Vec3i(x6, p.Y, z6), out value3) || value3 > value))
					{
						flag = true;
					}
				}
				if (flag)
				{
					return;
				}
				SetBlock(p.X, p.Y, p.Z, 0, false, false);
				fluidLevels.Remove(p);
				for (int l = 0; l < 4; l++)
				{
					int x7 = p.X;
					int num8;
					switch (l)
					{
					case 0:
						num8 = 1;
						break;
					case 1:
						num8 = -1;
						break;
					default:
						num8 = 0;
						break;
					}
					int x8 = x7 + num8;
					int y = p.Y;
					int z7 = p.Z;
					int num9;
					switch (l)
					{
					case 2:
						num9 = 1;
						break;
					case 3:
						num9 = -1;
						break;
					default:
						num9 = 0;
						break;
					}
					EnqueueFluid(x8, y, z7 + num9);
				}
			}
		}

		private void UpdateLightAt(int x, int y, int z, byte oldB, byte newB)
		{
			BlockInfo blockInfo = ((oldB != 0) ? Blocks.Info((B)oldB) : null);
			BlockInfo blockInfo2 = ((newB != 0) ? Blocks.Info((B)newB) : null);
			bool flag = blockInfo != null && blockInfo.Opaque;
			bool flag2 = blockInfo2 != null && blockInfo2.Opaque;
			int num = ((blockInfo != null) ? blockInfo.Light : 0);
			int num2 = ((blockInfo2 != null) ? blockInfo2.Light : 0);
			byte blockLight = GetBlockLight(x, y, z);
			if (num > 0 || (flag2 && !flag && blockLight > 0))
			{
				Light.RemoveBlock(this, x, y, z, blockLight);
			}
			if (!flag2)
			{
				Light.ResetQueue();
				if (num2 > 0)
				{
					Light.SeedBlock(this, x, y, z, num2);
				}
				for (int i = 0; i < 6; i++)
				{
					int num3;
					switch (i)
					{
					case 0:
						num3 = 1;
						break;
					case 1:
						num3 = -1;
						break;
					default:
						num3 = 0;
						break;
					}
					int x2 = x + num3;
					int num4;
					switch (i)
					{
					case 2:
						num4 = 1;
						break;
					case 3:
						num4 = -1;
						break;
					default:
						num4 = 0;
						break;
					}
					int y2 = y + num4;
					int num5;
					switch (i)
					{
					case 4:
						num5 = 1;
						break;
					case 5:
						num5 = -1;
						break;
					default:
						num5 = 0;
						break;
					}
					int z2 = z + num5;
					int blockLight2 = GetBlockLight(x2, y2, z2);
					if (blockLight2 > 1)
					{
						Light.SeedBlock(this, x2, y2, z2, blockLight2);
					}
				}
				Light.Solve(this, false);
			}
			if (flag2 && !flag)
			{
				byte skyLight = GetSkyLight(x, y, z);
				Light.RemoveSky(this, x, y, z, skyLight);
			}
			else
			{
				if (flag2)
				{
					return;
				}
				Light.ResetQueue();
				int num6 = ((y + 1 >= 128) ? 15 : GetSkyLight(x, y + 1, z));
				if (num6 > 0)
				{
					Light.SeedSky(this, x, y, z, num6);
				}
				for (int j = 0; j < 6; j++)
				{
					int num7;
					switch (j)
					{
					case 0:
						num7 = 1;
						break;
					case 1:
						num7 = -1;
						break;
					default:
						num7 = 0;
						break;
					}
					int x3 = x + num7;
					int num8;
					switch (j)
					{
					case 2:
						num8 = 1;
						break;
					case 3:
						num8 = -1;
						break;
					default:
						num8 = 0;
						break;
					}
					int y3 = y + num8;
					int num9;
					switch (j)
					{
					case 4:
						num9 = 1;
						break;
					case 5:
						num9 = -1;
						break;
					default:
						num9 = 0;
						break;
					}
					int z3 = z + num9;
					int skyLight2 = GetSkyLight(x3, y3, z3);
					if (skyLight2 > 1)
					{
						Light.SeedSky(this, x3, y3, z3, skyLight2 - 1);
					}
				}
				Light.Solve(this, true);
			}
		}

		private void MarkMeshDirtyAt(int x, int y, int z)
		{
			int num = M.FloorDiv(x, 16);
			int num2 = M.FloorDiv(z, 16);
			int num3 = x - num * 16;
			int num4 = z - num2 * 16;
			int num5 = ((num3 <= 1) ? (-1) : 0);
			int num6 = ((num3 >= 14) ? 1 : 0);
			int num7 = ((num4 <= 1) ? (-1) : 0);
			int num8 = ((num4 >= 14) ? 1 : 0);
			for (int i = num5; i <= num6; i++)
			{
				for (int j = num7; j <= num8; j++)
				{
					Chunk chunk = GetChunk(num + i, num2 + j);
					if (chunk != null)
					{
						MarkDirty(chunk);
					}
				}
			}
		}

		public void MarkDirty(Chunk c)
		{
			if (dirtySet.Add(Key(c.Cx, c.Cz)))
			{
				dirtyMeshes.Add(c);
			}
		}

		public void MarkAllDirty()
		{
			foreach (Chunk value in chunks.Values)
			{
				MarkDirty(value);
			}
		}

		public int ProcessMeshWork(Vec3 focus, float budgetMs, Action<Chunk> buildMesh)
		{
			if (dirtyMeshes.Count == 0)
			{
				return 0;
			}
			int num = 0;
			long num2 = Environment.TickCount;
			float fx = focus.X;
			float fz = focus.Z;
			dirtyMeshes.Sort(delegate(Chunk a, Chunk b)
			{
				float num3 = ((float)(a.BaseX + 8) - fx) * ((float)(a.BaseX + 8) - fx) + ((float)(a.BaseZ + 8) - fz) * ((float)(a.BaseZ + 8) - fz);
				float value = ((float)(b.BaseX + 8) - fx) * ((float)(b.BaseX + 8) - fx) + ((float)(b.BaseZ + 8) - fz) * ((float)(b.BaseZ + 8) - fz);
				return num3.CompareTo(value);
			});
			while (dirtyMeshes.Count > 0 && (float)(Environment.TickCount - num2) < budgetMs)
			{
				Chunk chunk = dirtyMeshes[0];
				dirtyMeshes.RemoveAt(0);
				dirtySet.Remove(Key(chunk.Cx, chunk.Cz));
				if (!chunk.PendingVboDelete && chunk.Generated && chunk.Lit)
				{
					buildMesh(chunk);
					num++;
				}
			}
			return num;
		}

		public void SpawnDrop(int x, int y, int z, byte block, int count)
		{
			if (block != 0)
			{
				Items.Add(new ItemEntity(block, new Vec3((float)x + 0.5f, (float)y + 0.4f, (float)z + 0.5f), count));
			}
		}

		public void UpdateEntities(float dt)
		{
			for (int i = 0; i < Creatures.Count; i++)
			{
				if (Creatures[i].Alive)
				{
					Creatures[i].Update(this, dt);
				}
			}
			for (int num = Items.Count - 1; num >= 0; num--)
			{
				ItemEntity itemEntity = Items[num];
				itemEntity.Update(this, dt);
				if (!itemEntity.Alive)
				{
					Items.RemoveAt(num);
				}
			}
		}

		public int CollectItems(Vec3 pos, float radius, Action<byte, int> onPickup)
		{
			float num = radius * radius;
			int num2 = 0;
			for (int num3 = Items.Count - 1; num3 >= 0; num3--)
			{
				ItemEntity itemEntity = Items[num3];
				if (!(Vec3.DistanceSq(itemEntity.Position, pos) > num))
				{
					onPickup(itemEntity.Block, itemEntity.Count);
					Items.RemoveAt(num3);
					num2++;
				}
			}
			return num2;
		}

		public bool Raycast(Vec3 origin, Vec3 dir, float maxDist, out Vec3i hit, out Vec3i normal)
		{
			hit = Vec3i.Zero;
			normal = Vec3i.Zero;
			Vec3 normalized = dir.Normalized;
			if (normalized.LengthSq < 1E-08f)
			{
				return false;
			}
			int num = M.FloorInt(origin.X);
			int num2 = M.FloorInt(origin.Y);
			int num3 = M.FloorInt(origin.Z);
			int num4 = ((normalized.X > 0f) ? 1 : (-1));
			int num5 = ((normalized.Y > 0f) ? 1 : (-1));
			int num6 = ((normalized.Z > 0f) ? 1 : (-1));
			float num7 = ((normalized.X != 0f) ? (((!(normalized.X > 0f)) ? (origin.X - (float)num) : ((float)(num + 1) - origin.X)) / Math.Abs(normalized.X)) : float.MaxValue);
			float num8 = ((normalized.Y != 0f) ? (((!(normalized.Y > 0f)) ? (origin.Y - (float)num2) : ((float)(num2 + 1) - origin.Y)) / Math.Abs(normalized.Y)) : float.MaxValue);
			float num9 = ((normalized.Z != 0f) ? (((!(normalized.Z > 0f)) ? (origin.Z - (float)num3) : ((float)(num3 + 1) - origin.Z)) / Math.Abs(normalized.Z)) : float.MaxValue);
			float num10 = ((normalized.X != 0f) ? (1f / Math.Abs(normalized.X)) : float.MaxValue);
			float num11 = ((normalized.Y != 0f) ? (1f / Math.Abs(normalized.Y)) : float.MaxValue);
			float num12 = ((normalized.Z != 0f) ? (1f / Math.Abs(normalized.Z)) : float.MaxValue);
			float num13 = 0f;
			int num14 = 0;
			while (num13 <= maxDist && num14++ < 400)
			{
				byte block = GetBlock(num, num2, num3);
				if (block != 0 && block != 12 && block != 13)
				{
					hit = new Vec3i(num, num2, num3);
					return true;
				}
				if (num7 < num8 && num7 < num9)
				{
					num += num4;
					num13 = num7;
					num7 += num10;
					normal = new Vec3i(-num4, 0, 0);
				}
				else if (num8 < num9)
				{
					num2 += num5;
					num13 = num8;
					num8 += num11;
					normal = new Vec3i(0, -num5, 0);
				}
				else
				{
					num3 += num6;
					num13 = num9;
					num9 += num12;
					normal = new Vec3i(0, 0, -num6);
				}
			}
			return false;
		}

		public bool RaycastBlock(Vec3 origin, Vec3 dir, float maxDist, out Vec3i hit, out Vec3i normal, out B block)
		{
			block = B.Air;
			if (!Raycast(origin, dir, maxDist, out hit, out normal))
			{
				return false;
			}
			block = GetB(hit.X, hit.Y, hit.Z);
			return true;
		}

		public bool HasLineOfSight(Vec3 a, Vec3 b)
		{
			Vec3 a2 = b - a;
			float length = a2.Length;
			if (length < 0.05f)
			{
				return true;
			}
			Vec3i hit;
			Vec3i normal;
			if (!Raycast(a, a2 / length, length - 0.15f, out hit, out normal))
			{
				return true;
			}
			return hit == b.ToVec3i();
		}

		public int LightAt(int x, int y, int z)
		{
			int val = GetSkyLight(x, y, z) * BakeLevel / 8;
			int blockLight = GetBlockLight(x, y, z);
			return Math.Max(val, blockLight);
		}

		public byte GetSkyLight(int x, int y, int z)
		{
			if (y < 0 || y >= 128)
			{
				return 0;
			}
			Chunk value;
			if (!chunks.TryGetValue(Key(M.FloorDiv(x, 16), M.FloorDiv(z, 16)), out value))
			{
				return 15;
			}
			if (!value.Lit)
			{
				return 15;
			}
			return value.SkyLight[Chunk.Index(x - value.BaseX, y, z - value.BaseZ)];
		}

		public byte GetBlockLight(int x, int y, int z)
		{
			if (y < 0 || y >= 128)
			{
				return 0;
			}
			Chunk value;
			if (!chunks.TryGetValue(Key(M.FloorDiv(x, 16), M.FloorDiv(z, 16)), out value))
			{
				return 0;
			}
			return value.BlockLight[Chunk.Index(x - value.BaseX, y, z - value.BaseZ)];
		}

		byte ILightVolume.LightBlock(int x, int y, int z)
		{
			return GetBlock(x, y, z);
		}

		byte ILightVolume.LightSky(int x, int y, int z)
		{
			return GetSkyLight(x, y, z);
		}

		byte ILightVolume.LightBlockValue(int x, int y, int z)
		{
			return GetBlockLight(x, y, z);
		}

		bool ILightVolume.LightIsOpaque(int x, int y, int z)
		{
			if (y < 0 || y >= 128)
			{
				return true;
			}
			byte block = GetBlock(x, y, z);
			return block != 0 && Blocks.Info((B)block).Opaque;
		}

		void ILightVolume.LightSetSky(int x, int y, int z, byte v)
		{
			if (y >= 0 && y < 128)
			{
				Chunk chunk = GetChunk(M.FloorDiv(x, 16), M.FloorDiv(z, 16));
				if (chunk != null)
				{
					chunk.SkyLight[Chunk.Index(x - chunk.BaseX, y, z - chunk.BaseZ)] = v;
					lightTouched.Add(Key(chunk.Cx, chunk.Cz));
				}
			}
		}

		void ILightVolume.LightSetBlockValue(int x, int y, int z, byte v)
		{
			if (y >= 0 && y < 128)
			{
				Chunk chunk = GetChunk(M.FloorDiv(x, 16), M.FloorDiv(z, 16));
				if (chunk != null)
				{
					chunk.BlockLight[Chunk.Index(x - chunk.BaseX, y, z - chunk.BaseZ)] = v;
					lightTouched.Add(Key(chunk.Cx, chunk.Cz));
				}
			}
		}

		public void Update(Vec3 focus, float dt)
		{
			UpdateStreaming(focus, dt);
			UpdateFluids();
			UpdateEntities(dt);
			UpdateParticles(dt);
		}

		public void SaveAll()
		{
			if (SaveDirectory == null)
			{
				return;
			}
			foreach (Chunk value in chunks.Values)
			{
				if (value.Generated)
				{
					WorldSave.SaveChunk(SaveDirectory, value);
				}
			}
		}
	}
}
