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
using System.Collections.Generic;

[AddComponentMenu("Interaction/Use Object")]
[System.Serializable]
public class InteractionObjectUse : InteractionObject
{
	public AnimationClip AnimationClip = null;
	public AudioSource Audio = null;

	public List<GameObject> HideGameObjects = new List<GameObject>();
	public List<GameObject> ShowGameObjects = new List<GameObject>();
	public bool DisableAfterUse = true;
	public GameObject Visual = null;

	void Awake()
	{
		if (Visual != null)
		{
			Animation = Visual.GetComponent<Animation>();
			Animation.wrapMode = WrapMode.Once;
		}

		throw new System.NotImplementedException();
	}

	public override void OnDestroy()
	{
		AnimationClip = null;
		Audio = null;

		HideGameObjects.Clear();
		ShowGameObjects.Clear();
		Visual = null;

		base.OnDestroy();
	}

	void Start()
	{
		base.Initialize();
	}

	public override void Enable()
	{
		base.Enable();
	}

	public override void Disable()
	{
		base.Disable();
	}

	public override void Reset()
	{
		base.Reset();

		if (Visual != null)
		{
			Animation.Stop();
			if (AnimationClip)
				AnimationClip.SampleAnimation(Visual, 0);
		}
	}

	public override void DoInteraction()
	{
		base.DoInteraction();

		if (DisableAfterUse)
			InteractionObjectUsable = false;

		if (Audio)
			Audio.Play();

		if (AnimationClip && Animation)
			Animation.Play(AnimationClip.name);

		foreach (GameObject go in ShowGameObjects)
			go.SetActive(true);

		foreach (GameObject go in HideGameObjects)
			go.SetActive(false);
	}

//        private IEnumerator SendEvent(string name, GameEvents.E_State state, float delay)
//        {
//            yield return new WaitForSeconds(delay);
//            GameBlackboard.Instance.GameEvents.Update(name, state);
//        }
}
