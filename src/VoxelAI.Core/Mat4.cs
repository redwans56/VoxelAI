using System;

namespace VoxelAI.Core
{
	public struct Mat4
	{
		public float[] A;

		public static Mat4 Identity()
		{
			Mat4 result = default(Mat4);
			result.A = new float[16];
			result.A[0] = (result.A[5] = (result.A[10] = (result.A[15] = 1f)));
			return result;
		}

		public static Mat4 Perspective(float fovYRad, float aspect, float near, float far)
		{
			Mat4 result = default(Mat4);
			result.A = new float[16];
			float num = 1f / (float)Math.Tan(fovYRad * 0.5f);
			result.A[0] = num / aspect;
			result.A[5] = num;
			result.A[10] = (far + near) / (near - far);
			result.A[11] = -1f;
			result.A[14] = 2f * far * near / (near - far);
			return result;
		}

		public static Mat4 Ortho(float l, float r, float b, float t, float n, float f)
		{
			Mat4 result = Identity();
			result.A[0] = 2f / (r - l);
			result.A[5] = 2f / (t - b);
			result.A[10] = -2f / (f - n);
			result.A[12] = (0f - (r + l)) / (r - l);
			result.A[13] = (0f - (t + b)) / (t - b);
			result.A[14] = (0f - (f + n)) / (f - n);
			return result;
		}

		public static Mat4 LookAt(Vec3 eye, Vec3 center, Vec3 up)
		{
			Vec3 normalized = (center - eye).Normalized;
			Vec3 normalized2 = Vec3.Cross(normalized, up).Normalized;
			Vec3 a = Vec3.Cross(normalized2, normalized);
			Mat4 result = Identity();
			result.A[0] = normalized2.X;
			result.A[4] = normalized2.Y;
			result.A[8] = normalized2.Z;
			result.A[1] = a.X;
			result.A[5] = a.Y;
			result.A[9] = a.Z;
			result.A[2] = 0f - normalized.X;
			result.A[6] = 0f - normalized.Y;
			result.A[10] = 0f - normalized.Z;
			result.A[12] = 0f - Vec3.Dot(normalized2, eye);
			result.A[13] = 0f - Vec3.Dot(a, eye);
			result.A[14] = Vec3.Dot(normalized, eye);
			return result;
		}

		public static Mat4 Translation(Vec3 t)
		{
			Mat4 result = Identity();
			result.A[12] = t.X;
			result.A[13] = t.Y;
			result.A[14] = t.Z;
			return result;
		}

		public static Mat4 Scale(Vec3 s)
		{
			Mat4 result = Identity();
			result.A[0] = s.X;
			result.A[5] = s.Y;
			result.A[10] = s.Z;
			return result;
		}

		public static Mat4 RotationY(float a)
		{
			Mat4 result = Identity();
			float num = (float)Math.Cos(a);
			float num2 = (float)Math.Sin(a);
			result.A[0] = num;
			result.A[2] = 0f - num2;
			result.A[8] = num2;
			result.A[10] = num;
			return result;
		}

		public static Mat4 RotationX(float a)
		{
			Mat4 result = Identity();
			float num = (float)Math.Cos(a);
			float num2 = (float)Math.Sin(a);
			result.A[5] = num;
			result.A[6] = num2;
			result.A[9] = 0f - num2;
			result.A[10] = num;
			return result;
		}

		public static Mat4 RotationZ(float a)
		{
			Mat4 result = Identity();
			float num = (float)Math.Cos(a);
			float num2 = (float)Math.Sin(a);
			result.A[0] = num;
			result.A[1] = num2;
			result.A[4] = 0f - num2;
			result.A[5] = num;
			return result;
		}

		public static Mat4 operator *(Mat4 a, Mat4 b)
		{
			Mat4 result = default(Mat4);
			result.A = new float[16];
			for (int i = 0; i < 4; i++)
			{
				for (int j = 0; j < 4; j++)
				{
					float num = 0f;
					for (int k = 0; k < 4; k++)
					{
						num += a.A[k * 4 + j] * b.A[i * 4 + k];
					}
					result.A[i * 4 + j] = num;
				}
			}
			return result;
		}

		public Vec3 TransformPoint(Vec3 p)
		{
			float num = A[0] * p.X + A[4] * p.Y + A[8] * p.Z + A[12];
			float num2 = A[1] * p.X + A[5] * p.Y + A[9] * p.Z + A[13];
			float num3 = A[2] * p.X + A[6] * p.Y + A[10] * p.Z + A[14];
			float num4 = A[3] * p.X + A[7] * p.Y + A[11] * p.Z + A[15];
			if (Math.Abs(num4) > 1E-06f && Math.Abs(num4 - 1f) > 1E-06f)
			{
				num /= num4;
				num2 /= num4;
				num3 /= num4;
			}
			return new Vec3(num, num2, num3);
		}

		public Vec3 TransformDir(Vec3 d)
		{
			return new Vec3(A[0] * d.X + A[4] * d.Y + A[8] * d.Z, A[1] * d.X + A[5] * d.Y + A[9] * d.Z, A[2] * d.X + A[6] * d.Y + A[10] * d.Z);
		}
	}
}
