using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace VoxelAI.World
{
	public static class WorldSave
	{
		private const string Magic = "VXC1";

		public static string Root
		{
			get
			{
				return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VoxelAI", "worlds");
			}
		}

		public static string WorldDir(string name)
		{
			return Path.Combine(Root, name);
		}

		public static string[] ListWorlds()
		{
			try
			{
				if (!Directory.Exists(Root))
				{
					return new string[0];
				}
				string[] directories = Directory.GetDirectories(Root);
				string[] array = new string[directories.Length];
				for (int i = 0; i < directories.Length; i++)
				{
					array[i] = Path.GetFileName(directories[i]);
				}
				return array;
			}
			catch
			{
				return new string[0];
			}
		}

		private static string ChunkPath(string dir, int cx, int cz)
		{
			return Path.Combine(dir, "chunks", cx.ToString(CultureInfo.InvariantCulture) + "_" + cz.ToString(CultureInfo.InvariantCulture) + ".vxc");
		}

		public static void SaveChunk(string dir, Chunk c)
		{
			try
			{
				string path = Path.Combine(dir, "chunks");
				if (!Directory.Exists(path))
				{
					Directory.CreateDirectory(path);
				}
				string path2 = ChunkPath(dir, c.Cx, c.Cz);
				using (FileStream fileStream = new FileStream(path2, FileMode.Create, FileAccess.Write, FileShare.None))
				{
					byte[] bytes = Encoding.ASCII.GetBytes("VXC1");
					fileStream.Write(bytes, 0, 4);
					WriteInt(fileStream, c.Cx);
					WriteInt(fileStream, c.Cz);
					using (GZipStream gZipStream = new GZipStream(fileStream, CompressionMode.Compress, true))
					{
						gZipStream.Write(c.Blocks, 0, c.Blocks.Length);
					}
				}
			}
			catch
			{
			}
		}

		public static bool TryLoadChunk(string dir, int cx, int cz, out byte[] blocks)
		{
			blocks = null;
			try
			{
				string path = ChunkPath(dir, cx, cz);
				if (!File.Exists(path))
				{
					return false;
				}
				using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
				{
					byte[] array = new byte[4];
					if (fileStream.Read(array, 0, 4) != 4)
					{
						return false;
					}
					if (Encoding.ASCII.GetString(array) != "VXC1")
					{
						return false;
					}
					ReadInt(fileStream);
					ReadInt(fileStream);
					using (GZipStream gZipStream = new GZipStream(fileStream, CompressionMode.Decompress, true))
					{
						byte[] array2 = new byte[32768];
						int i;
						int num;
						for (i = 0; i < array2.Length; i += num)
						{
							num = gZipStream.Read(array2, i, array2.Length - i);
							if (num <= 0)
							{
								break;
							}
						}
						if (i != array2.Length)
						{
							return false;
						}
						blocks = array2;
						return true;
					}
				}
			}
			catch
			{
				blocks = null;
				return false;
			}
		}

		private static void WriteInt(Stream s, int v)
		{
			byte[] bytes = BitConverter.GetBytes(v);
			s.Write(bytes, 0, 4);
		}

		private static int ReadInt(Stream s)
		{
			byte[] array = new byte[4];
			int num;
			for (int i = 0; i < 4; i += num)
			{
				num = s.Read(array, i, 4 - i);
				if (num <= 0)
				{
					break;
				}
			}
			return BitConverter.ToInt32(array, 0);
		}

		public static void WriteMeta(string dir, Dictionary<string, string> values)
		{
			try
			{
				if (!Directory.Exists(dir))
				{
					Directory.CreateDirectory(dir);
				}
				using (StreamWriter streamWriter = new StreamWriter(Path.Combine(dir, "world.meta")))
				{
					foreach (KeyValuePair<string, string> value in values)
					{
						streamWriter.WriteLine(value.Key + "=" + value.Value.Replace("\n", "\\n"));
					}
				}
			}
			catch
			{
			}
		}

		public static Dictionary<string, string> ReadMeta(string dir)
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			try
			{
				string path = Path.Combine(dir, "world.meta");
				if (!File.Exists(path))
				{
					return dictionary;
				}
				string[] array = File.ReadAllLines(path);
				foreach (string text in array)
				{
					int num = text.IndexOf('=');
					if (num > 0)
					{
						dictionary[text.Substring(0, num)] = text.Substring(num + 1).Replace("\\n", "\n");
					}
				}
				return dictionary;
			}
			catch
			{
				return dictionary;
			}
		}

		public static int MetaInt(Dictionary<string, string> d, string key, int def)
		{
			string value;
			int result;
			if (d.TryGetValue(key, out value) && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
			{
				return result;
			}
			return def;
		}

		public static float MetaFloat(Dictionary<string, string> d, string key, float def)
		{
			string value;
			float result;
			if (d.TryGetValue(key, out value) && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
			{
				return result;
			}
			return def;
		}

		public static string MetaString(Dictionary<string, string> d, string key, string def)
		{
			string value;
			if (d.TryGetValue(key, out value))
			{
				return value;
			}
			return def;
		}
	}
}
