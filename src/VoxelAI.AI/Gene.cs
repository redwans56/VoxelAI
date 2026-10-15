namespace VoxelAI.AI
{
	public static class Gene
	{
		public const int Aggression = 0;

		public const int Curiosity = 1;

		public const int Sociability = 2;

		public const int Greed = 3;

		public const int Caution = 4;

		public const int Impatience = 5;

		public const int Grit = 6;

		public const int Architect = 7;

		public const int SpeedBias = 8;

		public const int RiskTolerance = 9;

		public const int Patience = 10;

		public const int Adaptability = 11;

		public const int Count = 12;

		public static readonly string[] Names = new string[12]
		{
			"Aggression",
			"Curiosity",
			"Sociability",
			"Greed",
			"Caution",
			"Impatience",
			"Grit",
			"Architect",
			"SpeedBias",
			"Risk",
			"Patience",
			"Adaptability"
		};

		public static string Name(int i)
		{
			return Names[i];
		}
	}
}
