namespace VoxelAI.World
{
	public static class Blocks
	{
		public static readonly BlockInfo[] All;

		static Blocks()
		{
			All = new BlockInfo[24];
			Add(B.Air, "Air", 0, 0, 0, false, false, false, 0, 0f, false, false);
			Add(B.Grass, "Grass", 0, 1, 2, true, true, false, 0, 0.7f, false, false);
			Add(B.Dirt, "Dirt", 2, 2, 2, true, true, false, 0, 0.6f, false, false);
			Add(B.Stone, "Stone", 3, 3, 3, true, true, false, 0, 1.6f, false, false);
			Add(B.Cobble, "Cobblestone", 4, 4, 4, true, true, false, 0, 1.8f, false, false);
			Add(B.Sand, "Sand", 5, 5, 5, true, true, false, 0, 0.55f, false, false);
			Add(B.Gravel, "Gravel", 6, 6, 6, true, true, false, 0, 0.6f, false, false);
			Add(B.Log, "Log", 9, 8, 9, true, true, false, 0, 1.1f, false, false);
			Add(B.Leaves, "Leaves", 10, 10, 10, true, false, false, 0, 0.3f, false, true);
			Add(B.Planks, "Planks", 11, 11, 11, true, true, false, 0, 1f, false, false);
			Add(B.Glass, "Glass", 12, 12, 12, true, false, false, 0, 0.4f, false, true);
			Add(B.Brick, "Brick", 13, 13, 13, true, true, false, 0, 1.7f, false, false);
			Add(B.Water, "Water", 14, 14, 14, false, false, true, 0, 0f, false, false);
			Add(B.Lava, "Lava", 15, 15, 15, false, false, true, 15, 0f, false, false);
			Add(B.Snow, "Snow", 16, 29, 2, true, true, false, 0, 0.4f, false, false);
			Add(B.Cactus, "Cactus", 18, 17, 18, true, true, false, 0, 0.6f, true, false);
			Add(B.CoalOre, "Coal Ore", 19, 19, 19, true, true, false, 0, 2.2f, false, false);
			Add(B.IronOre, "Iron Ore", 20, 20, 20, true, true, false, 0, 2.6f, false, false);
			Add(B.GoldOre, "Gold Ore", 21, 21, 21, true, true, false, 0, 2.8f, false, false);
			Add(B.DiamondOre, "Diamond Ore", 22, 22, 22, true, true, false, 0, 3.2f, false, false);
			Add(B.Bedrock, "Bedrock", 7, 7, 7, true, true, false, 0, 999f, false, false);
			Add(B.CraftingTable, "Crafting Table", 23, 24, 11, true, true, false, 0, 1.2f, false, false);
			Add(B.Furnace, "Furnace", 27, 26, 27, true, true, false, 0, 1.9f, false, false);
			Add(B.Glowstone, "Glowstone", 28, 28, 28, true, true, false, 15, 0.8f, false, false);
		}

		public static BlockInfo Info(B b)
		{
			return All[(uint)b];
		}

		public static string Name(B b)
		{
			return All[(uint)b].Name;
		}

		private static BlockInfo Add(B id, string name, int top, int side, int bottom, bool solid, bool opaque, bool liquid, int light, float hardness, bool damaging, bool cutout)
		{
			BlockInfo blockInfo = new BlockInfo();
			blockInfo.Id = id;
			blockInfo.Name = name;
			blockInfo.TileTop = top;
			blockInfo.TileSide = side;
			blockInfo.TileBottom = bottom;
			blockInfo.Solid = solid;
			blockInfo.Opaque = opaque;
			blockInfo.Liquid = liquid;
			blockInfo.Light = (byte)light;
			blockInfo.Hardness = hardness;
			blockInfo.Damaging = damaging;
			blockInfo.Cutout = cutout;
			blockInfo.CustomBlockable = !liquid;
			All[(uint)id] = blockInfo;
			return blockInfo;
		}
	}
}
