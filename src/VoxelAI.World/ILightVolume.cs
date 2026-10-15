namespace VoxelAI.World
{
	public interface ILightVolume
	{
		int MaxY
		{
			get;
		}

		byte LightBlock(int x, int y, int z);

		byte LightSky(int x, int y, int z);

		byte LightBlockValue(int x, int y, int z);

		void LightSetSky(int x, int y, int z, byte v);

		void LightSetBlockValue(int x, int y, int z, byte v);

		bool LightIsOpaque(int x, int y, int z);
	}
}
