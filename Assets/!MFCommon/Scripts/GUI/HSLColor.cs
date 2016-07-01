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

public struct HSLColor
{
	public float H;
	public float S;
	public float L;
	public float A;

	public HSLColor(float h, float s, float l, float a = 1.0f)
	{
		H = h;
		S = s;
		L = l;
		A = a;
	}

	public HSLColor(Color color)
	{
		HSLColor temp = FromRGBA(color);
		H = temp.H;
		S = temp.S;
		L = temp.L;
		A = temp.A;
	}

	public static HSLColor FromRGBA(Color color)
	{
		float h, s, l, a;
		a = color.a;

		float cmin = Mathf.Min(Mathf.Min(color.r, color.g), color.b);
		float cmax = Mathf.Max(Mathf.Max(color.r, color.g), color.b);

		l = (cmin + cmax)/2f;

		if (cmin == cmax)
		{
			s = 0;
			h = 0;
		}
		else
		{
			float delta = cmax - cmin;

			s = (l <= .5f) ? (delta/(cmax + cmin)) : (delta/(2f - (cmax + cmin)));

			h = 0;

			if (color.r == cmax)
			{
				h = (color.g - color.b)/delta;
			}
			else if (color.g == cmax)
			{
				h = 2f + (color.b - color.r)/delta;
			}
			else if (color.b == cmax)
			{
				h = 4f + (color.r - color.g)/delta;
			}

			h = Mathf.Repeat(h*60f, 360f);
		}

		return new HSLColor(h, s, l, a);
	}

	public Color ToRGBA()
	{
		float r, g, b, a;
		a = this.A;

		float m1, m2;

		//	Note: there is a typo in the 2nd International Edition of Foley and
		//	van Dam's "Computer Graphics: Principles and Practice", section 13.3.5
		//	(The HLS Color Model). This incorrectly replaces the 1f in the following
		//	line with "l", giving confusing results.
		m2 = (L <= .5f) ? (L*(1f + S)) : (L + S - L*S);
		m1 = 2f*L - m2;

		if (S == 0f)
		{
			r = g = b = L;
		}
		else
		{
			r = Value(m1, m2, H + 120f);
			g = Value(m1, m2, H);
			b = Value(m1, m2, H - 120f);
		}

		return new Color(r, g, b, a);
	}

	static float Value(float n1, float n2, float hue)
	{
		hue = Mathf.Repeat(hue, 360f);

		if (hue < 60f)
		{
			return n1 + (n2 - n1)*hue/60f;
		}
		else if (hue < 180f)
		{
			return n2;
		}
		else if (hue < 240f)
		{
			return n1 + (n2 - n1)*(240f - hue)/60f;
		}
		else
		{
			return n1;
		}
	}

	public static implicit operator HSLColor(Color src)
	{
		return FromRGBA(src);
	}

	public static implicit operator Color(HSLColor src)
	{
		return src.ToRGBA();
	}
}
