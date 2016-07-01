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

[ExecuteInEditMode]
[AddComponentMenu("Image Effects/MADFINGER color correction - simple")]
public class MFColorCorrectionEffectSimple : ImageEffectBase
{
	public float R_offs = 0;
	public float G_offs = 0;
	public float B_offs = 0;

	bool HACK_CompensateImageFlip = false;

	void Awake()
	{
#if (UNITY_EDITOR || UNITY_STANDALONE) && !UNITY_STANDALONE_OSX
		HACK_CompensateImageFlip = true;
#endif

		// No need to compensate on OSX in general
		if ((Application.platform == RuntimePlatform.OSXEditor) || (Application.platform == RuntimePlatform.OSXWebPlayer))
		{
			HACK_CompensateImageFlip = false;
		}
	}

	// Called by camera to apply image effect
	void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		MFDebugUtils.Assert(shader);

		if (material)
		{
			material.shader = shader;
			material.SetVector("_ColorBias", new Vector4(R_offs, G_offs, B_offs, 0));
		}

		if (HACK_CompensateImageFlip && PostFXTracking.ScreenspaceLightFXEffectActive)
		{
			material.SetVector("_Params", Vector4.one);
		}
		else
		{
			material.SetVector("_Params", Vector4.zero);
		}

		Graphics.Blit(source, destination, material);
	}
}
