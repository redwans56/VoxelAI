using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using VoxelAI.AI;
using VoxelAI.Core;
using VoxelAI.Entities;
using VoxelAI.Gfx;
using VoxelAI.World;

namespace VoxelAI.Game
{
	public class Game
	{
		public Mode Current;

		public bool AutoCaptureMode;

		private int autoCaptureFrame;

		public readonly Renderer Renderer = new Renderer();

		public readonly Camera Camera = new Camera();

		public readonly InputState Input;

		public Player Player = new Player();

		public VoxelAI.World.World World;

		public AISystem AI;

		public GameWindow Window;

		private string worldName = "world1";

		private int menuIndex;

		private string[] menuItems = new string[3]
		{
			"Continue / Create World",
			"New World (random seed)",
			"Quit"
		};

		public float Fps;

		public float FrameMs;

		private float fpsTimer;

		private int fpsFrames;

		private float sessionTime;

		private List<Chunk> chunkSnapshot = new List<Chunk>();

		private float snapshotTimer;

		private string toast = string.Empty;

		private float toastTimer;

		private bool showLeaderboard;

		private bool showHelp;

		private bool showBrain;

		private bool showChronicle = true;

		private Agent inspected;

		private int selectedAgentIndex;

		private string playerHurtCause = string.Empty;

		public string LoadingText = "generating terrain";

		public int LoadingDone;

		public int LoadingTotal;

		public Game(InputState input)
		{
			Input = input;
		}

		public void Init()
		{
			Renderer.Init();
			Renderer.Resize(Camera.Width, Camera.Height);
		}

		public void SetMouseCaptured(bool on)
		{
			Input.SetCapture(on, Camera.Width, Camera.Height);
		}

		public void Resize(int w, int h)
		{
			Camera.Width = Math.Max(64, w);
			Camera.Height = Math.Max(64, h);
			Renderer.Resize(Camera.Width, Camera.Height);
			Input.SetCapture(Current == Mode.Playing, Camera.Width, Camera.Height);
		}

		public void StartWorld(string name, int seed)
		{
			if (World != null)
			{
				SaveGame();
				World.Shutdown();
			}
			worldName = name;
			string text = WorldSave.WorldDir(name);
			World = new VoxelAI.World.World(seed, text);
			World.DaylightChanged += delegate(VoxelAI.World.World w)
			{
				w.MarkAllDirty();
			};
			Dictionary<string, string> d = WorldSave.ReadMeta(text);
			int num = WorldSave.MetaInt(d, "seed", seed);
			if (num != seed)
			{
			}
			AI = new AISystem(World, num);
			string text2 = WorldSave.MetaString(d, "ai", string.Empty);
			if (text2.Length > 0)
			{
				AI.Load(text2);
			}
			Player = new Player();
			Player.Died += delegate(string cause)
			{
				Toast("you died (" + cause + ") - respawning");
				Player.Respawn(World);
			};
			Current = Mode.Loading;
			LoadingText = "generating world '" + name + "' (seed " + num + ")";
			LoadChunksAroundSpawn();
			Vec3 vec = World.FindSpawnPoint();
			Player.Position = vec;
			Camera.Pos = vec;
			Camera.Yaw = 0.6f;
			Camera.Pitch = -0.15f;
			if (AI.Agents.Count == 0)
			{
				AI.PopulateAt(vec, 10);
			}
			Player.Health = 100f;
			Current = Mode.Playing;
			Toast("WASD to move - H for help - Tab leaderboard - B brain inspector");
			Input.SetCapture(true, Camera.Width, Camera.Height);
		}

		private void LoadChunksAroundSpawn()
		{
			for (int i = -2; i <= 2; i++)
			{
				for (int j = -2; j <= 2; j++)
				{
					World.EnsureChunkLoaded(j, i, true);
				}
			}
			int num = 0;
			while (num++ < 900)
			{
				World.UpdateStreaming(new Vec3(0f, 70f, 0f), 0.05f);
				int num2 = 0;
				int num3 = 0;
				foreach (Chunk item in World.SnapshotChunks())
				{
					num3++;
					if (item.Lit)
					{
						num2++;
					}
				}
				LoadingDone = num2;
				LoadingTotal = num3;
				if (num3 >= 25 && num2 >= 20)
				{
					break;
				}
				Thread.Sleep(8);
			}
		}

		public void SaveGame()
		{
			if (World != null)
			{
				Dictionary<string, string> dictionary = new Dictionary<string, string>();
				dictionary["seed"] = World.Seed.ToString(CultureInfo.InvariantCulture);
				dictionary["time"] = World.TimeOfDay.ToString("0.####", CultureInfo.InvariantCulture);
				dictionary["player"] = string.Format(CultureInfo.InvariantCulture, "{0:0.##},{1:0.##},{2:0.##}", Player.Position.X, Player.Position.Y, Player.Position.Z);
				dictionary["ai"] = ((AI == null) ? string.Empty : AI.Save());
				dictionary["playtime"] = sessionTime.ToString("0.#", CultureInfo.InvariantCulture);
				WorldSave.WriteMeta(World.SaveDirectory, dictionary);
				World.SaveAll();
			}
		}

		public void Shutdown()
		{
			SaveGame();
			if (World != null)
			{
				World.Shutdown();
			}
			Current = Mode.Exiting;
		}

		public void Frame(float dt)
		{
			dt = M.Clamp(dt, 0.0005f, 0.1f);
			sessionTime += dt;
			fpsTimer += dt;
			fpsFrames++;
			if (fpsTimer > 0.5f)
			{
				Fps = (float)fpsFrames / fpsTimer;
				fpsTimer = 0f;
				fpsFrames = 0;
			}
			if (toastTimer > 0f)
			{
				toastTimer -= dt;
			}
			switch (Current)
			{
			case Mode.Menu:
				UpdateMenu();
				break;
			case Mode.Loading:
				DrawLoading();
				break;
			case Mode.Playing:
				UpdatePlaying(dt);
				break;
			case Mode.Paused:
				UpdatePaused();
				break;
			}
			if (AutoCaptureMode)
			{
				HandleAutoCapture();
			}
		}

		private void HandleAutoCapture()
		{
			autoCaptureFrame++;
			string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "screenshots");
			if (!Directory.Exists(dir))
			{
				Directory.CreateDirectory(dir);
			}
			if (Current == Mode.Menu)
			{
				if (autoCaptureFrame == 12)
				{
					Renderer.SaveScreenshot(Camera.Width, Camera.Height, Path.Combine(dir, "01_main_menu.png"));
					StartWorld("world1", 20260517);
					autoCaptureFrame = 0;
				}
			}
			else if (Current == Mode.Playing)
			{
				if (autoCaptureFrame == 50)
				{
					Player.Pitch = -0.15f;
					Player.Yaw = 1.15f;
					Camera.Pitch = Player.Pitch;
					Camera.Yaw = Player.Yaw;
					if (World.Items.Count == 0)
					{
						World.SpawnDrop((int)Player.Position.X + 2, (int)Player.Position.Y, (int)Player.Position.Z + 2, 3, 4);
						World.SpawnDrop((int)Player.Position.X - 2, (int)Player.Position.Y, (int)Player.Position.Z + 2, 7, 2);
					}
					Renderer.SaveScreenshot(Camera.Width, Camera.Height, Path.Combine(dir, "02_voxel_world.png"));
					showBrain = true;
					if (AI != null && AI.Agents.Count > 0)
					{
						inspected = AI.Agents[0];
						inspected.IsSelected = true;
					}
				}
				else if (autoCaptureFrame == 75)
				{
					Renderer.SaveScreenshot(Camera.Width, Camera.Height, Path.Combine(dir, "03_ai_brain_hud.png"));
					showBrain = false;
					showLeaderboard = true;
				}
				else if (autoCaptureFrame == 100)
				{
					Renderer.SaveScreenshot(Camera.Width, Camera.Height, Path.Combine(dir, "04_ai_leaderboard.png"));
				}
				else if (autoCaptureFrame >= 125)
				{
					Current = Mode.Exiting;
					if (Window != null)
					{
						Window.Close();
					}
				}
			}
		}

		private void UpdateMenu()
		{
			if (Input.Pressed(Keys.Up))
			{
				menuIndex = (menuIndex + menuItems.Length - 1) % menuItems.Length;
			}
			if (Input.Pressed(Keys.Down))
			{
				menuIndex = (menuIndex + 1) % menuItems.Length;
			}
			if (Input.Pressed(Keys.Return) || Input.Pressed(Keys.Space))
			{
				if (menuIndex == 0)
				{
					Dictionary<string, string> d = WorldSave.ReadMeta(WorldSave.WorldDir("world1"));
					StartWorld("world1", WorldSave.MetaInt(d, "seed", 20260517));
				}
				else if (menuIndex == 1)
				{
					int seed = Environment.TickCount ^ new Random().Next();
					StartWorld("world1", seed);
				}
				else
				{
					Current = Mode.Exiting;
				}
			}
			Renderer.BeginFrameMenu(Camera);
			UiMenu();
		}

		private void UiMenu()
		{
			Ui2D ui = Renderer.Ui;
			ui.Begin(Camera.Width, Camera.Height);
			float cx = (float)Camera.Width * 0.5f;

			ui.Rect(0f, 0f, Camera.Width, Camera.Height, 0.03f, 0.04f, 0.06f, 1f);
			ui.Outline(16f, 16f, Camera.Width - 32f, Camera.Height - 32f, 0.15f, 0.22f, 0.32f, 0.8f, 2f);

			float titleW = 640f;
			float titleH = 100f;
			ui.PixelPanel(cx - titleW * 0.5f, 55f, titleW, titleH, 0.05f, 0.07f, 0.11f, 0.92f, 0.25f, 0.55f, 0.85f);
			ui.TextCentered(cx, 72f, "VOXEL AI", 5.5f, 0.45f, 0.85f, 1f, 1f);
			ui.TextCentered(cx, 126f, "AN AUTONOMOUS MULTI-AGENT EVOLUTIONARY VOXEL SANDBOX", 1.25f, 0.7f, 0.82f, 0.95f, 0.95f);

			float btnW = 390f;
			float btnH = 44f;
			float startY = (float)Camera.Height * 0.40f;
			for (int i = 0; i < menuItems.Length; i++)
			{
				bool selected = (i == menuIndex);
				float btnY = startY + (float)i * 54f;
				if (selected)
				{
					ui.PixelPanel(cx - btnW * 0.5f - 4f, btnY - 2f, btnW + 8f, btnH + 4f, 0.12f, 0.16f, 0.24f, 0.95f, 1f, 0.85f, 0.3f);
					ui.TextCentered(cx, btnY + 12f, ">  " + menuItems[i] + "  <", 1.8f, 1f, 0.92f, 0.45f, 1f);
				}
				else
				{
					ui.PixelPanel(cx - btnW * 0.5f, btnY, btnW, btnH, 0.05f, 0.07f, 0.1f, 0.85f, 0.2f, 0.26f, 0.35f);
					ui.TextCentered(cx, btnY + 13f, menuItems[i], 1.7f, 0.75f, 0.82f, 0.9f, 0.9f);
				}
			}

			string[] array = WorldSave.ListWorlds();
			string saveStr = "SAVED SEEDS: " + ((array.Length != 0) ? string.Join(", ", array) : "NONE YET");
			ui.PixelBadge(cx - (ui.TextWidth(saveStr, 1.1f) + 20f) * 0.5f, (float)Camera.Height * 0.74f, saveStr, 1.1f, 0.06f, 0.1f, 0.15f, 0.55f, 0.75f, 0.95f);

			ui.TextCentered(cx, (float)Camera.Height - 55f, "ENTER / SPACE : SELECT      UP / DOWN : NAVIGATE", 1.2f, 0.6f, 0.7f, 0.8f, 0.95f);
			ui.End();
		}

		private void DrawLoading()
		{
			Ui2D ui = Renderer.Ui;
			ui.Begin(Camera.Width, Camera.Height);
			float num = (float)Camera.Width * 0.5f;
			float num2 = (float)Camera.Height * 0.5f;
			ui.Rect(0f, 0f, Camera.Width, Camera.Height, 0.03f, 0.04f, 0.06f, 1f);
			ui.TextCentered(num, num2 - 70f, "VOXEL AI", 5f, 0.45f, 0.85f, 1f, 1f);
			ui.TextCentered(num, num2 - 10f, LoadingText, 1.6f, 0.85f, 0.92f, 1f, 1f);
			float num3 = ((LoadingTotal <= 0) ? 0f : M.Clamp((float)LoadingDone / (float)Math.Max(1, LoadingTotal), 0f, 1f));
			ui.PixelBar(num - 220f, num2 + 20f, 440f, 20f, num3, 0.3f, 0.75f, 1f, 1f);
			ui.TextCentered(num, num2 + 55f, "CHUNKS COMPILED & LIT: " + LoadingDone + " / " + LoadingTotal, 1.25f, 0.7f, 0.8f, 0.9f, 1f);
			ui.TextCentered(num, (float)Camera.Height - 60f, Renderer.GLInfo, 1.1f, 0.45f, 0.55f, 0.65f, 0.9f);
			ui.End();
			if (LoadingDone >= 12 && LoadingTotal >= 20)
			{
				Current = Mode.Playing;
			}
		}

		private void UpdatePlaying(float dt)
		{
			HandleKeys();
			if (Input.MouseCaptured)
			{
				Player.Look(Input, 0.0032f);
			}
			bool jump;
			Player.HandleInput(World, Input, dt, out jump);
			Player.StepPhysics(World, dt);
			Player.UpdateTarget(World, dt, Input.MouseLeft && !showBrain && !showLeaderboard);
			if (Input.MouseRightJustPressed)
			{
				Player.PlaceAtTarget(World);
			}
			if (Input.Pressed(Keys.Q))
			{
				Player.DropOne(World);
			}
			for (int i = 0; i < 9; i++)
			{
				if (Input.Pressed((Keys)(49 + i)))
				{
					Player.HotbarIndex = i;
				}
			}
			if (Input.WheelDelta != 0)
			{
				Player.HotbarIndex = M.Mod(Player.HotbarIndex - Math.Sign(Input.WheelDelta), 9);
			}
			if (Input.MouseLeftJustPressed)
			{
				TryPunchCreature();
			}
			if (Player.Swing > 0f)
			{
				Player.Swing = Math.Max(0f, Player.Swing - dt * 3.5f);
			}
			Camera.Pos = Player.Position.Add(0f, 1.62f + Player.Bob, 0f);
			Camera.Yaw = Player.Yaw;
			Camera.Pitch = Player.Pitch;
			Camera.Update(Camera.Width, Camera.Height);
			World.Update(Player.Position, dt);
			if (AI != null)
			{
				AI.Update(dt);
			}
			Renderer.Tick(World, dt);
			chunkSnapshot.Clear();
			chunkSnapshot.AddRange(World.SnapshotChunks());
			World.ProcessMeshWork(Player.Position, 7f, delegate(Chunk c)
			{
				Renderer.BuildChunkMeshes(World, c);
			});
			Draw();
			if (showBrain && (inspected == null || inspected.Dead))
			{
				inspected = AI.FindNearest(Player.Position, 200f);
				for (int j = 0; j < AI.Agents.Count; j++)
				{
					AI.Agents[j].IsSelected = false;
				}
				if (inspected != null)
				{
					inspected.IsSelected = true;
				}
			}
			if (Current == Mode.Exiting && Window != null)
			{
				Window.Close();
			}
		}

		private void TryPunchCreature()
		{
			Vec3 eye = Player.Eye;
			Vec3 lookDir = Player.LookDir;
			for (int i = 0; i < World.Creatures.Count; i++)
			{
				Creature creature = World.Creatures[i];
				if (!creature.Alive)
				{
					continue;
				}
				Vec3 a = creature.Position.Add(0f, 0.4f, 0f) - eye;
				float length = a.Length;
				if (length > Player.Reach + 1.4f)
				{
					continue;
				}
				Vec3 a2 = a / length;
				if (!(Vec3.Dot(a2, lookDir) < 0.955f))
				{
					Agent agent = FindAgentFor(creature);
					if (agent != null)
					{
						AI.Damage(agent, 8f, null, "combat");
						Player.Swing = 1f;
						Toast(agent.Name + " g" + agent.Generation + " was hit (" + (int)agent.Health + " hp)");
						agent.Brain.MarkDanger((int)creature.Position.X, (int)creature.Position.Y, (int)creature.Position.Z, 0.7f);
					}
					break;
				}
			}
		}

		private Agent FindAgentFor(Creature c)
		{
			if (AI == null)
			{
				return null;
			}
			for (int i = 0; i < AI.Agents.Count; i++)
			{
				if (AI.Agents[i].Body == c)
				{
					return AI.Agents[i];
				}
			}
			return null;
		}

		private void HandleKeys()
		{
			if (Input.Pressed(Keys.Escape))
			{
				Current = Mode.Paused;
				Input.SetCapture(false, Camera.Width, Camera.Height);
			}
			if (Input.Pressed(Keys.H))
			{
				showHelp = !showHelp;
			}
			if (Input.Pressed(Keys.F2))
			{
				string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "screenshots");
				string fn = Path.Combine(dir, "screenshot_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png");
				Renderer.SaveScreenshot(Camera.Width, Camera.Height, fn);
				Toast("Screenshot saved to " + Path.GetFileName(fn));
			}
			if (Input.Pressed(Keys.Tab))
			{
				showLeaderboard = !showLeaderboard;
			}
			if (Input.Pressed(Keys.B) || Input.Pressed(Keys.O))
			{
				showBrain = !showBrain;
				if (showBrain)
				{
					SelectNextAgent();
				}
			}
			if (Input.Pressed(Keys.C))
			{
				showChronicle = !showChronicle;
			}
			if (Input.Pressed(Keys.F))
			{
				Player.Flying = !Player.Flying;
				Toast((!Player.Flying) ? "flying disabled" : "flying enabled");
			}
			if (Input.Pressed(Keys.G))
			{
				Player.Creative = !Player.Creative;
				Toast((!Player.Creative) ? "survival mode" : "creative mode");
			}
			if (Input.Pressed(Keys.T))
			{
				World.FrozenTime = !World.FrozenTime;
				Toast((!World.FrozenTime) ? "time running" : "time frozen");
			}
			if (Input.Pressed(Keys.N))
			{
				AI.EvolveNow();
				Toast("AI: forced evolution step");
			}

			if (Input.Pressed(Keys.M))
			{
				AI.MutationRate = M.Clamp(AI.MutationRate + 0.05f, 0f, 0.8f);
				Toast("mutation rate " + AI.MutationRate.ToString("0.00"));
			}
			if (Input.Pressed(Keys.Oemcomma))
			{
				AI.MutationRate = M.Clamp(AI.MutationRate - 0.05f, 0f, 0.8f);
				Toast("mutation rate " + AI.MutationRate.ToString("0.00"));
			}
			if (Input.Pressed(Keys.K))
			{
				Player.Health = 100f;
				Player.Respawn(World);
				Toast("respawned");
			}
			if (Input.Pressed(Keys.R))
			{
				SelectNextAgent();
			}
			if (Input.Pressed(Keys.P))
			{
				Toast("world seed " + World.Seed + " - chunk " + M.FloorDiv((int)Player.Position.X, 16) + "," + M.FloorDiv((int)Player.Position.Z, 16));
			}
		}

		private void SelectNextAgent()
		{
			if (AI != null && AI.Agents.Count != 0)
			{
				for (int i = 0; i < AI.Agents.Count; i++)
				{
					AI.Agents[i].IsSelected = false;
				}
				selectedAgentIndex = (selectedAgentIndex + 1) % AI.Agents.Count;
				inspected = AI.Agents[selectedAgentIndex];
				inspected.IsSelected = true;
				Toast("inspecting " + inspected.Name + " g" + inspected.Generation + " - " + inspected.G.Describe());
			}
		}

		private void UpdatePaused()
		{
			Renderer.BeginFrameMenu(Camera);
			Ui2D ui = Renderer.Ui;
			ui.Begin(Camera.Width, Camera.Height);
			ui.Rect(0f, 0f, Camera.Width, Camera.Height, 0f, 0f, 0f, 0.55f);
			float cx = (float)Camera.Width * 0.5f;
			ui.TextCentered(cx, 180f, "PAUSED", 5f, 1f, 1f, 1f, 1f);
			ui.TextCentered(cx, 260f, "ESC  RESUME", 2f, 0.85f, 0.9f, 1f, 1f);
			ui.TextCentered(cx, 292f, "S    SAVE WORLD + AI BRAINS", 2f, 0.85f, 0.9f, 1f, 1f);
			ui.TextCentered(cx, 324f, "N    NEW RANDOM WORLD", 2f, 0.85f, 0.9f, 1f, 1f);
			ui.TextCentered(cx, 356f, "X    SAVE AND QUIT", 2f, 0.85f, 0.9f, 1f, 1f);
			ui.TextCentered(cx, 420f, (AI == null) ? string.Empty : AI.StatusLine(), 1.6f, 0.7f, 0.85f, 1f, 1f);
			ui.TextCentered(cx, 452f, (AI == null) ? string.Empty : AI.Species.Summary(), 1.4f, 0.6f, 0.75f, 0.95f, 1f);
			ui.End();
			if (Input.Pressed(Keys.Escape))
			{
				Current = Mode.Playing;
				Input.SetCapture(true, Camera.Width, Camera.Height);
			}
			if (Input.Pressed(Keys.S))
			{
				SaveGame();
				Toast("saved");
			}
			if (Input.Pressed(Keys.N))
			{
				int seed = Environment.TickCount ^ new Random().Next();
				StartWorld(worldName, seed);
			}
			if (Input.Pressed(Keys.X))
			{
				SaveGame();
				Current = Mode.Exiting;
				if (Window != null)
				{
					Window.Close();
				}
			}
		}

		private void Draw()
		{
			Renderer.BeginFrame(World, Camera);
			List<Chunk> chunks = chunkSnapshot;
			Renderer.DrawChunks(World, Camera, chunks, false);
			Renderer.DrawCreatures(World, Camera, AI);
			Renderer.DrawItems(World, Camera);
			Renderer.DrawHighlight(Player.TargetBlock, Player.HasTarget && !showBrain);
			Renderer.DrawChunks(World, Camera, chunks, true);
			Renderer.DrawClouds(World, Camera, 0.016f);
			Renderer.DrawParticles(World, Camera);
			Renderer.DrawWeather(World, Camera, 0.016f);
			Renderer.DrawHeldItem(Player.SelectedBlock, Player.Swing, true, Camera);
			DrawHud();
		}

		private void DrawHud()
		{
			Ui2D ui = Renderer.Ui;
			ui.Begin(Camera.Width, Camera.Height);
			float s = ((Camera.Width < 1600) ? 1.35f : 1.6f);
			float cx = (float)Camera.Width * 0.5f;
			float cy = (float)Camera.Height * 0.5f;

			// --- Pixel Crosshair ---
			ui.Rect(cx - 10f, cy - 2f, 20f, 4f, 0f, 0f, 0f, 0.65f);
			ui.Rect(cx - 2f, cy - 10f, 4f, 20f, 0f, 0f, 0f, 0.65f);
			ui.Rect(cx - 9f, cy - 1f, 6f, 2f, 1f, 1f, 1f, 0.95f);
			ui.Rect(cx + 3f, cy - 1f, 6f, 2f, 1f, 1f, 1f, 0.95f);
			ui.Rect(cx - 1f, cy - 9f, 2f, 6f, 1f, 1f, 1f, 0.95f);
			ui.Rect(cx - 1f, cy + 3f, 2f, 6f, 1f, 1f, 1f, 0.95f);
			ui.Rect(cx - 1f, cy - 1f, 2f, 2f, 0.3f, 1f, 0.5f, 0.9f);

			int px = M.FloorInt(Player.Position.X);
			int py = M.FloorInt(Player.Position.Y);
			int pz = M.FloorInt(Player.Position.Z);

			// --- Top-Left System & Telemetry Card ---
			float cardW = 390f;
			float cardH = 82f;
			ui.PixelPanel(12f, 12f, cardW, cardH, 0.04f, 0.06f, 0.09f, 0.85f, 0.22f, 0.32f, 0.44f);
			ui.PixelBadge(16f, 16f, string.Format(CultureInfo.InvariantCulture, "VOXEL AI v1.2  {0:0} FPS ({1:0.0}ms)", Fps, FrameMs), s * 0.82f, 0.12f, 0.22f, 0.32f, 0.4f, 0.9f, 1f);

			string tLine1 = string.Format(CultureInfo.InvariantCulture, "POS {0}, {1}, {2}  [CHUNK {3},{4}]", px, py, pz, M.FloorDiv(px, 16), M.FloorDiv(pz, 16));
			string tLine2 = string.Format(CultureInfo.InvariantCulture, "{0} | {1} | LIGHT {2} (SUN {3:0.00})", World.BiomeAt(px, pz).ToString().ToUpperInvariant(), World.CurrentWeather.ToString().ToUpperInvariant(), World.LightAt(px, py, pz), World.Daylight);
			string tLine3 = string.Format(CultureInfo.InvariantCulture, "MODE {0} | TIME {1:0.00} | CHUNKS {2}/{3}", Player.Flying ? "FLY" : (Player.Creative ? "CREATIVE" : "SURVIVAL"), World.TimeOfDay, Renderer.DrawnChunks, World.LoadedChunkCount);

			ui.TextShadow(18f, 38f, tLine1, s * 0.8f, 0.85f, 0.92f, 1f, 0.95f);
			ui.TextShadow(18f, 52f, tLine2, s * 0.78f, 0.72f, 0.82f, 0.92f, 0.9f);
			ui.TextShadow(18f, 66f, tLine3, s * 0.75f, 0.58f, 0.7f, 0.8f, 0.85f);

			// --- Top-Right AI Colony Monitor Card ---
			if (AI != null)
			{
				float colW = 430f;
				float colH = 82f;
				float colX = (float)Camera.Width - colW - 12f;
				ui.PixelPanel(colX, 12f, colW, colH, 0.04f, 0.06f, 0.09f, 0.85f, 0.18f, 0.38f, 0.32f);
				ui.PixelBadge(colX + 6f, 16f, "COLONY NEURAL LINK ACTIVE", s * 0.82f, 0.08f, 0.25f, 0.18f, 0.4f, 1f, 0.6f);

				string aLine1 = AI.StatusLine();
				string aLine2 = string.Format(CultureInfo.InvariantCulture, "{0} | {1} CELLS EXPLORED", AI.Species.Summary(), AI.VisitedCells);
				string aLine3 = "[TAB] LEADERBOARD   [B] NEURAL CORTEX   [O] EVOLVE";

				ui.TextShadow(colX + 8f, 38f, aLine1, s * 0.8f, 0.88f, 0.95f, 0.88f, 0.95f);
				ui.TextShadow(colX + 8f, 52f, aLine2, s * 0.78f, 0.75f, 0.88f, 0.82f, 0.9f);
				ui.TextShadow(colX + 8f, 66f, aLine3, s * 0.75f, 1f, 0.85f, 0.4f, 0.95f);
			}

			// --- Bottom-Left Health & Status Card ---
			float hpY = Camera.Height - 64;
			float hpW = 210f;
			ui.PixelPanel(12f, hpY, hpW, 48f, 0.04f, 0.06f, 0.09f, 0.85f, 0.35f, 0.18f, 0.22f);
			float hpFrac = M.Clamp(Player.Health / 100f, 0f, 1f);
			string hpStr = string.Format(CultureInfo.InvariantCulture, "VITALS  {0}/100 HP", (int)Player.Health);
			ui.TextShadow(18f, hpY + 6f, hpStr, s * 0.82f, 1f, 0.88f, 0.88f, 0.95f);
			float barR = M.Lerp(1f, 0.2f, hpFrac);
			float barG = M.Lerp(0.2f, 0.92f, hpFrac);
			float barB = 0.25f;
			ui.PixelBar(18f, hpY + 22f, hpW - 12f, 16f, hpFrac, barR, barG, barB, 0.95f);

			// --- Bottom-Center Retro Hotbar ---
			int slots = 9;
			float slotSize = 48f;
			float slotGap = 6f;
			float totalBarW = (float)slots * slotSize + (float)(slots - 1) * slotGap;
			float barLeft = ((float)Camera.Width - totalBarW) * 0.5f;
			float barY = Camera.Height - 68;

			ui.PixelPanel(barLeft - 8f, barY - 6f, totalBarW + 16f, slotSize + 12f, 0.03f, 0.04f, 0.06f, 0.88f, 0.2f, 0.24f, 0.3f);

			for (int k = 0; k < slots; k++)
			{
				float sx = barLeft + (float)k * (slotSize + slotGap);
				bool selected = (k == Player.HotbarIndex);

				if (selected)
				{
					ui.PixelPanel(sx - 2f, barY - 2f, slotSize + 4f, slotSize + 4f, 0.15f, 0.14f, 0.08f, 0.95f, 1f, 0.88f, 0.25f);
				}
				else
				{
					ui.PixelPanel(sx, barY, slotSize, slotSize, 0.07f, 0.08f, 0.11f, 0.85f, 0.18f, 0.22f, 0.28f);
				}

				byte b = Player.Hotbar[k];
				if (b != 0)
				{
					float iconSize = 34f;
					float iconX = sx + (slotSize - iconSize) * 0.5f;
					float iconY = barY + (slotSize - iconSize) * 0.5f;
					GL.BindTexture(3553u, Renderer.Atlas.TextureId);
					ui.TileRect(iconX, iconY, iconSize, iconSize, Blocks.Info((B)b).TileSide, 1f, 1f, 1f, 1f);
					if (!Player.Creative)
					{
						int count = Player.CountOf(b);
						if (count > 1)
						{
							string cStr = count.ToString(CultureInfo.InvariantCulture);
							ui.TextShadow(sx + slotSize - ui.TextWidth(cStr, s * 0.75f) - 4f, barY + slotSize - 12f, cStr, s * 0.75f, 1f, 1f, 0.9f, 1f);
						}
					}
				}
				ui.TextShadow(sx + 4f, barY + 3f, (k + 1).ToString(CultureInfo.InvariantCulture), s * 0.75f, 0.65f, 0.75f, 0.85f, 0.85f);
			}

			byte sel = Player.SelectedBlock;
			if (sel != 0)
			{
				string itemName = Blocks.Name((B)sel).ToUpperInvariant() + (Player.Creative ? "  [CREATIVE]" : string.Format(CultureInfo.InvariantCulture, "  [{0}]", Player.CountOf(sel)));
				float itemBadgeW = ui.TextWidth(itemName, s * 0.9f) + 20f;
				float itemBadgeX = cx - itemBadgeW * 0.5f;
				float itemBadgeY = barY - 34f;
				ui.PixelBadge(itemBadgeX, itemBadgeY, itemName, s * 0.85f, 0.08f, 0.12f, 0.18f, 1f, 0.95f, 0.7f);
			}

			if (Player.MineProgress > 0f && Player.HasTarget)
			{
				float hardness = Math.Max(0.001f, Blocks.Info((B)World.GetBlock(Player.TargetBlock.X, Player.TargetBlock.Y, Player.TargetBlock.Z)).Hardness);
				float mineFrac = M.Clamp(Player.MineProgress / hardness, 0f, 1f);
				ui.PixelBar(cx - 50f, cy + 28f, 100f, 10f, mineFrac, 1f, 0.85f, 0.2f, 0.95f);
			}

			DrawAgentLabels(ui, s);

			if (showChronicle && AI != null && AI.Chronicle.Count > 0)
			{
				int numLines = Math.Min(5, AI.Chronicle.Count);
				float chrW = 520f;
				float chrH = 20f + (float)numLines * 14f;
				float chrY = (float)(Camera.Height - 120) - chrH;
				ui.PixelPanel(12f, chrY, chrW, chrH, 0.04f, 0.05f, 0.08f, 0.8f, 0.35f, 0.28f, 0.15f);
				ui.TextShadow(18f, chrY + 4f, "COLONY CHRONICLE", s * 0.75f, 1f, 0.85f, 0.45f, 0.95f);
				for (int l = 0; l < numLines; l++)
				{
					string text = AI.Chronicle[AI.Chronicle.Count - numLines + l];
					if (text.Length > 75)
					{
						text = text.Substring(0, 75) + "...";
					}
					ui.TextShadow(18f, chrY + 18f + (float)l * 13f, text, s * 0.72f, 0.85f, 0.92f, 0.98f, 0.9f);
				}
			}

			if (toastTimer > 0f && !showLeaderboard && !showBrain)
			{
				float toastW = ui.TextWidth(toast, s * 1.1f) + 24f;
				ui.PixelPanel(cx - toastW * 0.5f, 100f, toastW, 28f, 0.06f, 0.08f, 0.12f, M.Clamp(toastTimer, 0f, 0.92f), 1f, 0.85f, 0.3f, M.Clamp(toastTimer, 0f, 1f));
				ui.TextCentered(cx, 106f, toast, s * 1.05f, 1f, 0.95f, 0.6f, M.Clamp(toastTimer, 0f, 1f));
			}

			if (showLeaderboard)
			{
				DrawLeaderboard(ui, s);
			}
			if (showBrain)
			{
				DrawBrainPanel(ui, s);
			}
			if (showHelp)
			{
				DrawHelp(ui, s);
			}
			ui.End();
		}

		private void DrawAgentLabels(Ui2D ui, float s)
		{
			if (AI == null)
			{
				return;
			}
			for (int i = 0; i < AI.Agents.Count; i++)
			{
				Agent agent = AI.Agents[i];
				if (agent.Dead)
				{
					continue;
				}
				Vec3 world = agent.Body.Position.Add(0f, 1.9f, 0f);
				float dist = Vec3.Distance(agent.Body.Position, Camera.Pos);
				if (dist > 36f)
				{
					continue;
				}
				if (!World.HasLineOfSight(Camera.Pos, world))
				{
					continue;
				}
				float sx;
				float sy;
				float depth;
				if (Camera.WorldToScreen(world, out sx, out sy, out depth) && !(sx < -100f) && !(sy < -40f) && !(sx > (float)(Camera.Width + 100)) && !(sy > (float)(Camera.Height + 40)))
				{
					string s2 = agent.Name + " [G" + agent.Generation + "]";
					string s3 = string.Concat(agent.Action, ": ", agent.Status);
					float scale = M.Clamp(1.5f - dist * 0.025f, 0.85f, 1.4f);
					float tw = Math.Max(ui.TextWidth(s2, scale), ui.TextWidth(s3, scale * 0.8f));
					float panelW = tw + 16f;
					float panelH = 34f * scale;
					float px = sx - panelW * 0.5f;
					float py = sy - panelH - 4f;

					ui.PixelPanel(px, py, panelW, panelH, 0.06f, 0.08f, 0.12f, 0.82f, agent.IsSelected ? 1f : 0.4f, agent.IsSelected ? 0.9f : 0.6f, agent.IsSelected ? 0.2f : 0.8f);
					ui.Text(px + 8f, py + 4f, s2, scale, agent.Body.HeadR, agent.Body.HeadG, agent.Body.HeadB, 1f);
					ui.Text(px + 8f, py + 14f * scale, s3, scale * 0.8f, 0.85f, 0.92f, 1f, 0.95f);

					float barY = py + panelH - 8f;
					ui.PixelBar(px + 6f, barY, panelW - 12f, 4f, agent.HealthFrac, 0.85f, 0.2f, 0.2f, 0.9f);
				}
			}
		}

		private void DrawLeaderboard(Ui2D ui, float s)
		{
			if (AI == null)
			{
				return;
			}
			float w = Math.Min(920f, Camera.Width - 60);
			int count = Math.Min(AI.Agents.Count, 16);
			float rowH = 18f;
			float h = 110f + (float)count * rowH;
			float lx = ((float)Camera.Width - w) * 0.5f;
			float ly = 100f;

			ui.PixelPanel(lx, ly, w, h, 0.04f, 0.06f, 0.1f, 0.92f, 0.3f, 0.5f, 0.7f);
			ui.PixelBadge(lx + 14f, ly + 10f, "AI SPECIES LEADERBOARD  [TAB TO DISMISS]", s * 1.05f, 0.1f, 0.2f, 0.35f, 0.4f, 0.85f, 1f);

			string header = string.Format("{0,-4} {1,-10} {2,-4} {3,8} {4,7} {5,7} {6,13} {7,6} {8,6} {9,5} {10,6}",
				"#", "NAME", "GEN", "FITNESS", "MINED", "PLACED", "ACTION", "KILLS", "DEATHS", "ORE", "SKILL");
			ui.Text(lx + 16f, ly + 36f, header, s * 0.82f, 0.7f, 0.82f, 0.95f, 0.95f);
			ui.Rect(lx + 12f, ly + 50f, w - 24f, 1f, 0.25f, 0.35f, 0.45f, 0.8f);

			List<Agent> list = AI.Leaderboard();
			for (int i = 0; i < count; i++)
			{
				Agent agent = list[i];
				string rankStr = (i == 0) ? " 1st" : ((i == 1) ? " 2nd" : ((i == 2) ? " 3rd" : string.Format("{0,3}.", i + 1)));
				string actStr = (!agent.Dead) ? agent.Action.ToString() : "RESPAWN";
				string row = string.Format(CultureInfo.InvariantCulture, "{0,-4} {1,-10} {2,-4} {3,8:0} {4,7} {5,7} {6,13} {7,6} {8,6} {9,5} {10,6:0.00}",
					rankStr, agent.Name, agent.Generation, agent.Fitness, agent.MinedTotal, agent.PlacementsTotal, actStr, agent.Kills, agent.Deaths, agent.OresFound, agent.Brain.SkillMining);

				float rowY = ly + 56f + (float)i * rowH;
				if (i == 0)
				{
					ui.Rect(lx + 12f, rowY - 2f, w - 24f, rowH - 2f, 0.3f, 0.25f, 0.05f, 0.35f);
				}
				else if (i % 2 == 1)
				{
					ui.Rect(lx + 12f, rowY - 2f, w - 24f, rowH - 2f, 0.1f, 0.12f, 0.18f, 0.25f);
				}

				float r = (i == 0) ? 1f : ((i == 1) ? 0.85f : ((i == 2) ? 0.95f : agent.Body.HeadR));
				float g = (i == 0) ? 0.85f : ((i == 1) ? 0.88f : ((i == 2) ? 0.65f : agent.Body.HeadG));
				float b = (i == 0) ? 0.2f : ((i == 1) ? 0.92f : ((i == 2) ? 0.35f : agent.Body.HeadB));

				ui.Text(lx + 16f, rowY, row, s * 0.82f, r, g, b, (!agent.Dead) ? 1f : 0.5f);
			}

			ui.Text(lx + 16f, ly + h - 22f, "FITNESS FORMULA: MINING + BUILDING + CRAFTING + KILLS*20 + SURVIVAL*0.5 - DEATHS*15", s * 0.75f, 0.55f, 0.7f, 0.8f, 0.95f);
		}

		private void DrawBrainPanel(Ui2D ui, float s)
		{
			if (AI == null)
			{
				return;
			}
			if (inspected == null)
			{
				inspected = AI.FindNearest(Player.Position, 300f);
				if (inspected != null)
				{
					inspected.IsSelected = true;
				}
			}
			float w = 480f;
			float bx = 16f;
			float by = 100f;
			if (inspected == null)
			{
				ui.PixelPanel(bx, by, w, 60f, 0.06f, 0.08f, 0.12f, 0.85f, 0.8f, 0.5f, 0.2f);
				ui.Text(bx + 14f, by + 18f, "NO AGENT IN RANGE TO INSPECT (PRESS R)", s, 1f, 0.8f, 0.5f, 1f);
				return;
			}

			Agent agent2 = inspected;
			float h = 580f;
			ui.PixelPanel(bx, by, w, h, 0.04f, 0.06f, 0.1f, 0.92f, 0.25f, 0.85f, 0.5f);

			float py = by + 10f;
			ui.PixelBadge(bx + 12f, py, "NEURAL CORTEX: " + agent2.Name + " [GEN " + agent2.Generation + "]", s * 1.05f, 0.08f, 0.25f, 0.16f, 0.4f, 1f, 0.7f);
			py += 26f;

			ui.Text(bx + 14f, py, agent2.G.Describe(), s * 0.88f, 0.9f, 1f, 0.9f, 1f);
			py += 18f;
			ui.Text(bx + 14f, py, string.Format(CultureInfo.InvariantCulture, "FIT {0:0}   HP {1:0}/100   AGE {2:0}S   DEATHS {3}   CARRY {4}/{5}", agent2.Fitness, agent2.Health, agent2.Age, agent2.Deaths, agent2.CarryCount, agent2.CarryLimit), s * 0.82f, 0.85f, 0.9f, 1f, 1f);
			py += 17f;
			ui.Text(bx + 14f, py, string.Concat("STATE: ", agent2.Action, " - ", agent2.Status), s * 0.82f, 1f, 0.9f, 0.55f, 1f);
			py += 17f;
			ui.Text(bx + 14f, py, "METRICS: MINED " + agent2.MinedTotal + " | PLACED " + agent2.PlacementsTotal + " | CRAFTED " + agent2.CraftsTotal + " | HUTS " + agent2.StructuresBuilt + " | ORE " + agent2.OresFound, s * 0.78f, 0.75f, 0.88f, 0.95f, 1f);
			py += 22f;

			ui.Text(bx + 14f, py, "EVOLVED GENOMIC TRAITS", s * 0.9f, 0.95f, 0.8f, 1f, 1f);
			py += 18f;
			for (int i = 0; i < 12; i++)
			{
				float val = agent2.G.V[i];
				float barLen = 160f;
				ui.Text(bx + 14f, py, Gene.Name(i), s * 0.78f, 0.82f, 0.88f, 0.95f, 0.95f);
				ui.PixelBar(bx + 140f, py + 1f, barLen, 8f, val, 0.4f + val * 0.5f, 0.8f - val * 0.3f, 0.4f, 0.95f);
				ui.Text(bx + 140f + barLen + 8f, py, (val * 100f).ToString("0", CultureInfo.InvariantCulture) + "%", s * 0.78f, 1f, 1f, 1f, 0.95f);
				py += 13f;
			}

			py += 8f;
			ui.Text(bx + 14f, py, "REINFORCEMENT SKILLS", s * 0.9f, 0.4f, 0.85f, 1f, 1f);
			py += 18f;
			string[] skills = new string[6] { "MINING", "BUILDING", "COMBAT", "NAVIGATION", "CRAFTING", "SURVIVAL" };
			float[] skillVals = new float[6]
			{
				agent2.Brain.SkillMining,
				agent2.Brain.SkillBuilding,
				agent2.Brain.SkillCombat,
				agent2.Brain.SkillNavigation,
				agent2.Brain.SkillCrafting,
				agent2.Brain.SkillSurvival
			};
			for (int j = 0; j < skills.Length; j++)
			{
				float sBarLen = 160f;
				ui.Text(bx + 14f, py, skills[j], s * 0.78f, 0.82f, 0.88f, 0.95f, 0.95f);
				ui.PixelBar(bx + 140f, py + 1f, sBarLen, 8f, M.Clamp(skillVals[j], 0f, 1f), 0.3f, 0.6f + skillVals[j] * 0.4f, 1f, 0.95f);
				ui.Text(bx + 140f + sBarLen + 8f, py, (skillVals[j] * 100f).ToString("0", CultureInfo.InvariantCulture) + "%", s * 0.78f, 1f, 1f, 1f, 0.95f);
				py += 13f;
			}

			py += 10f;
			ui.Text(bx + 14f, py, "DANGER MEMORY: " + agent2.Brain.Danger.Count + " CELLS | LAST CAUSE: " + agent2.LastDamageCause, s * 0.78f, 1f, 0.65f, 0.65f, 0.95f);
			py += 16f;
			string lastLog = ((agent2.Brain.Log.Count <= 0) ? "IDLE" : agent2.Brain.Log[agent2.Brain.Log.Count - 1]);
			ui.Text(bx + 14f, py, "CORTEX LOG: " + lastLog, s * 0.76f, 0.75f, 0.85f, 0.9f, 0.95f);
		}

		private void DrawHelp(Ui2D ui, float s)
		{
			string[] array = new string[13]
			{
				"CONTROLS",
				"W A S D MOVE      SPACE JUMP/SWIM      SHIFT SPRINT      CTRL FLY DOWN",
				"F TOGGLE FLY      G TOGGLE CREATIVE/SURVIVAL      Q DROP ITEM",
				"LEFT CLICK MINE / PUNCH      RIGHT CLICK PLACE      WHEEL/1-9 HOTBAR",
				"ESC PAUSE      H HELP      TAB AI LEADERBOARD      B AI BRAIN INSPECTOR",
				"R NEXT AGENT      N FORCE AN EVOLUTION STEP      O PAUSE EVOLUTION",
				"M / , MUTATION RATE UP / DOWN      T FREEZE TIME      C CHRONICLE",
				"K RESPAWN AT A SAFE PLACE      P WORLD INFO",
				string.Empty,
				"THE AI: 10 AGENTS SENSE, DECIDE (UTILITY + LEARNED Q VALUES) AND PLAY THE SAME",
				"GAME YOU DO. THEY MINE, CRAFT, BUILD SHELTERS, FIGHT, FLEE LAVA AND EXPLORE.",
				"WHEN ONE DIES THE SPECIES RECORDS THE CAUSE, RAISES ITS SAFETY MARGINS AND",
				"REBORNS A CHILD WITH MUTATED GENES AND AN INHERITED SHARE OF ITS MEMORY."
			};
			float num = 900f;
			float num2 = 120f;
			float num3 = ((float)Camera.Width - num) * 0.5f;
			ui.Rect(num3, num2, num, 24f + (float)array.Length * (10f * s + 2f), 0f, 0f, 0f, 0.8f);
			ui.Outline(num3, num2, num, 24f + (float)array.Length * (10f * s + 2f), 0.6f, 0.8f, 1f, 0.9f, 2f);
			for (int i = 0; i < array.Length; i++)
			{
				ui.Text(num3 + 16f, num2 + 10f + (float)i * (10f * s + 2f), array[i], s, (i != 0) ? 0.85f : 1f, (i != 0) ? 0.9f : 0.9f, 1f, 0.95f);
			}
		}

		private void Toast(string msg)
		{
			toast = msg;
			toastTimer = 4.5f;
		}
	}
}
