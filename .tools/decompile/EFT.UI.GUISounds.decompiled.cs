using System;
using System.Collections;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Comfort.Common;
using DG.Tweening;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Audio;

namespace EFT.UI;

public class GUISounds : MonoBehaviour
{
	[CompilerGenerated]
	public class Class2844
	{
		public GUISounds guisounds_0;

		public Action callback;

		public void method_0()
		{
			guisounds_0.method_3();
			callback?.Invoke();
		}
	}

	[CompilerGenerated]
	public class Class2845
	{
		public GUISounds guisounds_0;

		public Action callback;

		public void method_0()
		{
			guisounds_0.method_7();
			callback?.Invoke();
		}
	}

	[CompilerGenerated]
	public class Class2846
	{
		public string fileName;

		public bool method_0(AudioClip x)
		{
			return x.name == fileName;
		}
	}

	private const float float_0 = 2f;

	private const float float_1 = 0.15f;

	private AudioSource audioSource_0;

	private AudioSource audioSource_1;

	private AudioSource audioSource_2;

	private AudioSource audioSource_3;

	private AudioSource audioSource_4;

	private AudioMixer audioMixer_0;

	private Coroutine coroutine_0;

	private IEnumerator ienumerator_0;

	private const string string_0 = "Audio/Music/";

	private AudioClip[] audioClip_0;

	private int int_0;

	private ItemSounds itemSounds_0;

	private UISoundsWrapper uisoundsWrapper_0;

	private CancellationTokenSource cancellationTokenSource_0;

	private AudioMixerGroup audioMixerGroup_0;

	private AudioMixerGroup audioMixerGroup_1;

	[CompilerGenerated]
	private AudioMixer audioMixer_1;

	[CompilerGenerated]
	private bool bool_0;

	[CompilerGenerated]
	private bool bool_1;

	public AudioMixer MasterMixer
	{
		[CompilerGenerated]
		get
		{
			return audioMixer_1;
		}
		[CompilerGenerated]
		set
		{
			audioMixer_1 = value;
		}
	}

	public bool BackgroundMusicActive
	{
		[CompilerGenerated]
		get
		{
			return bool_0;
		}
		[CompilerGenerated]
		set
		{
			bool_0 = value;
		}
	}

	public bool HideoutSoundActive
	{
		[CompilerGenerated]
		get
		{
			return bool_1;
		}
		[CompilerGenerated]
		set
		{
			bool_1 = value;
		}
	}

	public async Task method_0(AudioSource audioSource)
	{
		ResourceRequest resourceRequest = Resources.LoadAsync<AudioMixer>("Audio/MasterMixer");
		await GClass841.Await(resourceRequest);
		ResourceRequest resourceRequest2 = Resources.LoadAsync<UISoundsWrapper>("Audio/UISoundsWrapper");
		await GClass841.Await(resourceRequest2);
		audioMixer_0 = (AudioMixer)resourceRequest.asset;
		uisoundsWrapper_0 = (UISoundsWrapper)resourceRequest2.asset;
		audioSource_0 = audioSource;
		audioSource_3 = audioSource.gameObject.AddComponent<AudioSource>();
		audioSource_1 = audioSource.gameObject.AddComponent<AudioSource>();
		audioSource_2 = audioSource.gameObject.AddComponent<AudioSource>();
		audioSource_4 = audioSource.gameObject.AddComponent<AudioSource>();
		MasterMixer = audioMixer_0;
		audioMixerGroup_0 = audioMixer_0.FindMatchingGroups("UI").First();
		audioMixerGroup_1 = audioMixer_0.FindMatchingGroups("InGame/Inventory").First();
		audioSource_0.outputAudioMixerGroup = audioMixerGroup_0;
		audioSource_1.outputAudioMixerGroup = audioMixerGroup_0;
		audioSource_3.outputAudioMixerGroup = audioMixer_0.FindMatchingGroups("Music").First();
		audioSource_2.outputAudioMixerGroup = audioMixer_0.FindMatchingGroups("Chat").First();
		audioSource_4.outputAudioMixerGroup = audioMixer_0.FindMatchingGroups("UI").First();
		audioSource_3.playOnAwake = false;
		audioClip_0 = Resources.LoadAll<AudioClip>("Audio/Music/");
	}

	public void method_1()
	{
		GClass1857.WaitForAllBundles(GClass1857.Retain(Singleton<IEasyAssets>.Instance, new string[1] { "assets/content/audio/itemsounds/itemsounds.bundle" }), delegate
		{
			itemSounds_0 = GClass1857.GetAsset<ItemSounds>(Singleton<IEasyAssets>.Instance, "assets/content/audio/itemsounds/itemsounds.bundle");
		});
	}

	public void PlayKarmaSound(bool isPositive)
	{
		AudioClip audioClip = (isPositive ? uisoundsWrapper_0._karmaPositiveSound : uisoundsWrapper_0._karmaNegativeSound);
		if (!(audioClip == null))
		{
			PlaySound(audioClip);
		}
	}

	public void PlaySound(AudioClip clip, bool single = false, bool commonUiSound = false, float volume = 1f)
	{
		AudioMixerGroup outputAudioMixerGroup = audioMixerGroup_0;
		if (commonUiSound)
		{
			outputAudioMixerGroup = method_12();
			volume = method_13();
		}
		if (!single)
		{
			if (commonUiSound)
			{
				audioSource_1.outputAudioMixerGroup = outputAudioMixerGroup;
				audioSource_1.PlayOneShot(clip, volume);
			}
			else
			{
				audioSource_0.outputAudioMixerGroup = audioMixerGroup_0;
				audioSource_0.PlayOneShot(clip);
			}
		}
		else
		{
			audioSource_4.Stop();
			audioSource_4.outputAudioMixerGroup = outputAudioMixerGroup;
			audioSource_4.PlayOneShot(clip, volume);
		}
	}

	public async Task ForcePlaySound(AudioClip clip)
	{
		cancellationTokenSource_0?.Cancel();
		CancellationTokenSource cancellationTokenSource = (cancellationTokenSource_0 = new CancellationTokenSource());
		SoundSettingsControllerClass settings = Singleton<SharedGameSettingsClass>.Instance.Sound.Settings;
		bool flag = settings.InterfaceVolumeValue < -17;
		bool flag2 = settings.MusicVolumeValue > -40;
		if (flag)
		{
			MasterMixer.SetFloat("InterfaceVolume", -17f);
		}
		if (flag2)
		{
			MasterMixer.SetFloat("MusicVolume", -40f);
		}
		audioSource_4.Stop();
		audioSource_4.PlayOneShot(clip);
		await Task.Delay(Mathf.CeilToInt(clip.length * 1000f));
		if (!cancellationTokenSource.IsCancellationRequested)
		{
			if (flag)
			{
				MasterMixer.SetFloat("InterfaceVolume", settings.InterfaceVolumeValue);
			}
			if (flag2)
			{
				MasterMixer.SetFloat("MusicVolume", settings.MusicVolumeValue);
			}
		}
	}

	public void PlayItemSound(string soundGroup, EInventorySoundType soundType, bool single = false)
	{
		AudioClip itemClip = GetItemClip(soundGroup, soundType);
		if (itemClip == null)
		{
			Debug.LogWarning("Could not find sound: " + soundGroup + "_" + soundType);
		}
		else
		{
			PlaySound(itemClip, single, commonUiSound: true);
		}
	}

	public bool PlayItemSound(EModClass itemClass)
	{
		EUISoundType soundType;
		switch (itemClass)
		{
		case EModClass.None:
			return false;
		case EModClass.Master:
			soundType = EUISoundType.MenuInstallModVital;
			break;
		default:
			soundType = EUISoundType.MenuInstallModFunc;
			break;
		case EModClass.Gear:
			soundType = EUISoundType.MenuInstallModGear;
			break;
		}
		PlayUISound(soundType);
		return true;
	}

	public void method_2(ExitStatus exitStatus)
	{
		bool flag = exitStatus == ExitStatus.Killed || exitStatus == ExitStatus.MissingInAction || exitStatus == ExitStatus.Left;
		PlayEndGameSound(flag ? EEndGameSoundType.ArenaLose : EEndGameSoundType.ArenaWin);
	}

	public void method_3()
	{
		int num;
		do
		{
			num = UnityEngine.Random.Range(0, audioClip_0.Length);
		}
		while (int_0 == num);
		int_0 = num;
		AudioClip audioClip = audioClip_0[int_0];
		method_7();
		audioSource_3.clip = audioClip;
		audioSource_3.Play();
		coroutine_0 = GClass855.WaitSeconds(StaticManager.Instance, audioClip.length, method_3);
	}

	public void method_4(float delay = 1f, Action callback = null)
	{
		GClass7.StopBehaviourTimer(this, ref ienumerator_0);
		ienumerator_0 = GClass7.StartBehaviourTimer(this, delay, delegate
		{
			method_3();
			callback?.Invoke();
		});
	}

	public void method_5()
	{
		if (!audioSource_3.isPlaying && !(audioSource_3.clip == null))
		{
			audioSource_3.Play();
		}
	}

	public void method_6()
	{
		if (audioSource_3.isPlaying)
		{
			audioSource_3.Pause();
		}
	}

	public void method_7()
	{
		BackgroundMusicActive = false;
		method_8();
		audioSource_3.Stop();
		audioSource_3.clip = null;
	}

	public void StopMenuBackgroundMusicWithDelay(float transitionTime = 1f, Action callback = null)
	{
		method_8();
		coroutine_0 = GClass855.WaitSeconds(StaticManager.Instance, transitionTime, delegate
		{
			method_7();
			callback?.Invoke();
		});
	}

	public void method_8()
	{
		if (coroutine_0 != null)
		{
			StaticManager.Instance.StopCoroutine(coroutine_0);
			coroutine_0 = null;
		}
	}

	public void method_9(bool isActive)
	{
		BackgroundMusicActive = isActive;
		audioSource_3.DOFade(isActive ? 1 : 0, 2f);
	}

	public async Task method_10(bool isActive, CancellationToken token)
	{
		BackgroundMusicActive = isActive;
		await GClass2379.AsTask(audioSource_3.DOFade(isActive ? 1 : 0, 2f), token);
	}

	public void method_11(bool active)
	{
		HideoutSoundActive = active;
		int num = ((!active) ? (-80) : Singleton<SharedGameSettingsClass>.Instance.Sound.Settings.HideoutVolumeValue);
		int num2 = ((!active) ? (-80) : 0);
		MasterMixer.DOKill();
		MasterMixer.DOSetFloat("InGame", num2, 1f);
		MasterMixer.DOKill(complete: true);
		MasterMixer.DOSetFloat("HideoutVolume", num, 1f);
	}

	public void SetHideoutLowPassFilter(bool active, float transitionDuration = 1f, bool forced = false)
	{
		float endValue = (active ? 22000f : 0f);
		MasterMixer.DOKill(!forced);
		MasterMixer.DOSetFloat("HideoutLowPass", endValue, transitionDuration);
	}

	[CanBeNull]
	public AudioClip GetItemClip(string soundGroup, EInventorySoundType soundType)
	{
		if (!(itemSounds_0 == null))
		{
			return itemSounds_0.GetClip(soundGroup, soundType);
		}
		return null;
	}

	[CanBeNull]
	public AudioClip GetLootingClip(string fileName)
	{
		if (!(itemSounds_0 == null))
		{
			return itemSounds_0.LootingClips.FirstOrDefault((AudioClip x) => x.name == fileName);
		}
		return null;
	}

	public void PlayUILoadSound()
	{
		AudioClip randomClip = uisoundsWrapper_0.LoadSounds.GetRandomClip();
		if (randomClip != null)
		{
			PlaySound(randomClip, single: false, commonUiSound: true);
		}
	}

	public void PlayUIUnloadSound()
	{
		AudioClip randomClip = uisoundsWrapper_0.UnloadSounds.GetRandomClip();
		if (randomClip != null)
		{
			PlaySound(randomClip, single: false, commonUiSound: true);
		}
	}

	public void PlayUISound(EUISoundType soundType)
	{
		AudioClip uIClip = uisoundsWrapper_0.GetUIClip(soundType);
		if (uIClip != null)
		{
			PlaySound(uIClip);
		}
	}

	public void PlayChatSound(ESocialNetworkSoundType soundType)
	{
		AudioClip socialNetworkClip = uisoundsWrapper_0.GetSocialNetworkClip(soundType);
		if (socialNetworkClip != null)
		{
			audioSource_2.PlayOneShot(socialNetworkClip);
		}
	}

	public void PlayEndGameSound(EEndGameSoundType soundType)
	{
		AudioClip endGameClip = uisoundsWrapper_0.GetEndGameClip(soundType);
		if (endGameClip != null)
		{
			PlaySound(endGameClip);
		}
	}

	public void PlayNotificationSound()
	{
		AudioClip audioClip = CacheResourcesPopAbstractClass.Pop<AudioClip>("Audio/Interface Sounds/notification_exp");
		if (!(audioClip == null))
		{
			audioSource_0.outputAudioMixerGroup = audioMixerGroup_0;
			audioSource_0.PlayOneShot(audioClip);
		}
	}

	public AudioMixerGroup method_12()
	{
		if (Singleton<AbstractGame>.Instantiated && Singleton<AbstractGame>.Instance.InRaid)
		{
			return audioMixerGroup_1;
		}
		return audioMixerGroup_0;
	}

	public float method_13()
	{
		if (Singleton<AbstractGame>.Instantiated && Singleton<AbstractGame>.Instance.InRaid)
		{
			return 0.15f;
		}
		return 1f;
	}

	[CompilerGenerated]
	public void method_14()
	{
		itemSounds_0 = GClass1857.GetAsset<ItemSounds>(Singleton<IEasyAssets>.Instance, "assets/content/audio/itemsounds/itemsounds.bundle");
	}
}
