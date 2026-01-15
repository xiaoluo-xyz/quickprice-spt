using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Comfort.Common;
using EFT.UI;
using UnityEngine;
using UnityEngine.Networking;

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

        /// <summary>
        /// 尝试播放自定义音效（若未加载则触发异步加载）
        /// </summary>
        public static bool TryPlayCustomSound(int priceLevel)
        {
            var clip = TryGetLoadedClip(priceLevel);
            if (clip == null)
            {
                return false;
            }

            var guiSounds = Singleton<GUISounds>.Instance;
            if (guiSounds == null)
                return false;

            guiSounds.PlaySound(clip, single: false, commonUiSound: true);
            return true;
        }

        /// <summary>
        /// 游戏启动时预加载所有搜索音效
        /// </summary>
        public static void PreloadAll()
        {
            if (PreloadStarted)
                return;

            var plugin = Plugin.Instance;
            if (plugin == null)
                return;

            PreloadStarted = true;
            plugin.StartCoroutine(PreloadAllCoroutine());
        }

        private static AudioClip TryGetLoadedClip(int priceLevel)
        {
            lock (SyncRoot)
            {
                LoadedClips.TryGetValue(priceLevel, out var clip);
                return clip;
            }
        }

        private static bool BeginLoading(int priceLevel)
        {
            lock (SyncRoot)
            {
                if (LoadedClips.ContainsKey(priceLevel) || LoadingLevels.Contains(priceLevel) || MissingLevels.Contains(priceLevel))
                    return false;

                LoadingLevels.Add(priceLevel);
                return true;
            }
        }

        private static string ResolveClipPath(int priceLevel)
        {
            string baseDir = GetSearchSoundDirectory();
            if (string.IsNullOrWhiteSpace(baseDir))
                return null;

            string path = Path.Combine(baseDir, $"search_level{priceLevel}{SupportedExtension}");
            return File.Exists(path) ? path : null;
        }

        private static string GetSearchSoundDirectory()
        {
            string pluginDir = GetPluginDirectory();
            if (string.IsNullOrWhiteSpace(pluginDir))
                return null;

            return Path.Combine(pluginDir, "sounds", "search");
        }

        private static string GetPluginDirectory()
        {
            string location = Plugin.Instance?.Info?.Location;
            if (string.IsNullOrWhiteSpace(location))
                return null;

            return Path.GetDirectoryName(location);
        }

        private static IEnumerator LoadClipCoroutine(int priceLevel, string path)
        {
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
                    MarkMissing(priceLevel);
                    yield break;
                }

                var clip = DownloadHandlerAudioClip.GetContent(request);
                if (clip == null)
                {
                    Plugin.Log.LogError($"搜索音效加载失败: {path} - AudioClip 为空");
                    MarkMissing(priceLevel);
                    yield break;
                }

                clip.name = Path.GetFileNameWithoutExtension(path);
                lock (SyncRoot)
                {
                    LoadedClips[priceLevel] = clip;
                    LoadingLevels.Remove(priceLevel);
                }
            }
        }

        private static IEnumerator PreloadAllCoroutine()
        {
            for (int level = 1; level <= 6; level++)
            {
                if (TryGetLoadedClip(level) != null)
                    continue;

                string path = ResolveClipPath(level);
                if (string.IsNullOrWhiteSpace(path))
                {
                    MarkMissing(level);
                    continue;
                }

                if (!BeginLoading(level))
                    continue;

                yield return LoadClipCoroutine(level, path);
            }
        }

        private static void MarkMissing(int priceLevel)
        {
            lock (SyncRoot)
            {
                LoadingLevels.Remove(priceLevel);
                MissingLevels.Add(priceLevel);
            }
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
