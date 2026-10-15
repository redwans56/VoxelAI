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
				if (autoCaptureFrame == 10)
				{
					Renderer.SaveScreenshot(Camera.Width, Camera.Height, Path.Combine(dir, "01_main_menu.png"));
					StartWorld("world1", 20260517);
					autoCaptureFrame = 0;
				}
			}
			else if (Current == Mode.Playing)
			{
				if (autoCaptureFrame == 45)
				{
					Player.Pitch = -0.25f;
					Player.Yaw = 0.8f;
					Camera.Pitch = Player.Pitch;
					Camera.Yaw = Player.Yaw;
					Renderer.SaveScreenshot(Camera.Width, Camera.Height, Path.Combine(dir, "02_voxel_world.png"));
					showBrain = true;
					if (AI != null && AI.Agents.Count > 0)
					{
						inspected = AI.Agents[0];
						inspected.IsSelected = true;
					}
				}
				else if (autoCaptureFrame == 60)
				{
					Renderer.SaveScreenshot(Camera.Width, Camera.Height, Path.Combine(dir, "03_ai_brain_hud.png"));
					showBrain = false;
					showLeaderboard = true;
				}
				else if (autoCaptureFrame == 75)
				{
					Renderer.SaveScreenshot(Camera.Width, Camera.Height, Path.Combine(dir, "04_ai_leaderboard.png"));
				}
				else if (autoCaptureFrame >= 90)
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
			ui.Rect(0f, 0f, Camera.Width, Camera.Height, 0.04f, 0.05f, 0.08f, 1f);
			ui.TextCentered(cx, (float)Camera.Height * 0.18f, "VOXEL AI", 6f, 0.55f, 0.85f, 1f, 1f);
			ui.TextCentered(cx, (float)Camera.Height * 0.18f + 60f, "A .NET FRAMEWORK VOXEL WORLD WITH SELF IMPROVING AI PLAYERS", 1.6f, 0.7f, 0.78f, 0.9f, 1f);
			for (int i = 0; i < menuItems.Length; i++)
			{
				bool flag = i == menuIndex;
				ui.TextCentered(cx, (float)Camera.Height * 0.42f + (float)i * 34f, ((!flag) ? "  " : "> ") + menuItems[i], 2.4f, (!flag) ? 0.75f : 1f, (!flag) ? 0.78f : 0.95f, (!flag) ? 0.8f : 0.5f, 1f);
			}
			string[] array = WorldSave.ListWorlds();
			ui.TextCentered(cx, (float)Camera.Height * 0.72f, "SAVED WORLDS: " + ((array.Length != 0) ? string.Join(", ", array) : "NONE YET"), 1.5f, 0.6f, 0.7f, 0.8f, 1f);
			ui.TextCentered(cx, (float)Camera.Height - 70f, "ENTER = SELECT      ARROWS = NAVIGATE", 1.5f, 0.55f, 0.62f, 0.7f, 1f);
			ui.End();
		}

		private void DrawLoading()
		{
			Ui2D ui = Renderer.Ui;
			ui.Begin(Camera.Width, Camera.Height);
			ui.Rect(0f, 0f, Camera.Width, Camera.Height, 0.03f, 0.04f, 0.06f, 1f);
			float num = (float)Camera.Width * 0.5f;
			float num2 = (float)Camera.Height * 0.5f;
			ui.TextCentered(num, num2 - 60f, "VOXEL AI", 5f, 0.5f, 0.8f, 1f, 1f);
			ui.TextCentered(num, num2, LoadingText, 2f, 0.85f, 0.9f, 1f, 1f);
			float num3 = ((LoadingTotal <= 0) ? 0f : M.Clamp((float)LoadingDone / (float)Math.Max(1, LoadingTotal), 0f, 1f));
			ui.Rect(num - 220f, num2 + 30f, 440f, 18f, 0.12f, 0.14f, 0.18f, 1f);
			ui.Rect(num - 218f, num2 + 32f, 436f * num3, 14f, 0.35f, 0.75f, 1f, 1f);
			ui.TextCentered(num, num2 + 60f, "CHUNKS LIT " + LoadingDone + " / " + LoadingTotal, 1.5f, 0.7f, 0.8f, 0.9f, 1f);
			ui.TextCentered(num, (float)Camera.Height - 80f, Renderer.GLInfo, 1.2f, 0.5f, 0.55f, 0.65f, 1f);
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
			Renderer.DrawClouds(World, Camera, 0.016f);
			Renderer.DrawHeldItem(Player.SelectedBlock, Player.Swing, true, Camera);
			Renderer.DrawHighlight(Player.TargetBlock, Player.HasTarget && !showBrain);
			Renderer.DrawChunks(World, Camera, chunks, true);
			Renderer.DrawParticles(World, Camera);
			Renderer.DrawWeather(World, Camera, 0.016f);
			DrawHud();
		}

		private void DrawHud()
		{
			Ui2D ui = Renderer.Ui;
			ui.Begin(Camera.Width, Camera.Height);
			float num = ((Camera.Width < 1600) ? 1.4f : 1.7f);
			float num2 = (float)Camera.Width * 0.5f;
			float num3 = (float)Camera.Height * 0.5f;
			ui.Rect(num2 - 9f, num3 - 1f, 18f, 2f, 1f, 1f, 1f, 0.8f);
			ui.Rect(num2 - 1f, num3 - 9f, 2f, 18f, 1f, 1f, 1f, 0.8f);
			int num4 = M.FloorInt(Player.Position.X);
			int num5 = M.FloorInt(Player.Position.Y);
			int num6 = M.FloorInt(Player.Position.Z);
			string[] array = new string[5]
			{
				string.Format(CultureInfo.InvariantCulture, "VOXEL AI  {0:0} FPS   {1:0.0} MS", Fps, FrameMs),
				string.Format(CultureInfo.InvariantCulture, "XYZ {0} {1} {2}   CHUNK {3},{4}", num4, num5, num6, M.FloorDiv(num4, 16), M.FloorDiv(num6, 16)),
				string.Format(CultureInfo.InvariantCulture, "{0}   {1}   LIGHT {2}   DAYLIGHT {3:0.00}", World.CurrentWeather, World.BiomeAt(num4, num6), World.LightAt(num4, num5, num6), World.Daylight),
				string.Format(CultureInfo.InvariantCulture, "TIME {0:0.00}   CHUNKS {1}  DRAWN {2}  VERTS {3}", World.TimeOfDay, World.LoadedChunkCount, Renderer.DrawnChunks, Renderer.DrawnVertices),
				string.Format(CultureInfo.InvariantCulture, "MODE {0}   {1}", Player.Flying ? "FLY" : ((!Player.Creative) ? "SURVIVAL" : "CREATIVE"), Renderer.GLInfo)
			};
			for (int i = 0; i < array.Length; i++)
			{
				ui.TextShadow(12f, 10f + (float)i * (8f * num + 2f), array[i], num, 0.92f, 0.95f, 1f, 0.95f);
			}
			if (AI != null)
			{
				string[] array2 = new string[4]
				{
					AI.StatusLine(),
					AI.Species.Summary(),
					AI.VisitedCells + " CELLS EXPLORED BY THE COLONY",
					"TAB LEADERBOARD   B BRAIN   N EVOLVE   O EVOLUTION ON/OFF"
				};
				for (int j = 0; j < array2.Length; j++)
				{
					float num7 = ui.TextWidth(array2[j], num);
					ui.TextShadow((float)Camera.Width - num7 - 12f, 10f + (float)j * (8f * num + 2f), array2[j], num, (j != 0) ? 0.75f : 0.6f, 1f, (j != 0) ? 0.85f : 0.7f, 0.95f);
				}
			}
			float num8 = Camera.Height - 62;
			ui.Rect(12f, num8, 220f, 20f, 0.1f, 0.1f, 0.12f, 0.8f);
			float num9 = M.Clamp(Player.Health / 100f, 0f, 1f);
			ui.Rect(14f, num8 + 2f, 216f * num9, 16f, 0.8f * (1f - num9) + 0.2f, 0.25f + 0.6f * num9, 0.25f, 0.95f);
			ui.TextShadow(16f, num8 + 4f, "HEALTH " + (int)Player.Health, num * 0.9f, 1f, 1f, 1f, 0.95f);
			int num10 = 9;
			float num11 = 46f;
			float num12 = 6f;
			float num13 = (float)num10 * num11 + (float)(num10 - 1) * num12;
			float num14 = ((float)Camera.Width - num13) * 0.5f;
			float num15 = Camera.Height - 70;
			for (int k = 0; k < num10; k++)
			{
				float num16 = num14 + (float)k * (num11 + num12);
				bool flag = k == Player.HotbarIndex;
				ui.Rect(num16, num15, num11, num11, 0.06f, 0.07f, 0.09f, 0.75f);
				if (flag)
				{
					ui.Outline(num16 - 1f, num15 - 1f, num11 + 2f, num11 + 2f, 1f, 0.95f, 0.4f, 1f, 2f);
				}
				byte b = Player.Hotbar[k];
				if (b != 0)
				{
					float num17 = 34f;
					GL.BindTexture(3553u, Renderer.Atlas.TextureId);
					ui.TileRect(num16 + (num11 - num17) * 0.5f, num15 + (num11 - num17) * 0.5f, num17, num17, Blocks.Info((B)b).TileSide, 1f, 1f, 1f, 1f);
				}
				ui.TextShadow(num16 + 3f, num15 + 2f, (k + 1).ToString(CultureInfo.InvariantCulture), num * 0.8f, 0.9f, 0.9f, 1f, 0.9f);
			}
			byte selectedBlock = Player.SelectedBlock;
			if (selectedBlock != 0)
			{
				string s = Blocks.Name((B)selectedBlock).ToUpperInvariant() + ((!Player.Creative) ? ("  x" + Player.CountOf(selectedBlock)) : "  (INFINITE)");
				ui.TextCentered((float)Camera.Width * 0.5f, num15 - 24f, s, num, 1f, 1f, 1f, 0.95f);
			}
			if (Player.MineProgress > 0f && Player.HasTarget)
			{
				float num18 = M.Clamp(Player.MineProgress / Math.Max(0.001f, Blocks.Info((B)World.GetBlock(Player.TargetBlock.X, Player.TargetBlock.Y, Player.TargetBlock.Z)).Hardness), 0f, 1f);
				ui.Rect(num2 - 40f, num3 + 26f, 80f, 8f, 0.1f, 0.1f, 0.1f, 0.75f);
				ui.Rect(num2 - 39f, num3 + 27f, 78f * num18, 6f, 0.95f, 0.85f, 0.4f, 1f);
			}
			DrawAgentLabels(ui, num);
			if (showChronicle && AI != null && AI.Chronicle.Count > 0)
			{
				int num19 = Math.Min(5, AI.Chronicle.Count);
				for (int l = 0; l < num19; l++)
				{
					string text = AI.Chronicle[AI.Chronicle.Count - num19 + l];
					if (text.Length > 92)
					{
						text = text.Substring(0, 92) + "...";
					}
					ui.TextShadow(12f, (float)(Camera.Height - 120) - (float)(num19 - l) * (8f * num), text, num * 0.85f, 0.85f, 0.95f, 1f, 0.9f);
				}
				ui.TextShadow(12f, Camera.Height - 120, "AI CHRONICLE", num * 0.85f, 1f, 0.8f, 0.4f, 0.95f);
			}
			if (toastTimer > 0f)
			{
				ui.TextCentered((float)Camera.Width * 0.5f, 90f, toast, num * 1.1f, 1f, 0.95f, 0.6f, M.Clamp(toastTimer, 0f, 1f));
			}
			if (showLeaderboard)
			{
				DrawLeaderboard(ui, num);
			}
			if (showBrain)
			{
				DrawBrainPanel(ui, num);
			}
			if (showHelp)
			{
				DrawHelp(ui, num);
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
				float sx;
				float sy;
				float depth;
				if (Camera.WorldToScreen(world, out sx, out sy, out depth) && !(sx < -100f) && !(sy < -40f) && !(sx > (float)(Camera.Width + 100)) && !(sy > (float)(Camera.Height + 40)))
				{
					float num = Vec3.Distance(agent.Body.Position, Camera.Pos);
					if (!(num > 64f))
					{
						string s2 = agent.Name + " g" + agent.Generation;
						string s3 = string.Concat(agent.Action, ": ", agent.Status);
						float num2 = M.Clamp(1.6f - num * 0.012f, 0.8f, 1.5f);
						float num3 = Math.Max(ui.TextWidth(s2, num2), ui.TextWidth(s3, num2 * 0.85f));
						float num4 = sx - num3 * 0.5f;
						float num5 = sy;
						ui.Rect(num4 - 4f, num5 - 2f, num3 + 8f, 22f * num2 + 4f, 0f, 0f, 0f, (!agent.IsSelected) ? 0.32f : 0.6f);
						ui.Text(num4, num5, s2, num2, agent.Body.HeadR, agent.Body.HeadG, agent.Body.HeadB, 1f);
						ui.Text(num4, num5 + 9f * num2, s3, num2 * 0.85f, 0.85f, 0.9f, 1f, 0.95f);
						float num6 = num3 + 8f;
						ui.Rect(num4 - 4f, num5 + 20f * num2 + 4f, num6, 4f, 0.2f, 0.05f, 0.05f, 0.8f);
						ui.Rect(num4 - 4f, num5 + 20f * num2 + 4f, num6 * agent.HealthFrac, 4f, 0.3f, 0.9f, 0.3f, 0.9f);
						float num7 = M.Clamp(agent.Fitness / Math.Max(50f, AI.BestFitnessEver), 0f, 1f);
						ui.Rect(num4 - 4f, num5 + 26f * num2 + 4f, num6, 4f, 0.1f, 0.15f, 0.25f, 0.8f);
						ui.Rect(num4 - 4f, num5 + 26f * num2 + 4f, num6 * num7, 4f, 0.5f, 0.7f, 1f, 0.9f);
					}
				}
			}
		}

		private void DrawLeaderboard(Ui2D ui, float s)
		{
			if (AI != null)
			{
				float num = Math.Min(900, Camera.Width - 80);
				float num2 = 90f + (float)AI.Agents.Count * (10f * s + 4f);
				float num3 = ((float)Camera.Width - num) * 0.5f;
				float num4 = 120f;
				ui.Rect(num3, num4, num, num2, 0f, 0f, 0f, 0.72f);
				ui.Outline(num3, num4, num, num2, 0.4f, 0.7f, 1f, 0.8f, 2f);
				ui.Text(num3 + 14f, num4 + 8f, "AI LEADERBOARD  (TAB TO CLOSE)", s * 1.2f, 0.7f, 0.9f, 1f, 1f);
				string s2 = string.Format("{0,-10} {1,-4} {2,8} {3,7} {4,7} {5,13} {6,6} {7,6} {8,5} {9,5}", "NAME", "GEN", "FITNESS", "MINED", "PLACED", "ACTION", "KILLS", "DEATHS", "ORE", "SKILL");
				ui.Text(num3 + 14f, num4 + 30f, s2, s * 0.9f, 0.85f, 0.9f, 1f, 0.95f);
				List<Agent> list = AI.Leaderboard();
				for (int i = 0; i < list.Count; i++)
				{
					Agent agent = list[i];
					string text = ((!agent.Dead) ? agent.Action.ToString() : "respawning");
					string s3 = string.Format(CultureInfo.InvariantCulture, "{0,-10} {1,-4} {2,8:0} {3,7} {4,7} {5,13} {6,6} {7,6} {8,5} {9,5:0.00}", agent.Name, agent.Generation, agent.Fitness, agent.MinedTotal, agent.PlacementsTotal, text, agent.Kills, agent.Deaths, agent.OresFound, agent.Brain.SkillMining);
					float headR = agent.Body.HeadR;
					float headG = agent.Body.HeadG;
					float headB = agent.Body.HeadB;
					ui.Text(num3 + 14f, num4 + 46f + (float)i * (10f * s + 4f), s3, s * 0.9f, headR, headG, headB, (!agent.Dead) ? 1f : 0.5f);
				}
				ui.Text(num3 + 14f, num4 + num2 - 22f, "FITNESS = MINING + BUILDING + CRAFTING + KILLS + SURVIVAL - DAMAGE", s * 0.8f, 0.7f, 0.78f, 0.9f, 0.95f);
			}
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
			float w = 470f;
			float num = 14f;
			float num2 = 120f;
			if (inspected == null)
			{
				ui.Rect(num, num2, w, 60f, 0f, 0f, 0f, 0.7f);
				ui.Text(num + 12f, num2 + 10f, "NO AGENT TO INSPECT (PRESS R)", s, 1f, 0.8f, 0.5f, 1f);
				return;
			}
			Agent agent = inspected;
			float h = 560f;
			ui.Rect(num, num2, w, h, 0f, 0f, 0f, 0.74f);
			ui.Outline(num, num2, w, h, 0.4f, 0.9f, 0.5f, 0.9f, 2f);
			Agent agent2 = agent;
			float num3 = num2 + 8f;
			ui.Text(num + 12f, num3, "BRAIN INSPECTOR  " + agent2.Name + "  GEN " + agent2.Generation + "  (B/R)", s * 1.1f, 0.6f, 1f, 0.7f, 1f);
			num3 += 20f;
			ui.Text(num + 12f, num3, agent2.G.Describe(), s, 0.9f, 1f, 0.9f, 1f);
			num3 += 18f;
			ui.Text(num + 12f, num3, string.Format(CultureInfo.InvariantCulture, "FIT {0:0}   HP {1:0}   AGE {2:0}S   DEATHS {3}   CARRY {4}/{5}", agent2.Fitness, agent2.Health, agent2.Age, agent2.Deaths, agent2.CarryCount, agent2.CarryLimit), s * 0.9f, 0.85f, 0.9f, 1f, 1f);
			num3 += 18f;
			ui.Text(num + 12f, num3, string.Concat("DOING: ", agent2.Action, " - ", agent2.Status), s * 0.9f, 1f, 0.9f, 0.6f, 1f);
			num3 += 18f;
			ui.Text(num + 12f, num3, "TOTALS: MINED " + agent2.MinedTotal + "  PLACED " + agent2.PlacementsTotal + "  CRAFTED " + agent2.CraftsTotal + "  HUTS " + agent2.StructuresBuilt + "  ORE " + agent2.OresFound, s * 0.85f, 0.8f, 0.9f, 1f, 1f);
			num3 += 22f;
			ui.Text(num + 12f, num3, "GENES (EVOLVED)", s, 0.9f, 0.8f, 1f, 1f);
			num3 += 18f;
			for (int i = 0; i < 12; i++)
			{
				float num4 = agent2.G.V[i];
				float num5 = 150f;
				ui.Text(num + 12f, num3, Gene.Name(i), s * 0.85f, 0.85f, 0.9f, 1f, 0.95f);
				ui.Rect(num + 130f, num3 + 2f, num5, 7f, 0.15f, 0.15f, 0.2f, 1f);
				ui.Rect(num + 130f, num3 + 2f, num5 * num4, 7f, 0.4f + num4 * 0.5f, 0.8f - num4 * 0.3f, 0.4f, 1f);
				ui.Text(num + 130f + num5 + 8f, num3, (num4 * 100f).ToString("0", CultureInfo.InvariantCulture), s * 0.85f, 1f, 1f, 1f, 0.95f);
				num3 += 12f;
			}
			num3 += 8f;
			ui.Text(num + 12f, num3, "LEARNED SKILLS (REINFORCEMENT)", s, 0.9f, 0.8f, 1f, 1f);
			num3 += 18f;
			string[] array = new string[6]
			{
				"MINING",
				"BUILDING",
				"COMBAT",
				"NAVIGATION",
				"CRAFTING",
				"SURVIVAL"
			};
			float[] array2 = new float[6]
			{
				agent2.Brain.SkillMining,
				agent2.Brain.SkillBuilding,
				agent2.Brain.SkillCombat,
				agent2.Brain.SkillNavigation,
				agent2.Brain.SkillCrafting,
				agent2.Brain.SkillSurvival
			};
			for (int j = 0; j < array.Length; j++)
			{
				float num6 = 150f;
				ui.Text(num + 12f, num3, array[j], s * 0.85f, 0.85f, 0.9f, 1f, 0.95f);
				ui.Rect(num + 130f, num3 + 2f, num6, 7f, 0.15f, 0.15f, 0.2f, 1f);
				ui.Rect(num + 130f, num3 + 2f, num6 * M.Clamp(array2[j], 0f, 1f), 7f, 0.3f, 0.6f + array2[j] * 0.4f, 1f, 1f);
				num3 += 12f;
			}
			num3 += 10f;
			ui.Text(num + 12f, num3, "LEARNED Q VALUES (ACTION VALUE)", s, 0.9f, 0.8f, 1f, 1f);
			num3 += 18f;
			for (int k = 0; k < 14; k++)
			{
				Act act = (Act)k;
				if (act != Act.Count)
				{
					float num7 = agent2.Brain.Q[k];
					float num8 = 150f;
					float num9 = M.Clamp((num7 + 1f) / 2f, 0f, 1f);
					ui.Text(num + 12f, num3, act.ToString(), s * 0.85f, 0.85f, 0.9f, 1f, 0.95f);
					ui.Rect(num + 130f, num3 + 2f, num8, 7f, 0.15f, 0.15f, 0.2f, 1f);
					ui.Rect(num + 130f, num3 + 2f, num8 * num9, 7f, 1f, 0.6f + num9 * 0.3f, 0.2f, 1f);
					ui.Text(num + 130f + num8 + 8f, num3, num7.ToString("0.00", CultureInfo.InvariantCulture), s * 0.85f, 1f, 1f, 1f, 0.95f);
					num3 += 11f;
				}
			}
			num3 += 6f;
			ui.Text(num + 12f, num3, "DANGER MEMORY " + agent2.Brain.Danger.Count + " CELLS   LAST CAUSE: " + agent2.LastDamageCause, s * 0.85f, 1f, 0.7f, 0.7f, 0.95f);
			num3 += 16f;
			ui.Text(num + 12f, num3, "SPECIES: " + AI.Species.DeathSummary(), s * 0.8f, 0.9f, 0.8f, 0.8f, 0.95f);
			num3 += 16f;
			string str = ((agent2.Brain.Log.Count <= 0) ? "-" : agent2.Brain.Log[agent2.Brain.Log.Count - 1]);
			ui.Text(num + 12f, num3, "LAST: " + str, s * 0.8f, 0.8f, 0.85f, 0.9f, 0.95f);
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
