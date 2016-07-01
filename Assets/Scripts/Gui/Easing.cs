//
// By using or accessing the source codes or any other information of the Game SHADOWGUN: DeadZone ("Game"),
// you ("You" or "Licensee") agree to be bound by all the terms and conditions of SHADOWGUN: DeadZone Public
// License Agreement (the "PLA") starting the day you access the "Game" under the Terms of the "PLA".
//
// You can review the most current version of the "PLA" at any time at: http://madfingergames.com/pla/deadzone
//
// If you don't agree to all the terms and conditions of the "PLA", you shouldn't, and aren't permitted
// to use or access the source codes or any other information of the "Game" supplied by MADFINGER Games, a.s.
//

using UnityEngine;

namespace Tween
{
	public delegate float EasingFunc(float time, float startValue, float changeValue, float duration);

	// http://theinstructionlimit.com/wp-content/uploads/2009/07/easing.png
	namespace Easing
	{
		public static class Linear
		{
			public static float EaseNone(float time, float startValue, float changeValue, float duration)
			{
				return changeValue*time/duration + startValue;
			}
		}

		public static class Quad
		{
			public static float EaseIn(float time, float startValue, float changeValue, float duration)
			{
				return changeValue*(time /= duration)*time + startValue;
			}

			public static float EaseOut(float time, float startValue, float changeValue, float duration)
			{
				return -changeValue*(time /= duration)*(time - 2) + startValue;
			}

			public static float EaseInOut(float time, float startValue, float changeValue, float duration)
			{
				if ((time /= duration*0.5f) < 1)
				{
					return changeValue*0.5f*time*time + startValue;
				}
				return -changeValue*0.5f*((--time)*(time - 2) - 1) + startValue;
			}
		}

		public static class Sine
		{
			const float PI2 = Mathf.PI*0.5f;

			public static float EaseIn(float time, float startValue, float changeValue, float duration)
			{
				return -changeValue*(float)Mathf.Cos(time/duration*PI2) + changeValue + startValue;
			}

			public static float EaseOut(float time, float startValue, float changeValue, float duration)
			{
				return changeValue*(float)Mathf.Sin(time/duration*PI2) + startValue;
			}

			public static float EaseInOut(float time, float startValue, float changeValue, float duration)
			{
				return -changeValue*0.5f*((float)Mathf.Cos(Mathf.PI*time/duration) - 1) + startValue;
			}
		}

		public static class Strong
		{
			public static float EaseIn(float time, float startValue, float changeValue, float duration)
			{
				return changeValue*(time /= duration)*time*time*time*time + startValue;
			}

			public static float EaseOut(float time, float startValue, float changeValue, float duration)
			{
				return changeValue*((time = time/duration - 1)*time*time*time*time + 1) + startValue;
			}

			public static float EaseInOut(float time, float startValue, float changeValue, float duration)
			{
				if ((time /= duration*0.5f) < 1)
				{
					return changeValue*0.5f*time*time*time*time*time + startValue;
				}
				return changeValue*0.5f*((time -= 2)*time*time*time*time + 2) + startValue;
			}
		}
	}
}
