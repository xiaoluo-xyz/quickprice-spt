using System;
using System.Collections.Generic;
using EFT.UI;
using JetBrains.Annotations;
using NLog;
using UnityEngine;

public class UISoundsWrapper : ScriptableObject
{
	[Serializable]
	public class UISound : SoundElement<EUISoundType>
	{
	}

	[Serializable]
	public class SotialNetworkSound : SoundElement<ESocialNetworkSoundType>
	{
	}

	[Serializable]
	public class EndGameSound : SoundElement<EEndGameSoundType>
	{
	}

	[Serializable]
	public abstract class SoundElement<TSoundTypeEnum>
	{
		[SerializeField]
		public TSoundTypeEnum _soundType;

		[SerializeField]
		public AudioClip _sound;

		public TSoundTypeEnum TSoundType => _soundType;

		public AudioClip TSound => _sound;

		public SoundElement()
		{
		}
	}

	[Serializable]
	public class UIClipSounds
	{
		[SerializeField]
		public AudioClip[] _audioClips;

		public AudioClip GetRandomClip()
		{
			AudioClip[] audioClips = _audioClips;
			if (audioClips == null)
			{
				return null;
			}
			return audioClips[UnityEngine.Random.Range(0, _audioClips.Length - 1)];
		}
	}

	public class Class391 : LoggerClass
	{
		public Class391()
			: base(LogManager.GetLogger("ui_sounds"), LoggerMode.Add)
		{
		}

		public void LogMissingSound(Enum soundType)
		{
			LogError("Required AudioClip \"{0}\" has not been loaded. Check the list of AudioClips in \"Assets/Resources/Audio/UISounds\".", soundType);
		}
	}

	[SerializeField]
	private UISound[] _UIAudioClips;

	[SerializeField]
	private SotialNetworkSound[] _socialNetworkAudioClips;

	[SerializeField]
	private EndGameSound[] _endGameAudioClips;

	[SerializeField]
	private UIClipSounds _loadSounds;

	[SerializeField]
	private UIClipSounds _unloadSounds;

	[SerializeField]
	public AudioClip _karmaPositiveSound;

	[SerializeField]
	public AudioClip _karmaNegativeSound;

	private static readonly Class391 class391_0 = new Class391();

	private readonly Dictionary<EUISoundType, AudioClip> dictionary_0 = new Dictionary<EUISoundType, AudioClip>(GClass866<EUISoundType>.EqualityComparer);

	private readonly Dictionary<ESocialNetworkSoundType, AudioClip> dictionary_1 = new Dictionary<ESocialNetworkSoundType, AudioClip>(GClass866<ESocialNetworkSoundType>.EqualityComparer);

	private readonly Dictionary<EEndGameSoundType, AudioClip> dictionary_2 = new Dictionary<EEndGameSoundType, AudioClip>(GClass866<EEndGameSoundType>.EqualityComparer);

	public UIClipSounds LoadSounds => _loadSounds;

	public UIClipSounds UnloadSounds => _unloadSounds;

	[CanBeNull]
	public AudioClip GetSocialNetworkClip(ESocialNetworkSoundType soundType)
	{
		if (!dictionary_1.ContainsKey(soundType))
		{
			class391_0.LogMissingSound(soundType);
			return null;
		}
		return dictionary_1[soundType];
	}

	[CanBeNull]
	public AudioClip GetUIClip(EUISoundType soundType)
	{
		if (!dictionary_0.ContainsKey(soundType))
		{
			class391_0.LogMissingSound(soundType);
			return null;
		}
		return dictionary_0[soundType];
	}

	[CanBeNull]
	public AudioClip GetEndGameClip(EEndGameSoundType soundType)
	{
		if (!dictionary_2.ContainsKey(soundType))
		{
			class391_0.LogMissingSound(soundType);
			return null;
		}
		return dictionary_2[soundType];
	}

	public void OnEnable()
	{
		SoundElement<EUISoundType>[] uIAudioClips = _UIAudioClips;
		smethod_0(uIAudioClips, dictionary_0);
		SoundElement<ESocialNetworkSoundType>[] socialNetworkAudioClips = _socialNetworkAudioClips;
		smethod_0(socialNetworkAudioClips, dictionary_1);
		SoundElement<EEndGameSoundType>[] endGameAudioClips = _endGameAudioClips;
		smethod_0(endGameAudioClips, dictionary_2);
	}

	public static void smethod_0<T>(SoundElement<T>[] clipList, Dictionary<T, AudioClip> clipsIndex)
	{
		clipsIndex.Clear();
		if (clipList == null)
		{
			return;
		}
		foreach (SoundElement<T> soundElement in clipList)
		{
			if (soundElement != null)
			{
				T tSoundType = soundElement.TSoundType;
				if (!clipsIndex.ContainsKey(tSoundType))
				{
					clipsIndex.Add(tSoundType, soundElement.TSound);
				}
			}
		}
	}
}
