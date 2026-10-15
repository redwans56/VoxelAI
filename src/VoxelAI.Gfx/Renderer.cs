using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using VoxelAI.AI;
using VoxelAI.Core;
using VoxelAI.Entities;
using VoxelAI.Game;
using VoxelAI.World;

namespace VoxelAI.Gfx
{
	public class Renderer
	{
		public TextureAtlas Atlas = new TextureAtlas();

		public Ui2D Ui = new Ui2D();

		private readonly ChunkNeighborhood nb = new ChunkNeighborhood();

		public bool UseVbo;

		private static readonly float[] EmptyMesh = new float[0];

		public int DrawnChunks;

		public int DrawnVertices;

		public string GLInfo = string.Empty;

		private Camera GameCam;

		private float[] boxBuf = new float[131072];

		private float[] itemBuf = new float[32768];

		private float[] heldBuf = new float[32768];

		private float[] particleBuf = new float[131072];

		private float[] cloudBuf = new float[16384];

		private const int WeatherCount = 900;

		private float[] wx = new float[900];

		private float[] wy2 = new float[900];

		private float[] wz = new float[900];

		private float[] weatherLine = new float[14400];

		private Rng weatherRng = new Rng(4242);

		private float[] stars = new float[780];

		private bool starsBuilt;

		private float cloudScroll;

		private float Time2;

		public void Init()
		{
			GL.Enable(2929u);
			GL.DepthFunc(515u);
			GL.Enable(2884u);
			GL.CullFace(1029u);
			GL.FrontFace(2305u);
			GL.ShadeModel(7425u);
			GL.Hint(3152u, 4354u);
			GL.Enable(3553u);
			GL.TexEnvi(8960u, 8704u, 8448);
			GL.Enable(2912u);
			GL.Fogi(2917u, 2049);
			GL.Fogf(2914u, 0.011f);
			GL.Enable(3008u);
			GL.AlphaFunc(516u, 0.5f);
			GL.Disable(3042u);
			GL.Disable(2896u);
			GL.PixelStorei(3317u, 4);
			GL.Disable(3089u);
			Atlas.Upload();
			Ui.BuildFontTexture();
			Ui.TextureAtlasId = Atlas.TextureId;
			UseVbo = GL.HasVbo;
			GLInfo = GL.RendererName + " | OpenGL " + GL.Version + ((!UseVbo) ? " | client arrays" : " | VBO");
			BuildStars();
		}

		private void BuildStars()
		{
			for (int i = 0; i < stars.Length; i += 3)
			{
				float num = weatherRng.RangeF(-1f, 1f);
				float num2 = weatherRng.RangeF(0f, (float)Math.PI * 2f);
				float num3 = (float)Math.Sqrt(1f - num * num);
				stars[i] = num3 * (float)Math.Cos(num2) * 380f;
				stars[i + 1] = Math.Abs(num) * 380f + 30f;
				stars[i + 2] = num3 * (float)Math.Sin(num2) * 380f;
			}
			starsBuilt = true;
		}

		public void Resize(int w, int h)
		{
			Ui.Width = Math.Max(1, w);
			Ui.Height = Math.Max(1, h);
		}

		public void BeginFrame(VoxelAI.World.World world, Camera cam)
		{
			GameCam = cam;
			GL.Viewport(0, 0, cam.Width, cam.Height);
			float num = world.FogR;
			float num2 = world.FogG;
			float num3 = world.FogB;
			float param = 0.0105f + world.WeatherIntensity * 0.008f;
			if (world.GetBlock((int)Math.Floor(cam.Pos.X), (int)Math.Floor(cam.Pos.Y), (int)Math.Floor(cam.Pos.Z)) == 12)
			{
				num = 0.12f;
				num2 = 0.28f;
				num3 = 0.52f;
				param = 0.075f;
			}
			GL.ClearColor(num, num2, num3, 1f);
			GL.ClearDepth(1.0);
			GL.Clear(16640u);
			GL.MatrixMode(5889u);
			GL.LoadMatrixf(cam.Proj.A);
			GL.MatrixMode(5888u);
			GL.LoadMatrixf(cam.View.A);
			GL.Enable(2912u);
			GL.Fogf(2914u, param);
			GL.Fogfv(2918u, new float[4]
			{
				num,
				num2,
				num3,
				1f
			});
			DrawSkyGradient(world, cam);
			DrawCelestial(world, cam);
		}

		public void BeginFrameMenu(Camera cam)
		{
			GL.Viewport(0, 0, cam.Width, cam.Height);
			GL.ClearColor(0.03f, 0.04f, 0.07f, 1f);
			GL.ClearDepth(1.0);
			GL.Clear(16640u);
			GL.Disable(2912u);
			GL.MatrixMode(5889u);
			GL.LoadMatrixf(cam.Proj.A);
			GL.MatrixMode(5888u);
			GL.LoadMatrixf(cam.View.A);
		}

		private void DrawSkyGradient(VoxelAI.World.World world, Camera cam)
		{
			float num = cam.FovDeg * 0.5f * ((float)Math.PI / 180f);
			float v = 0.5f + (float)Math.Tan(cam.Pitch) / (float)Math.Tan(num) * 0.5f;
			v = M.Clamp(v, 0.01f, 0.99f);
			GL.Disable(2929u);
			GL.DepthMask(false);
			GL.Disable(3553u);
			GL.Disable(2912u);
			GL.Disable(2884u);
			GL.MatrixMode(5889u);
			GL.PushMatrix();
			GL.LoadIdentity();
			GL.Ortho(0.0, cam.Width, cam.Height, 0.0, -1.0, 1.0);
			GL.MatrixMode(5888u);
			GL.PushMatrix();
			GL.LoadIdentity();
			float y = v * (float)cam.Height;
			GL.Begin(7u);
			GL.Color3f(world.SkyR, world.SkyG, world.SkyB);
			GL.Vertex3f(0f, 0f, 0f);
			GL.Vertex3f(cam.Width, 0f, 0f);
			GL.Color3f(world.FogR, world.FogG, world.FogB);
			GL.Vertex3f(cam.Width, y, 0f);
			GL.Vertex3f(0f, y, 0f);
			GL.Color3f(world.FogR, world.FogG, world.FogB);
			GL.Vertex3f(0f, y, 0f);
			GL.Vertex3f(cam.Width, y, 0f);
			GL.Color3f(world.FogR * 0.6f, world.FogG * 0.6f, world.FogB * 0.6f);
			GL.Vertex3f(cam.Width, cam.Height, 0f);
			GL.Vertex3f(0f, cam.Height, 0f);
			GL.End();
			GL.MatrixMode(5889u);
			GL.PopMatrix();
			GL.MatrixMode(5888u);
			GL.PopMatrix();
			GL.MatrixMode(5889u);
			GL.LoadMatrixf(cam.Proj.A);
			GL.MatrixMode(5888u);
			GL.LoadMatrixf(cam.View.A);
			if (world.Daylight < 0.55f && starsBuilt)
			{
				float a = M.Clamp((0.55f - world.Daylight) / 0.55f, 0f, 1f);
				GL.PushMatrix();
				GL.Translatef(cam.Pos.X, cam.Pos.Y * 0.2f, cam.Pos.Z);
				GCHandle gCHandle = GCHandle.Alloc(stars, GCHandleType.Pinned);
				try
				{
					IntPtr pointer = gCHandle.AddrOfPinnedObject();
					GL.EnableClientState(32884u);
					GL.DisableClientState(32886u);
					GL.DisableClientState(32888u);
					GL.VertexPointer(3, 5126u, 12, pointer);
					GL.Color4f(1f, 1f, 1f, a);
					GL.DrawArrays(0u, 0, stars.Length / 3);
				}
				finally
				{
					GL.DisableClientState(32884u);
					gCHandle.Free();
				}
				GL.PopMatrix();
			}
			GL.Enable(3553u);
			GL.Enable(2884u);
			GL.Enable(2912u);
			GL.DepthMask(true);
			GL.Enable(2929u);
		}

		private void DrawCelestial(VoxelAI.World.World world, Camera cam)
		{
			float num = (world.TimeOfDay - 0.25f) * ((float)Math.PI * 2f);
			Vec3 normalized = new Vec3((float)Math.Cos(num) * 0.4f, (float)Math.Sin(num), 0.55f).Normalized;
			Vec3 a = -normalized;
			float y = normalized.Y;
			GL.Disable(2929u);
			GL.DepthMask(false);
			GL.Disable(2912u);
			GL.Disable(2884u);
			GL.Enable(3042u);
			GL.BlendFunc(770u, 771u);
			float u;
			float v;
			float u2;
			float v2;
			if (y > -0.15f)
			{
				TextureAtlas.GetUv(31, out u, out v, out u2, out v2);
				DrawBillboard(cam.Pos + normalized * 300f, 38f, u, v, u2, v2, 1f, 1f, 1f, M.Clamp(y * 3f + 0.4f, 0f, 1f));
			}
			if (y < 0.15f)
			{
				TextureAtlas.GetUv(32, out u, out v, out u2, out v2);
				DrawBillboard(cam.Pos + a * 300f, 30f, u, v, u2, v2, 1f, 1f, 1f, M.Clamp((0f - y) * 3f + 0.4f, 0f, 1f));
			}
			GL.Disable(3042u);
			GL.Enable(2912u);
			GL.DepthMask(true);
			GL.Enable(2929u);
			GL.Enable(2884u);
		}

		private void DrawBillboard(Vec3 world, float size, float u0, float v0, float u1, float v1, float r, float g, float b, float a)
		{
			Vec3 normalized = Vec3.Cross(new Vec3(0f, 1f, 0f), (world - GameCam.Pos).Normalized).Normalized;
			Vec3 normalized2 = Vec3.Cross((world - GameCam.Pos).Normalized, normalized).Normalized;
			float[] array = itemBuf;
			int v2 = 0;
			Vec3 p = world - normalized * size - normalized2 * size;
			Vec3 p2 = world + normalized * size - normalized2 * size;
			Vec3 p3 = world + normalized * size + normalized2 * size;
			Vec3 p4 = world - normalized * size + normalized2 * size;
			PushV(ref v2, array, p, u0, v1, r, g, b);
			PushV(ref v2, array, p2, u1, v1, r, g, b);
			PushV(ref v2, array, p3, u1, v0, r, g, b);
			PushV(ref v2, array, p, u0, v1, r, g, b);
			PushV(ref v2, array, p3, u1, v0, r, g, b);
			PushV(ref v2, array, p4, u0, v0, r, g, b);
			DrawArray3D(array, 6, true);
		}

		private static void PushV(ref int v, float[] buf, Vec3 p, float u, float w, float r, float g, float b)
		{
			buf[v] = p.X;
			buf[v + 1] = p.Y;
			buf[v + 2] = p.Z;
			buf[v + 3] = u;
			buf[v + 4] = w;
			buf[v + 5] = r;
			buf[v + 6] = g;
			buf[v + 7] = b;
			v += 8;
		}

		public void BuildChunkMeshes(VoxelAI.World.World world, Chunk c)
		{
			MeshBuilder.Build(world, c, false, nb);
			MeshBuilder.Build(world, c, true, nb);
			UploadChunk(c);
		}

		private void UploadChunk(Chunk c)
		{
			if (UseVbo)
			{
				if (c.Vbo == 0)
				{
					c.Vbo = GL.GenBuffer();
				}
				GL.BindBuffer(34962u, c.Vbo);
				GL.BufferData(34962u, c.Mesh, 35044u);
				if (c.LiquidVbo == 0)
				{
					c.LiquidVbo = GL.GenBuffer();
				}
				GL.BindBuffer(34962u, c.LiquidVbo);
				GL.BufferData(34962u, c.LiquidMesh, 35044u);
				GL.BindBuffer(34962u, 0);
				c.Mesh = EmptyMesh;
				c.LiquidMesh = EmptyMesh;
			}
			c.UploadedVersion = c.MeshVersion;
		}

		public void ReleaseChunk(Chunk c)
		{
			if (UseVbo)
			{
				GL.DeleteBuffer(ref c.Vbo);
				GL.DeleteBuffer(ref c.LiquidVbo);
			}
			c.DisposeMesh();
		}

		public void DrawChunks(VoxelAI.World.World world, Camera cam, List<Chunk> chunks, bool liquidPass)
		{
			GL.BindTexture(3553u, Atlas.TextureId);
			if (liquidPass)
			{
				GL.Enable(3042u);
				GL.BlendFunc(770u, 771u);
				GL.DepthMask(false);
				GL.Disable(3008u);
				GL.Disable(2884u);
			}
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < chunks.Count; i++)
			{
				Chunk chunk = chunks[i];
				if (!chunk.Generated || !chunk.Lit || chunk.UploadedVersion != chunk.MeshVersion)
				{
					continue;
				}
				Vec3 min = new Vec3(chunk.BaseX, 0f, chunk.BaseZ);
				Vec3 max = new Vec3(chunk.BaseX + 16, 128f, chunk.BaseZ + 16);
				if (!cam.BoxVisible(min, max))
				{
					continue;
				}
				int num3 = ((!liquidPass) ? chunk.VertexCount : chunk.LiquidVertexCount);
				if (num3 != 0)
				{
					if (UseVbo)
					{
						GL.BindBuffer(34962u, (!liquidPass) ? chunk.Vbo : chunk.LiquidVbo);
						GL.EnableClientState(32884u);
						GL.EnableClientState(32888u);
						GL.EnableClientState(32886u);
						int stride = 32;
						GL.VertexPointer(3, 5126u, stride, (IntPtr)0);
						GL.TexCoordPointer(2, 5126u, stride, (IntPtr)12);
						GL.ColorPointer(3, 5126u, stride, (IntPtr)20);
						GL.DrawArrays(4u, 0, num3);
						GL.BindBuffer(34962u, 0);
						GL.DisableClientState(32884u);
						GL.DisableClientState(32888u);
						GL.DisableClientState(32886u);
					}
					else
					{
						DrawArray3D((!liquidPass) ? chunk.Mesh : chunk.LiquidMesh, num3, true);
					}
					num++;
					num2 += num3;
				}
			}
			if (liquidPass)
			{
				GL.Disable(3042u);
				GL.DepthMask(true);
				GL.Enable(3008u);
				GL.Enable(2884u);
			}
			DrawnChunks = num;
			DrawnVertices = num2;
		}

		private void DrawArray3D(float[] arr, int verts, bool textured)
		{
			if (verts <= 0)
			{
				return;
			}
			GCHandle gCHandle = GCHandle.Alloc(arr, GCHandleType.Pinned);
			try
			{
				IntPtr pointer = gCHandle.AddrOfPinnedObject();
				int stride = 32;
				GL.EnableClientState(32884u);
				GL.VertexPointer(3, 5126u, stride, pointer);
				if (textured)
				{
					GL.EnableClientState(32888u);
					GL.TexCoordPointer(2, 5126u, stride, (IntPtr)(pointer.ToInt64() + 12));
				}
				else
				{
					GL.DisableClientState(32888u);
				}
				GL.EnableClientState(32886u);
				GL.ColorPointer(3, 5126u, stride, (IntPtr)(pointer.ToInt64() + 20));
				GL.DrawArrays(4u, 0, verts);
			}
			finally
			{
				GL.DisableClientState(32886u);
				GL.DisableClientState(32888u);
				GL.DisableClientState(32884u);
				gCHandle.Free();
			}
		}

		public void DrawClouds(VoxelAI.World.World world, Camera cam, float dt)
		{
			cloudScroll += dt * 0.35f;
			if (cloudScroll > 1f)
			{
				cloudScroll -= 1f;
			}
			GL.Disable(2912u);
			GL.Enable(3042u);
			GL.BlendFunc(770u, 771u);
			GL.DepthMask(false);
			GL.Disable(2884u);
			GL.Disable(3008u);
			float y = 116f;
			float num = 420f;
			float x = cam.Pos.X;
			float z = cam.Pos.Z;
			float num2 = M.Lerp(0.55f, 1f, world.Daylight);
			GL.BindTexture(3553u, Atlas.TextureId);
			float[] array = cloudBuf;
			int v = 0;
			for (int i = 0; i < 5; i++)
			{
				float num3 = (float)(i - 2) * num * 0.5f;
				float num4 = (float)(i - 2) * num * 0.5f;
				Vec3 p = new Vec3(x - num * 0.5f + num3, y, z - num * 0.5f + num4);
				Vec3 p2 = new Vec3(x + num * 0.5f + num3, y, z - num * 0.5f + num4);
				Vec3 p3 = new Vec3(x + num * 0.5f + num3, y, z + num * 0.5f + num4);
				Vec3 p4 = new Vec3(x - num * 0.5f + num3, y, z + num * 0.5f + num4);
				float num5 = cloudScroll;
				PushV(ref v, array, p, num5, 1f + num5, num2, num2, num2);
				PushV(ref v, array, p2, 1f + num5, 1f + num5, num2, num2, num2);
				PushV(ref v, array, p3, 1f + num5, num5, num2, num2, num2);
				PushV(ref v, array, p, num5, 1f + num5, num2, num2, num2);
				PushV(ref v, array, p3, 1f + num5, num5, num2, num2, num2);
				PushV(ref v, array, p4, num5, num5, num2, num2, num2);
				if (v >= array.Length - 48)
				{
					DrawArray3D(array, v / 8, true);
					v = 0;
				}
			}
			if (v > 0)
			{
				DrawArray3D(array, v / 8, true);
			}
			GL.Enable(3008u);
			GL.Enable(2884u);
			GL.DepthMask(true);
			GL.Disable(3042u);
			GL.Enable(2912u);
		}

		public void DrawCreatures(VoxelAI.World.World world, Camera cam, AISystem ai)
		{
			GL.BindTexture(3553u, 0);
			GL.Disable(3553u);
			for (int i = 0; i < world.Creatures.Count; i++)
			{
				Creature creature = world.Creatures[i];
				if (creature.Alive && cam.PointVisible(creature.Position, 3f))
				{
					GL.PushMatrix();
					GL.Translatef(creature.Position.X, creature.Position.Y - creature.Size.Y * 0.5f, creature.Position.Z);
					GL.Rotatef(creature.Yaw * (180f / (float)Math.PI), 0f, 1f, 0f);
					int v = 0;
					float num = (float)Math.Sin(creature.WalkPhase) * 0.35f;
					v = MeshBuilder.BuildColoredBox(boxBuf, v, new Vec3(-0.28f, 0f, -0.12f), new Vec3(-0.08f, 0.45f + num * 0.1f, 0.12f), creature.BodyR * 0.7f, creature.BodyG * 0.7f, creature.BodyB * 0.7f, 1f);
					v = MeshBuilder.BuildColoredBox(boxBuf, v, new Vec3(0.08f, 0f, -0.12f), new Vec3(0.28f, 0.45f - num * 0.1f, 0.12f), creature.BodyR * 0.7f, creature.BodyG * 0.7f, creature.BodyB * 0.7f, 1f);
					v = MeshBuilder.BuildColoredBox(boxBuf, v, new Vec3(-0.3f, 0.42f, -0.18f), new Vec3(0.3f, 1.05f, 0.18f), creature.BodyR, creature.BodyG, creature.BodyB, 1f);
					v = MeshBuilder.BuildColoredBox(boxBuf, v, new Vec3(-0.45f, 0.45f, -0.1f), new Vec3(-0.3f, 0.95f - num * 0.2f, 0.1f), creature.BodyR * 1.1f, creature.BodyG * 1.1f, creature.BodyB * 1.1f, 1f);
					v = MeshBuilder.BuildColoredBox(boxBuf, v, new Vec3(0.3f, 0.45f, -0.1f), new Vec3(0.45f, 0.95f + num * 0.2f, 0.1f), creature.BodyR * 1.1f, creature.BodyG * 1.1f, creature.BodyB * 1.1f, 1f);
					v = MeshBuilder.BuildColoredBox(boxBuf, v, new Vec3(-0.25f, 1.05f, -0.25f), new Vec3(0.25f, 1.55f, 0.25f), creature.HeadR, creature.HeadG, creature.HeadB, 1f);
					v = MeshBuilder.BuildColoredBox(boxBuf, v, new Vec3(-0.15f, 1.3f, 0.25f), new Vec3(-0.05f, 1.4f, 0.27f), 0.08f, 0.08f, 0.1f, 1f);
					v = MeshBuilder.BuildColoredBox(boxBuf, v, new Vec3(0.05f, 1.3f, 0.25f), new Vec3(0.15f, 1.4f, 0.27f), 0.08f, 0.08f, 0.1f, 1f);
					DrawArray3D(boxBuf, v / 8, false);
					GL.PopMatrix();
				}
			}
			if (ai != null)
			{
				Agent agent = null;
				for (int j = 0; j < ai.Agents.Count; j++)
				{
					if (ai.Agents[j].IsSelected)
					{
						agent = ai.Agents[j];
						break;
					}
				}
				if (agent != null && !agent.Dead)
				{
					Vec3 position = agent.Body.Position;
					GL.Disable(3553u);
					GL.Disable(2884u);
					GL.Enable(3042u);
					GL.DepthMask(false);
					GL.LineWidth(2.5f);
					GL.Color4f(1f, 0.95f, 0.2f, 0.85f);
					GL.Begin(2u);
					for (int k = 0; k < 24; k++)
					{
						float num2 = (float)k / 24f * ((float)Math.PI * 2f);
						GL.Vertex3f(position.X + (float)Math.Cos(num2) * 0.6f, position.Y - agent.Body.Size.Y * 0.5f + 0.03f, position.Z + (float)Math.Sin(num2) * 0.6f);
					}
					GL.End();
					GL.LineWidth(1f);
					GL.DepthMask(true);
					GL.Disable(3042u);
					GL.Enable(2884u);
				}
			}
			GL.Enable(3553u);
		}

		public void DrawItems(VoxelAI.World.World world, Camera cam)
		{
			GL.BindTexture(3553u, Atlas.TextureId);
			for (int i = 0; i < world.Items.Count; i++)
			{
				ItemEntity itemEntity = world.Items[i];
				if (cam.PointVisible(itemEntity.Position, 1f))
				{
					GL.PushMatrix();
					GL.Translatef(itemEntity.Position.X, itemEntity.Position.Y, itemEntity.Position.Z);
					GL.Rotatef(itemEntity.Spin * 57.3f, 0f, 1f, 0f);
					int num = MeshBuilder.BuildBlockCube(itemBuf, 0, new Vec3(-0.18f, -0.18f, -0.18f), 0.36f, itemEntity.Block, 1f);
					DrawArray3D(itemBuf, num / 8, true);
					GL.PopMatrix();
				}
			}
		}

		public void DrawParticles(VoxelAI.World.World world, Camera cam)
		{
			GL.Disable(3553u);
			GL.Disable(2884u);
			GL.Enable(3042u);
			GL.BlendFunc(770u, 771u);
			GL.DepthMask(false);
			int num = 0;
			for (int i = 0; i < world.Particles.Count; i++)
			{
				VoxelAI.World.World.Particle particle = world.Particles[i];
				if (cam.PointVisible(particle.Pos, 1.5f))
				{
					float num2 = M.Clamp(particle.Life / particle.MaxLife * 1.6f, 0f, 1f);
					float size = particle.Size;
					Vec3 min = particle.Pos - new Vec3(size, size, size);
					Vec3 max = particle.Pos + new Vec3(size, size, size);
					num = MeshBuilder.BuildColoredBox(particleBuf, num, min, max, particle.R * num2, particle.G * num2, particle.B * num2, 1f);
					if (num > particleBuf.Length - 384)
					{
						DrawArray3D(particleBuf, num / 8, false);
						num = 0;
					}
				}
			}
			if (num > 0)
			{
				DrawArray3D(particleBuf, num / 8, false);
			}
			GL.DepthMask(true);
			GL.Disable(3042u);
			GL.Enable(2884u);
			GL.Enable(3553u);
		}

		public void DrawWeather(VoxelAI.World.World world, Camera cam, float dt)
		{
			float weatherIntensity = world.WeatherIntensity;
			if (weatherIntensity < 0.05f)
			{
				return;
			}
			int x = (int)Math.Floor(cam.Pos.X);
			int num = (int)Math.Floor(cam.Pos.Y);
			int z = (int)Math.Floor(cam.Pos.Z);
			bool flag = false;
			for (int i = num + 2; i < 128; i++)
			{
				if (world.IsOpaqueAt(x, i, z))
				{
					flag = true;
					break;
				}
			}
			if (flag)
			{
				return;
			}
			bool flag2 = world.CurrentWeather == Weather.Snowfall;
			bool flag3 = world.CurrentWeather == Weather.Storm;
			int num2 = (int)(900f * weatherIntensity);
			GL.Disable(3553u);
			GL.Enable(3042u);
			GL.BlendFunc(770u, 771u);
			GL.DepthMask(false);
			int num3 = 0;
			for (int j = 0; j < num2; j++)
			{
				if (wx[j] == 0f && wy2[j] == 0f && wz[j] == 0f)
				{
					wx[j] = cam.Pos.X + weatherRng.RangeF(-22f, 22f);
					wy2[j] = cam.Pos.Y + weatherRng.RangeF(6f, 22f);
					wz[j] = cam.Pos.Z + weatherRng.RangeF(-22f, 22f);
				}
				float num4 = (flag2 ? 2.2f : ((!flag3) ? 19f : 26f));
				wy2[j] -= num4 * dt;
				if (flag2)
				{
					wx[j] += (float)Math.Sin(Time2) * dt * 1.2f;
				}
				float num5 = wx[j] - cam.Pos.X;
				float num6 = wz[j] - cam.Pos.Z;
				if (wy2[j] < cam.Pos.Y - 16f || num5 * num5 + num6 * num6 > 900f)
				{
					wx[j] = cam.Pos.X + weatherRng.RangeF(-22f, 22f);
					wy2[j] = cam.Pos.Y + weatherRng.RangeF(8f, 22f);
					wz[j] = cam.Pos.Z + weatherRng.RangeF(-22f, 22f);
					continue;
				}
				float r = (flag2 ? 0.95f : ((!flag3) ? 0.6f : 0.5f));
				float g = ((!flag2) ? 0.66f : 0.97f);
				float bl = ((!flag2) ? 0.9f : 1f);
				float al = ((!flag2) ? 0.42f : 0.85f);
				if (flag2)
				{
					num3 = PushLine(weatherLine, num3, new Vec3(wx[j], wy2[j], wz[j]), new Vec3(wx[j], wy2[j] - 0.12f, wz[j]), r, g, bl, al);
				}
				else
				{
					Vec3 a = new Vec3(wx[j], wy2[j], wz[j]);
					Vec3 b = new Vec3(wx[j], wy2[j] - ((!flag3) ? 1f : 1.4f), wz[j]);
					num3 = PushLine(weatherLine, num3, a, b, r, g, bl, al);
				}
				if (num3 > weatherLine.Length - 16)
				{
					DrawLines(weatherLine, num3 / 8, (!flag3) ? 1.1f : 1.6f);
					num3 = 0;
				}
			}
			if (num3 > 0)
			{
				DrawLines(weatherLine, num3 / 8, (!flag3) ? 1.1f : 1.6f);
			}
			GL.DepthMask(true);
			GL.Disable(3042u);
			GL.Enable(3553u);
		}

		private static int PushLine(float[] arr, int v, Vec3 a, Vec3 b, float r, float g, float bl, float al)
		{
			arr[v] = a.X;
			arr[v + 1] = a.Y;
			arr[v + 2] = a.Z;
			arr[v + 3] = 0f;
			arr[v + 4] = 0f;
			arr[v + 5] = r;
			arr[v + 6] = g;
			arr[v + 7] = bl;
			v += 8;
			arr[v] = b.X;
			arr[v + 1] = b.Y;
			arr[v + 2] = b.Z;
			arr[v + 3] = 0f;
			arr[v + 4] = 0f;
			arr[v + 5] = r;
			arr[v + 6] = g;
			arr[v + 7] = bl;
			v += 8;
			return v;
		}

		private void DrawLines(float[] arr, int verts, float width)
		{
			if (verts > 0)
			{
				GL.LineWidth(width);
				GCHandle gCHandle = GCHandle.Alloc(arr, GCHandleType.Pinned);
				try
				{
					IntPtr pointer = gCHandle.AddrOfPinnedObject();
					GL.EnableClientState(32884u);
					GL.EnableClientState(32886u);
					GL.VertexPointer(3, 5126u, 32, pointer);
					GL.ColorPointer(3, 5126u, 32, (IntPtr)(pointer.ToInt64() + 20));
					GL.DrawArrays(1u, 0, verts);
				}
				finally
				{
					GL.DisableClientState(32886u);
					GL.DisableClientState(32884u);
					gCHandle.Free();
				}
				GL.LineWidth(1f);
			}
		}

		public void DrawHeldItem(byte block, float swing, bool visible, Camera cam)
		{
			if (visible && block != 0)
			{
				GL.Clear(256u);
				GL.MatrixMode(5888u);
				GL.LoadIdentity();
				GL.Translatef(0.62f - swing * 0.25f, -0.55f + swing * 0.25f, -1.05f);
				GL.Rotatef(-24f + swing * 40f, 0f, 1f, 0f);
				GL.Rotatef(12f, 1f, 0f, 0f);
				GL.BindTexture(3553u, Atlas.TextureId);
				GL.Enable(3553u);
				int num = MeshBuilder.BuildBlockCube(heldBuf, 0, new Vec3(-0.28f, -0.28f, -0.28f), 0.56f, block, 1.15f);
				DrawArray3D(heldBuf, num / 8, true);
				GL.LoadMatrixf(cam.View.A);
			}
		}

		public void DrawHighlight(Vec3i b, bool on)
		{
			if (on)
			{
				GL.Disable(3553u);
				GL.Disable(2884u);
				GL.LineWidth(2f);
				GL.Color4f(0.05f, 0.05f, 0.06f, 0.85f);
				MeshBuilder.EmitWireBox(new Vec3(b.X, b.Y, b.Z), new Vec3(b.X + 1, b.Y + 1, b.Z + 1), 0.003f);
				GL.LineWidth(1f);
				GL.Enable(2884u);
				GL.Enable(3553u);
			}
		}

		public void Tick(VoxelAI.World.World world, float dt)
		{
			Atlas.Update(dt);
			Time2 += dt;
		}
	}
}
