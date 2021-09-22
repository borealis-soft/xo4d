using System.Collections.Generic;
using UnityEngine;

public class MusicController : MonoBehaviour
{
	public static MusicController Instance;
	public List<AudioClip> musicList;
	public float TimeUnderNextMusic = 1.5f;

	private AudioSource audioSource;
	private float timeCounter = 0f;
	private int curentMusicId = 0;

	private void Awake()
	{
		if (Instance)
		{
			Destroy(this);
			return;
		}
		Instance = this;
		DontDestroyOnLoad(this);
		audioSource = GetComponent<AudioSource>();
	}

	void Start()
	{
		ShuflMusicList();
		PlayAudio();
	}

	void Update()
	{
		if (audioSource.isPlaying) return;
		if (timeCounter > 0) timeCounter -= Time.deltaTime;
		else
		{
			if (curentMusicId++ == musicList.Count - 1) ShuflMusicList();
			PlayAudio();
		}
	}

	private void ShuflMusicList()
	{
		var random = new System.Random();
		musicList.Sort(delegate (AudioClip a, AudioClip b) { return random.Next(-1, 2); });
		curentMusicId = 0;
	}

	private void PlayAudio()
	{
		audioSource.PlayOneShot(musicList[curentMusicId]);
		timeCounter = TimeUnderNextMusic;
	}
}
