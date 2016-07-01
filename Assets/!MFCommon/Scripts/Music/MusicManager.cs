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

[AddComponentMenu("Music + Sound/Music Manager")]
[System.Serializable]
public class MusicManager : MonoBehaviour
{
	[System.Serializable]
	public class MusicEvent
	{
		public string Name;
		public GameEvents.E_State State;

		public AudioClip Clip;
		public float FadeOutTime = 1.5f;
		public float FadeInTime = 1.5f;
		public float Volume = 1;

		public MusicEvent(string name, GameEvents.E_State state, AudioClip clip, float fadeoutTime, float fadeInTime, float volume)
		{
			Name = name;
			State = state;
			Clip = clip;
			FadeOutTime = fadeoutTime;
			FadeInTime = fadeInTime;
			Volume = volume;
		}
	}

	public AudioClip DefaultClip;
	public float DefaultFadeInTime = 1.5f;
	public float DefaultVolume = 1;

	public List<MusicEvent> MusicEvents = new List<MusicEvent>();

	public static MusicManager Instance;

	AudioSource Audio;
	const float MaxMusicVolume = 0.0f;
	float UnmodifiedVolume = 1.0f; //volume without modification from GuiOptions

	float ModifiedVolume
	{
		get { return GuiOptions.musicOn ? UnmodifiedVolume*GuiOptions.musicVolume : 0.0f; }
	} //volume modified by options
	float FadeMusicVolume; //valid during fade (in SwitchMusicCoroutine)

	// Use this for initialization
	void Awake()
	{
		Instance = this;
		Audio = GetComponent<AudioSource>();
		Audio.ignoreListenerVolume = true;

		//Debug.Log("MusicManager Awake");
	}

	void Start()
	{
		PlayDefaultMusic();
	}

	void OnDestroy()
	{
		if (Audio)
			Audio.Stop();
	}

	public void ApplyVolumeChange()
	{
		Audio.volume = ModifiedVolume;
	}

	public void PlayDefaultMusic()
	{
		SetNewMusic(DefaultClip, DefaultVolume, 0, DefaultFadeInTime);
	}

	public void SetNewMusic(AudioClip clip, float volume, float fadeOutTime, float fadeIntime)
	{
		StopCoroutine("FadeOutInMusic");
		UnmodifiedVolume = volume;

		//Debug.Log("New music: " + clip.name + " UnmodifiedVolume: " + UnmodifiedVolume + " GuiOptions.musicVolume: " + GuiOptions.musicVolume);
		StartCoroutine(SwitchMusic(clip, ModifiedVolume, fadeOutTime, fadeIntime));
	}

	public void FadeOutMusic(float fadeOutTime)
	{
		StopCoroutine("FadeOutInMusic");
		StartCoroutine(SwitchMusic(null, 0, fadeOutTime, 0));
	}

	IEnumerator SwitchMusic(AudioClip clip, float inMusicVolume, float fadeOutTime, float fadeIntime)
	{
		FadeMusicVolume = inMusicVolume;

		if (Audio.clip == clip)
			yield break;

		if (Audio.isPlaying)
		{
			//Debug.Log("Is playing");
			if (fadeOutTime == 0)
			{
				Audio.volume = 0;
				Audio.Stop();
			}
			else
			{
				float maxVolume = Audio.volume;
				float volume = Audio.volume;
				while (volume > 0)
				{
					volume -= 1/fadeOutTime*Time.deltaTime*maxVolume;

					if (volume < 0)
						volume = 0;

					Audio.volume = volume;
					//Debug.Log("1: Setting volume to: " + volume);

					yield return new WaitForEndOfFrame();
				}
				Audio.Stop();
				Audio.clip = null;
				//Debug.Log("Stop old music");
			}
		}

		yield return new WaitForEndOfFrame();

		if (clip != null)
		{
			//Debug.Log("Set new music: " + clip.name);
			Audio.clip = clip;
			Audio.Play();

			if (fadeIntime == 0)
			{
				Audio.volume = FadeMusicVolume;
			}
			else
			{
				float volume = 0;

				while (volume < FadeMusicVolume)
				{
					volume += 1/fadeIntime*Time.deltaTime*FadeMusicVolume;

					if (volume > FadeMusicVolume)
						volume = FadeMusicVolume;

					Audio.volume = volume;
					//Debug.Log("2: Setting volume to: " + volume);

					yield return new WaitForEndOfFrame();
				}
				Audio.volume = FadeMusicVolume;
			}
		}
	}

	public void EventHandler(string name, GameEvents.E_State state)
	{
		foreach (MusicEvent musicEvent in MusicEvents)
		{
			if (musicEvent.Name == name && musicEvent.State == state)
			{
				SetNewMusic(musicEvent.Clip, musicEvent.Volume, musicEvent.FadeOutTime, musicEvent.FadeInTime);
				return;
			}
		}
	}

	public void ApplyOptionsChange()
	{
		Audio.volume = ModifiedVolume;
		FadeMusicVolume = ModifiedVolume;
		//Debug.Log("Audio.volume: " + Audio.volume + " FadeMusicVolume: " + FadeMusicVolume + " cur clip: " + (Audio.clip ? Audio.clip.name : " null ") + " playing: " + Audio.isPlaying);
	}
}
