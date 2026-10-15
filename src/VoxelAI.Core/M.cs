using System;

namespace VoxelAI.Core
{
	public static class M
	{
		public const float Pi = (float)Math.PI;

		public const float Tau = (float)Math.PI * 2f;

		public const float Deg2Rad = (float)Math.PI / 180f;

		public const float Rad2Deg = 180f / (float)Math.PI;

		public static float Clamp(float v, float lo, float hi)
		{
			return (v < lo) ? lo : ((!(v > hi)) ? v : hi);
		}

		public static int Clamp(int v, int lo, int hi)
		{
			return (v < lo) ? lo : ((v <= hi) ? v : hi);
		}

		public static byte ClampByte(int v)
		{
			return (byte)((v >= 0) ? ((v <= 255) ? v : 255) : 0);
		}

		public static float Lerp(float a, float b, float t)
		{
			return a + (b - a) * ((t < 0f) ? 0f : ((!(t > 1f)) ? t : 1f));
		}

		public static float SmoothStep(float t)
		{
			t = Clamp(t, 0f, 1f);
			return t * t * (3f - 2f * t);
		}

		public static float Fract(float v)
		{
			return v - (float)Math.Floor(v);
		}

		public static int FloorInt(float v)
		{
			return (int)Math.Floor(v);
		}

		public static int FloorDiv(int a, int b)
		{
			return (a < 0) ? (-((-a + b - 1) / b)) : (a / b);
		}

		public static int Mod(int a, int b)
		{
			int num = a % b;
			return (num >= 0) ? num : (num + b);
		}

		public static float MoveTowards(float cur, float target, float maxDelta)
		{
			float value = target - cur;
			if (Math.Abs(value) <= maxDelta)
			{
				return target;
			}
			return cur + (float)Math.Sign(value) * maxDelta;
		}

		public static float WrapAngle(float a)
		{
			while (a > (float)Math.PI)
			{
				a -= (float)Math.PI * 2f;
			}
			while (a < -(float)Math.PI)
			{
				a += (float)Math.PI * 2f;
			}
			return a;
		}

		public static float ApproachAngle(float cur, float target, float maxDelta)
		{
			float value = WrapAngle(target - cur);
			if (Math.Abs(value) <= maxDelta)
			{
				return WrapAngle(target);
			}
			return WrapAngle(cur + (float)Math.Sign(value) * maxDelta);
		}

		public static float ExpSmooth(float cur, float target, float rate, float dt)
		{
			float num = 1f - (float)Math.Exp((0f - rate) * dt);
			return cur + (target - cur) * num;
		}
	}
}
