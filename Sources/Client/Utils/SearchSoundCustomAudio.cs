using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Comfort.Common;
using EFT.UI;
using QuickPrice.Logging;

namespace QuickPrice.Utils
{
    /// <summary>
    /// 搜索音效自定义加载（从本地文件直接播放）
    /// </summary>
    public static class SearchSoundCustomAudio
    {
        private const string SupportedExtension = ".mp3";
        private static readonly Dictionary<int, AudioClip> LoadedClips = new Dictionary<int, AudioClip>();
        private static readonly HashSet<int> LoadingLevels = new HashSet<int>();
        private static readonly HashSet<int> MissingLevels = new HashSet<int>();
        private static readonly object SyncRoot = new object();
        private static bool PreloadStarted;
        private static AudioSource AudioSource;
        private const string AudioSourceName = "QP_SearchSoundAudioSource";

        /// <summary>
        /// 尝试播放自定义音效（若未加载则触发异步加载）
        /// </summary>
        public static bool TryPlayCustomSound(int priceLevel)
        {
            ClientLog.Debug($"🔊 TryPlayCustomSound: level={priceLevel}");
            var clip = TryGetLoadedClip(priceLevel);
            if (clip == null)
            {
                ClientLog.Debug($"🔊 Clip not loaded, start async load: level={priceLevel}");
                TryLoadAndPlay(priceLevel);
                return false;
            }

            var played = PlayClip(clip);
            ClientLog.Debug($"🔊 PlayClip result: level={priceLevel}, name={clip.name}, played={played}");
            return played;
        }

        /// <summary>
        /// 是否已加载对应等级的自定义音效
        /// </summary>
        public static bool HasCustomSound(int priceLevel)
        {
            return TryGetLoadedClip(priceLevel) != null;
        }

        /// <summary>
        /// 游戏启动时预加载所有搜索音效
        /// </summary>
        public static void PreloadAll()
        {
            if (PreloadStarted)
            {
                ClientLog.Debug("🔊 PreloadAll skipped: already started");
                return;
            }

            var plugin = Plugin.Instance;
            if (plugin == null)
            {
                ClientLog.Debug("🔊 PreloadAll skipped: Plugin.Instance is null");
                return;
            }

            PreloadStarted = true;
            ClientLog.Debug("🔊 PreloadAll started");
            EnsureAudioSource();
            plugin.StartCoroutine(PreloadAllCoroutine());
        }

        private static AudioClip TryGetLoadedClip(int priceLevel)
        {
            lock (SyncRoot)
            {
                LoadedClips.TryGetValue(priceLevel, out var clip);
                if (clip != null)
                {
                    ClientLog.Debug($"🔊 Cache hit: level={priceLevel}, name={clip.name}");
                }
                return clip;
            }
        }

        private static bool BeginLoading(int priceLevel)
        {
            lock (SyncRoot)
            {
                if (LoadedClips.ContainsKey(priceLevel) || LoadingLevels.Contains(priceLevel) || MissingLevels.Contains(priceLevel))
                {
                    ClientLog.Debug($"🔊 BeginLoading blocked: level={priceLevel}, loaded={LoadedClips.ContainsKey(priceLevel)}, loading={LoadingLevels.Contains(priceLevel)}, missing={MissingLevels.Contains(priceLevel)}");
                    return false;
                }

                LoadingLevels.Add(priceLevel);
                ClientLog.Debug($"🔊 BeginLoading ok: level={priceLevel}");
                return true;
            }
        }

        private static string ResolveClipPath(int priceLevel)
        {
            string baseDir = GetSearchSoundDirectory();
            if (string.IsNullOrWhiteSpace(baseDir))
            {
                ClientLog.Debug($"🔊 ResolveClipPath failed: baseDir is empty, level={priceLevel}");
                return null;
            }

            string path = Path.Combine(baseDir, $"search_level{priceLevel}{SupportedExtension}");
            ClientLog.Debug($"🔊 ResolveClipPath: level={priceLevel}, path={path}, exists={File.Exists(path)}");
            return File.Exists(path) ? path : null;
        }

        private static string GetSearchSoundDirectory()
        {
            string pluginDir = GetPluginDirectory();
            if (string.IsNullOrWhiteSpace(pluginDir))
            {
                ClientLog.Debug("🔊 GetSearchSoundDirectory failed: pluginDir is empty");
                return null;
            }

            string dir = Path.Combine(pluginDir, "sounds", "search");
            ClientLog.Debug($"🔊 GetSearchSoundDirectory: {dir}");
            return dir;
        }

        private static string GetPluginDirectory()
        {
            string location = Plugin.Instance?.Info?.Location;
            if (string.IsNullOrWhiteSpace(location))
            {
                ClientLog.Debug("🔊 GetPluginDirectory failed: Plugin.Instance.Info.Location is empty");
                return null;
            }

            string dir = Path.GetDirectoryName(location);
            ClientLog.Debug($"🔊 GetPluginDirectory: {dir}");
            return dir;
        }

        private static AudioSource EnsureAudioSource()
        {
            if (AudioSource != null)
            {
                ClientLog.Debug("🔊 EnsureAudioSource: reuse existing AudioSource");
                return AudioSource;
            }

            var plugin = Plugin.Instance;
            if (plugin == null)
            {
                ClientLog.Debug("🔊 EnsureAudioSource failed: Plugin.Instance is null");
                return null;
            }

            var existing = GameObject.Find(AudioSourceName);
            if (existing == null)
            {
                ClientLog.Debug("🔊 EnsureAudioSource: create new GameObject");
                existing = new GameObject(AudioSourceName);
                UnityEngine.Object.DontDestroyOnLoad(existing);
            }

            AudioSource = existing.GetComponent<AudioSource>();
            if (AudioSource == null)
            {
                ClientLog.Debug("🔊 EnsureAudioSource: add AudioSource component");
                AudioSource = existing.AddComponent<AudioSource>();
            }

            AudioSource.playOnAwake = false;
            AudioSource.loop = false;
            AudioSource.spatialBlend = 0f;
            AudioSource.dopplerLevel = 0f;
            AudioSource.volume = 1f;

            ClientLog.Debug("🔊 EnsureAudioSource: ready");
            return AudioSource;
        }

        private static bool PlayClip(AudioClip clip)
        {
            if (clip == null)
                return false;

            var guiSounds = Singleton<GUISounds>.Instance;
            if (guiSounds != null)
            {
                ClientLog.Debug($"🔊 PlayClip via GUISounds: {clip.name}");
                guiSounds.PlaySound(clip, single: false, commonUiSound: true);
                return true;
            }

            var source = EnsureAudioSource();
            if (source == null)
            {
                ClientLog.Debug($"🔊 PlayClip failed: no AudioSource, clip={clip.name}");
                return false;
            }

            ClientLog.Debug($"🔊 PlayClip via AudioSource: {clip.name}");
            source.PlayOneShot(clip);
            return true;
        }

        private static void TryLoadAndPlay(int priceLevel)
        {
            string path = ResolveClipPath(priceLevel);
            if (string.IsNullOrWhiteSpace(path))
            {
                ClientLog.Debug($"🔊 TryLoadAndPlay failed: path empty, level={priceLevel}");
                MarkMissing(priceLevel);
                return;
            }

            if (!BeginLoading(priceLevel))
                return;

            var plugin = Plugin.Instance;
            if (plugin == null)
            {
                ClientLog.Debug($"🔊 TryLoadAndPlay failed: Plugin.Instance is null, level={priceLevel}");
                MarkMissing(priceLevel);
                return;
            }

            ClientLog.Debug($"🔊 TryLoadAndPlay start coroutine: level={priceLevel}, path={path}");
            plugin.StartCoroutine(LoadClipAndPlayCoroutine(priceLevel, path));
        }

        private static IEnumerator LoadClipCoroutine(int priceLevel, string path)
        {
            ClientLog.Debug($"🔊 LoadClipCoroutine start: level={priceLevel}, path={path}");
            var uri = new Uri(path).AbsoluteUri;
            var audioType = GetAudioType(path);

            using (var request = UnityWebRequestMultimedia.GetAudioClip(uri, audioType))
            {
                yield return request.SendWebRequest();
#if UNITY_2020_2_OR_NEWER
                bool hasError = request.result != UnityWebRequest.Result.Success;
#else
                bool hasError = request.isNetworkError || request.isHttpError;
#endif
                if (hasError)
                {
                    Plugin.Log.LogError($"搜索音效加载失败: {path} - {request.error}");
                    ClientLog.Debug($"🔊 LoadClipCoroutine error: level={priceLevel}, error={request.error}");
                    MarkMissing(priceLevel);
                    yield break;
                }

                var clip = DownloadHandlerAudioClip.GetContent(request);
                if (clip == null)
                {
                    Plugin.Log.LogError($"搜索音效加载失败: {path} - AudioClip 为空");
                    ClientLog.Debug($"🔊 LoadClipCoroutine clip null: level={priceLevel}");
                    MarkMissing(priceLevel);
                    yield break;
                }

                clip.name = Path.GetFileNameWithoutExtension(path);
                lock (SyncRoot)
                {
                    LoadedClips[priceLevel] = clip;
                    LoadingLevels.Remove(priceLevel);
                }
                ClientLog.Debug($"🔊 LoadClipCoroutine success: level={priceLevel}, name={clip.name}");
            }
        }

        private static IEnumerator LoadClipAndPlayCoroutine(int priceLevel, string path)
        {
            ClientLog.Debug($"🔊 LoadClipAndPlayCoroutine start: level={priceLevel}, path={path}");
            var uri = new Uri(path).AbsoluteUri;
            var audioType = GetAudioType(path);

            using (var request = UnityWebRequestMultimedia.GetAudioClip(uri, audioType))
            {
                yield return request.SendWebRequest();
#if UNITY_2020_2_OR_NEWER
                bool hasError = request.result != UnityWebRequest.Result.Success;
#else
                bool hasError = request.isNetworkError || request.isHttpError;
#endif
                if (hasError)
                {
                    Plugin.Log.LogError($"搜索音效加载失败: {path} - {request.error}");
                    ClientLog.Debug($"🔊 LoadClipAndPlayCoroutine error: level={priceLevel}, error={request.error}");
                    MarkMissing(priceLevel);
                    yield break;
                }

                var clip = DownloadHandlerAudioClip.GetContent(request);
                if (clip == null)
                {
                    Plugin.Log.LogError($"搜索音效加载失败: {path} - AudioClip 为空");
                    ClientLog.Debug($"🔊 LoadClipAndPlayCoroutine clip null: level={priceLevel}");
                    MarkMissing(priceLevel);
                    yield break;
                }

                clip.name = Path.GetFileNameWithoutExtension(path);
                lock (SyncRoot)
                {
                    LoadedClips[priceLevel] = clip;
                    LoadingLevels.Remove(priceLevel);
                }
                ClientLog.Debug($"🔊 LoadClipAndPlayCoroutine loaded: level={priceLevel}, name={clip.name}");

                if (!PlayClip(clip))
                {
                    ClientLog.Debug($"搜索音效已加载但未播放: level={priceLevel}, path={path}");
                }
            }
        }

        private static IEnumerator PreloadAllCoroutine()
        {
            ClientLog.Debug("🔊 PreloadAllCoroutine start");
            for (int level = 1; level <= 6; level++)
            {
                if (TryGetLoadedClip(level) != null)
                    continue;

                string path = ResolveClipPath(level);
                if (string.IsNullOrWhiteSpace(path))
                {
                    ClientLog.Debug($"🔊 Preload skip missing file: level={level}");
                    MarkMissing(level);
                    continue;
                }

                if (!BeginLoading(level))
                    continue;

                yield return LoadClipCoroutine(level, path);
            }
            ClientLog.Debug("🔊 PreloadAllCoroutine done");
        }

        private static void MarkMissing(int priceLevel)
        {
            lock (SyncRoot)
            {
                LoadingLevels.Remove(priceLevel);
                MissingLevels.Add(priceLevel);
            }
            ClientLog.Debug($"🔊 MarkMissing: level={priceLevel}");
        }

        private static AudioType GetAudioType(string path)
        {
            switch (Path.GetExtension(path).ToLowerInvariant())
            {
                case ".mp3":
                default:
                    return AudioType.MPEG;
            }
        }
    }
}
