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

[AddComponentMenu("GUI/Frontend/OptionPages/GuiPageOptionsSounds")]
public class GuiPageOptionsSounds : GuiScreen
{
	readonly static string SLIDER_SOUND_VOLUME = "Sound_Slider";
	readonly static string SLIDER_MUSIC_VOLUME = "Music_Slider";
	readonly static string SWITCH_MUSIC = "MusicOn_Switch";

	// PRIVATE MEMBERS

	GUIBase_Slider m_SliderSoundVolume;
	GUIBase_Slider m_SliderMusicVolume;
	GUIBase_Switch m_SwitchMusic;

	// GUIVIEW INTERFACE

	protected override void OnViewInit()
	{
		base.OnViewInit();

		m_SliderSoundVolume = GuiBaseUtils.GetControl<GUIBase_Slider>(Layout, SLIDER_SOUND_VOLUME);
		m_SliderMusicVolume = GuiBaseUtils.GetControl<GUIBase_Slider>(Layout, SLIDER_MUSIC_VOLUME);
		m_SwitchMusic = GuiBaseUtils.GetControl<GUIBase_Switch>(Layout, SWITCH_MUSIC);
	}

	protected override void OnViewShow()
	{
		base.OnViewShow();

		m_SliderSoundVolume.RegisterChangeValueDelegate(OnSoundVolumeChanged);
		m_SliderMusicVolume.RegisterChangeValueDelegate(OnMusicVolumeChanged);
		m_SwitchMusic.RegisterDelegate(OnMusicToggled);
	}

	protected override void OnViewHide()
	{
		m_SliderSoundVolume.RegisterChangeValueDelegate(null);
		m_SliderMusicVolume.RegisterChangeValueDelegate(null);
		m_SwitchMusic.RegisterDelegate(null);

		base.OnViewHide();
	}

	protected override void OnViewReset()
	{
		m_SliderSoundVolume.SetValue(GuiOptions.soundVolume);
		m_SliderMusicVolume.SetValue(GuiOptions.musicVolume);
		m_SwitchMusic.SetValue(GuiOptions.musicOn);
	}

	// HANDLERS

	void OnSoundVolumeChanged(float value)
	{
		GuiOptions.soundVolume = value;
		AudioListener.volume = value;
	}

	void OnMusicVolumeChanged(float value)
	{
		GuiOptions.musicVolume = value;

		//apply music change right now, its better for player feedback
		if (MusicManager.Instance != null)
		{
			MusicManager.Instance.ApplyOptionsChange();
		}
	}

	void OnMusicToggled(bool state)
	{
		GuiOptions.musicOn = state;

		//apply musci change right now, its better for player feedback
		if (MusicManager.Instance != null)
		{
			MusicManager.Instance.ApplyOptionsChange();
		}
	}
}
