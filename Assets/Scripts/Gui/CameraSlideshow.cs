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

[AddComponentMenu("GUI/Game/CameraSlideshow")]
public class CameraSlideshow : MonoBehaviour
{
	public GUIBase_Widget fadeWIdget;
	public float fadeTime = 1.0f;

	string[] animName;
	int currentAnim = 0;

	//fade
	float targetFadeTime; //time when we are finished with fade
	int fade = 0; //-1 fadein, 1 fadeout, 0 none 

	void Awake()
	{
		//store animations names
		animName = new string[GetComponent<Animation>().GetClipCount()];
		int i = 0;
		foreach (AnimationState ast in GetComponent<Animation>())
		{
			animName[i++] = ast.clip.name;
		}
	}

	void Start()
	{
		if (animName.Length > 0)
		{
			StartCoroutine("Slideshow");
		}
	}

	void OnDisable()
	{
		StopCoroutine("Slideshow");
	}

	void OnDestroy()
	{
		StopCoroutine("Slideshow");
	}

	void LateUpdate()
	{
		//update fade
		if (fade != 0)
		{
			if (Time.time < targetFadeTime)
			{
				float val = (targetFadeTime - Time.time)/fadeTime; //hodnota ktera je na zacatku 1 a postupne se zmensuje k 0
				float curAlpha = (fade > 0) ? 1.0f - val : val;
				fadeWIdget.FadeAlpha = curAlpha;
			}
			else
			{
				//end fading
				if (fade > 0)
				{
					fadeWIdget.FadeAlpha = 1.0f;
				}
				else
				{
					fadeWIdget.FadeAlpha = 0.0f;
					fadeWIdget.Show(false, true);
				}
				fade = 0;
			}
		}
	}

	void FadeIn()
	{
		fadeWIdget.FadeAlpha = 1.0f;
		fadeWIdget.Show(true, true);
		targetFadeTime = Time.time + fadeTime;
		fade = -1; //-1 fadein, 1 fadeout, 0 none 
	}

	void FadeOut()
	{
		fadeWIdget.FadeAlpha = 0.0f;
		fadeWIdget.Show(true, true);
		targetFadeTime = Time.time + fadeTime;
		fade = 1; //-1 fadein, 1 fadeout, 0 none 
	}

	IEnumerator Slideshow()
	{
		yield return new WaitForSeconds(0.1f);
		fadeWIdget.FadeAlpha = 1.0f;
		fadeWIdget.Show(true, true);

		while (true)
		{
			FadeIn();
			GetComponent<Animation>().Play(animName[currentAnim]);

			//Debug.Log("Playing anim " + animName[currentAnim] + " " + animation[animName[currentAnim]].length);
			yield return new WaitForSeconds(GetComponent<Animation>()[animName[currentAnim]].length - fadeTime);

			FadeOut();
			yield return new WaitForSeconds(fadeTime);
			GetComponent<Animation>().Stop();

			//next anim 
			currentAnim++;
			if (currentAnim >= animName.Length)
				currentAnim = 0;
		}
	}
}
