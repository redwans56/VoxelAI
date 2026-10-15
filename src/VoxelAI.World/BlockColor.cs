namespace VoxelAI.World
{
	public static class BlockColor
	{
		private static readonly int[] Rgb = new int[24]
		{
			0,
			6269508,
			8806467,
			8553090,
			7237230,
			14405539,
			8420472,
			6966832,
			3700268,
			10650190,
			13165808,
			9849916,
			3042500,
			14049304,
			16054524,
			3832378,
			2763310,
			13016182,
			15516740,
			6480608,
			3947584,
			10254922,
			8026746,
			13148240
		};

		public static void Get(byte b, out float r, out float g, out float bl)
		{
			int num = ((b < 0 || b >= Rgb.Length) ? 8421504 : Rgb[b]);
			r = (float)((num >> 16) & 0xFF) / 255f;
			g = (float)((num >> 8) & 0xFF) / 255f;
			bl = (float)(num & 0xFF) / 255f;
		}
	}
}
