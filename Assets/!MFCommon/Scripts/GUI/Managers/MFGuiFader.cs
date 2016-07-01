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
using System.Collections;

public class MFGuiFader : MonoBehaviour
{
	public readonly static float FAST = 0.1f;
	public readonly static float NORMAL = 0.25f;
	public readonly static float SLOW = 0.5f;

	// PRIVATE MEMBERS

	static MFGuiFader m_Instance = null;

	Material m_Material = null;
	bool m_Fading = false;
	bool m_Paused = false;

	// GETTERS/SETTERS

	static MFGuiFader Instance
	{
		get
		{
			if (m_Instance == null)
			{
				GameObject go = new GameObject(typeof (MFGuiFader).ToString());
				m_Instance = go.AddComponent<MFGuiFader>();
				DontDestroyOnLoad(go);
			}
			return m_Instance;
		}
	}

	public static bool Fading
	{
		get { return Instance.m_Fading; }
		private set { Instance.m_Fading = value; }
	}

	public static bool Paused
	{
		get { return Instance.m_Paused; }
		set { Instance.m_Paused = value; }
	}

	// PUBLIC METHODS

	public static void FadeIn(float duration = 0.1f)
	{
		FadeIn(duration, Color.black);
	}

	public static void FadeIn(float duration, Color color)
	{
		if (Fading == true)
			return;
		Fading = true;
		Paused = false;

		Instance.StartCoroutine(Instance.FadeIn_Coroutine(duration, color));
	}

	public static void FadeOut(float duration = 0.1f)
	{
		FadeOut(duration, Color.black);
	}

	public static void FadeOut(float duration, Color color)
	{
		if (Fading == true)
			return;
		Fading = true;
		Paused = false;

		Instance.StartCoroutine(Instance.FadeOut_Coroutine(duration, color));
	}

	// MONOBEHAVIOUR INTERFACE

	void Awake()
	{
		m_Instance = this;
		m_Material = LoadMaterial();
		if (m_Material == null)
			Debug.LogWarning("GuiFader material failed to load");
	}

	static Material LoadMaterial()
	{
		/*
		string[] lines =
		{
			"Shader \"Plane/No zTest\" {",
			"	SubShader {",
			"		Pass {",
			"			Blend",
			"			SrcAlpha OneMinusSrcAlpha",
			"			ZWrite Off",
			"			Cull Off",
			"			Fog { Mode Off }",
			"			BindChannels { Bind \"Color\", color }",
			"		}",
			"	}",
			"}"
		};
		return new Material(string.Join(" ", lines));
		*/

		return Resources.Load("Effects/m_gui_fader", typeof(Material)) as Material;
	}

	IEnumerator FadeIn_Coroutine(float duration, Color color)
	{
		float alpha = 0.0f;
		while (alpha < 1.0f)
		{
			yield return new WaitForEndOfFrame();
			if (m_Paused == false)
			{
				alpha = Mathf.Clamp01(alpha + Time.deltaTime/duration);
			}
			DrawQuad(color, alpha);
		}

		m_Fading = false;
	}

	IEnumerator FadeOut_Coroutine(float duration, Color color)
	{
		float alpha = 1.0f;
		while (alpha > 0.0f)
		{
			yield return new WaitForEndOfFrame();
			if (m_Paused == false)
			{
				alpha = Mathf.Clamp01(alpha - Time.deltaTime/duration);
			}
			DrawQuad(color, alpha);
		}

		m_Fading = false;
	}

	void DrawQuad(Color color, float alpha)
	{
		color.a = alpha;
		if (m_Material != null)
			m_Material.SetPass(0);

		GL.PushMatrix();
		GL.LoadOrtho();
		GL.Begin(GL.QUADS);
		GL.Color(color);
		GL.Vertex3(0, 0, -1);
		GL.Vertex3(0, 1, -1);
		GL.Vertex3(1, 1, -1);
		GL.Vertex3(1, 0, -1);
		GL.End();
		GL.PopMatrix();
	}
}
