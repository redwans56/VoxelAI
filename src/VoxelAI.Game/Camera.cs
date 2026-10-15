using System;
using VoxelAI.Core;

namespace VoxelAI.Game
{
	public class Camera
	{
		public Vec3 Pos = new Vec3(0f, 70f, 0f);

		public float Yaw;

		public float Pitch;

		public float FovDeg = 74f;

		public float Near = 0.08f;

		public float Far = 320f;

		public float Roll;

		public Mat4 View = Mat4.Identity();

		public Mat4 Proj = Mat4.Identity();

		public Mat4 ViewProj = Mat4.Identity();

		public int Width = 1280;

		public int Height = 720;

		private readonly float[] planes = new float[24];

		public Vec3 Forward
		{
			get
			{
				float num = (float)Math.Cos(Pitch);
				return new Vec3((float)Math.Sin(Yaw) * num, (float)Math.Sin(Pitch), (float)Math.Cos(Yaw) * num).Normalized;
			}
		}

		public void Update(int width, int height)
		{
			Width = Math.Max(1, width);
			Height = Math.Max(1, height);
			float aspect = (float)Width / (float)Height;
			Near = 0.08f;
			Far = 460f;
			Vec3 pos = Pos;
			Vec3 forward = Forward;
			View = Mat4.LookAt(pos, pos + forward, new Vec3(0f, 1f, 0f));
			Proj = Mat4.Perspective(FovDeg * ((float)Math.PI / 180f), aspect, Near, Far);
			ViewProj = Proj * View;
			ExtractPlanes();
		}

		private void ExtractPlanes()
		{
			float[] a = ViewProj.A;
			float num = a[0];
			float num2 = a[4];
			float num3 = a[8];
			float num4 = a[12];
			float num5 = a[1];
			float num6 = a[5];
			float num7 = a[9];
			float num8 = a[13];
			float num9 = a[2];
			float num10 = a[6];
			float num11 = a[10];
			float num12 = a[14];
			float num13 = a[3];
			float num14 = a[7];
			float num15 = a[11];
			float num16 = a[15];
			SetPlane(0, num13 + num, num14 + num2, num15 + num3, num16 + num4);
			SetPlane(1, num13 - num, num14 - num2, num15 - num3, num16 - num4);
			SetPlane(2, num13 + num5, num14 + num6, num15 + num7, num16 + num8);
			SetPlane(3, num13 - num5, num14 - num6, num15 - num7, num16 - num8);
			SetPlane(4, num13 + num9, num14 + num10, num15 + num11, num16 + num12);
			SetPlane(5, num13 - num9, num14 - num10, num15 - num11, num16 - num12);
		}

		private void SetPlane(int i, float a, float b, float c, float d)
		{
			float num = (float)Math.Sqrt(a * a + b * b + c * c);
			if (num < 1E-08f)
			{
				num = 1f;
			}
			planes[i * 4] = a / num;
			planes[i * 4 + 1] = b / num;
			planes[i * 4 + 2] = c / num;
			planes[i * 4 + 3] = d / num;
		}

		public bool BoxVisible(Vec3 min, Vec3 max)
		{
			for (int i = 0; i < 6; i++)
			{
				float num = planes[i * 4];
				float num2 = planes[i * 4 + 1];
				float num3 = planes[i * 4 + 2];
				float num4 = planes[i * 4 + 3];
				float num5 = ((!(num >= 0f)) ? min.X : max.X);
				float num6 = ((!(num2 >= 0f)) ? min.Y : max.Y);
				float num7 = ((!(num3 >= 0f)) ? min.Z : max.Z);
				if (num * num5 + num2 * num6 + num3 * num7 + num4 < 0f)
				{
					return false;
				}
			}
			return true;
		}

		public bool PointVisible(Vec3 p, float radius)
		{
			return BoxVisible(p - new Vec3(radius, radius, radius), p + new Vec3(radius, radius, radius));
		}

		public bool WorldToScreen(Vec3 world, out float sx, out float sy, out float depth)
		{
			Vec3 vec = ViewProj.TransformPoint(world);
			depth = vec.Z;
			sx = (vec.X * 0.5f + 0.5f) * (float)Width;
			sy = (1f - (vec.Y * 0.5f + 0.5f)) * (float)Height;
			return View.TransformPoint(world).Z < -0.05f;
		}
	}
}
