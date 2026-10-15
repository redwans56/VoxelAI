using System.Collections.Generic;
using System.Windows.Forms;

namespace VoxelAI.Game
{
	public class InputState
	{
		private readonly HashSet<Keys> down = new HashSet<Keys>();

		private readonly HashSet<Keys> pressedThisFrame = new HashSet<Keys>();

		private readonly object padlock = new object();

		public int MouseX;

		public int MouseY;

		public int MouseDeltaX;

		public int MouseDeltaY;

		public bool MouseLeft;

		public bool MouseRight;

		public bool MouseMiddle;

		public bool MouseLeftJustPressed;

		public bool MouseRightJustPressed;

		public int WheelDelta;

		public bool MouseCaptured;

		private int centerX;

		private int centerY;

		private bool warping;

		public void SetKey(Keys k, bool isDown)
		{
			lock (padlock)
			{
				if (isDown)
				{
					if (down.Add(k))
					{
						pressedThisFrame.Add(k);
					}
				}
				else
				{
					down.Remove(k);
				}
			}
		}

		public bool IsDown(Keys k)
		{
			lock (padlock)
			{
				return down.Contains(k);
			}
		}

		public bool Pressed(Keys k)
		{
			lock (padlock)
			{
				return pressedThisFrame.Contains(k);
			}
		}

		public void SetMouseButton(MouseButtons b, bool isDown)
		{
			lock (padlock)
			{
				switch (b)
				{
				case MouseButtons.Left:
					if (isDown && !MouseLeft)
					{
						MouseLeftJustPressed = true;
					}
					MouseLeft = isDown;
					break;
				case MouseButtons.Right:
					if (isDown && !MouseRight)
					{
						MouseRightJustPressed = true;
					}
					MouseRight = isDown;
					break;
				case MouseButtons.Middle:
					MouseMiddle = isDown;
					break;
				}
			}
		}

		public void OnMouseMove(int x, int y)
		{
			lock (padlock)
			{
				if (MouseCaptured)
				{
					MouseDeltaX += x - centerX;
					MouseDeltaY += y - centerY;
				}
				MouseX = x;
				MouseY = y;
			}
		}

		public void SetCapture(bool captured, int clientWidth, int clientHeight)
		{
			lock (padlock)
			{
				MouseCaptured = captured;
				centerX = clientWidth / 2;
				centerY = clientHeight / 2;
			}
		}

		public void GetWarpTarget(out int x, out int y)
		{
			lock (padlock)
			{
				x = centerX;
				y = centerY;
			}
		}

		public void MarkWarped()
		{
			lock (padlock)
			{
				warping = true;
			}
		}

		public void OnWheel(int delta)
		{
			lock (padlock)
			{
				WheelDelta += delta;
			}
		}

		public void EndFrame()
		{
			lock (padlock)
			{
				pressedThisFrame.Clear();
				MouseLeftJustPressed = false;
				MouseRightJustPressed = false;
				WheelDelta = 0;
				MouseDeltaX = 0;
				MouseDeltaY = 0;
			}
		}
	}
}
