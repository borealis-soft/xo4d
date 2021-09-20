using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SettingControl : MonoBehaviour
{
	public Dropdown Resolution;
	public Dropdown Quality;
	public AudioMixer SoundMixer;
	public AudioMixer MusicMixer;
	public Toggle FullscreanTog;
	public Slider MusicSlider;
	public Slider SoundSlider;

	private void Awake()
	{
		//Resolution.ClearOptions();
		List<string> strResolution = new List<string>();
		int resId = 0;
		foreach (var resolution in Screen.resolutions)
		{
			strResolution.Add(resolution.width + " X " + resolution.height);
			if (Screen.currentResolution.Equals(resolution))
				resId = strResolution.Count - 1;
		}
		Resolution.AddOptions(strResolution);
		Resolution.value = resId;
		Quality.value = QualitySettings.GetQualityLevel();
		FullscreanTog.isOn = Screen.fullScreen;
		MusicMixer.GetFloat("MusicVolumeParam", out float newVal);
		MusicSlider.value = newVal;
		SoundMixer.GetFloat("SoundVolumeParam", out newVal);
		SoundSlider.value = newVal;
	}

	private void Start()
	{
		FullscreanTog.onValueChanged.AddListener(SetFullScreen);
		MusicSlider.onValueChanged.AddListener(SetMusicVolume);
		SoundSlider.onValueChanged.AddListener(SetSoundVolume);
		Resolution.onValueChanged.AddListener(SetResolution);
		Quality.onValueChanged.AddListener(SetQuality);
	}

	public void SetFullScreen(bool flag)
	{
		Screen.fullScreen = flag;
	}

	public void SetResolution(int n)
	{
		Resolution rsl = Screen.resolutions[n];
		Screen.SetResolution(rsl.width, rsl.height, Screen.fullScreen);
	}

	public void SetQuality(int lvl)
	{
		QualitySettings.SetQualityLevel(lvl);
	}

	public void SetSoundVolume(float val)
	{
		SoundMixer.SetFloat("SoundVolumeParam", val);
	}

	public void SetMusicVolume(float val)
	{
		MusicMixer.SetFloat("MusicVolumeParam", val);
	}
}
