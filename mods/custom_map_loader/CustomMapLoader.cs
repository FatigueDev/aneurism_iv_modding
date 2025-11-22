using System;
using System.IO;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Managers;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Utilities;

namespace custom_map_loader
{
	public struct MapInfo
	{
		MapInfo(string sceneName, string consoleName)
		{

		}
	}

	[BepInPlugin("custom_map_loader", "Custom Map Loader", "0.0.1")]
	public class CustomMapLoader : BasePlugin
	{
		internal static new ManualLogSource Log;
		internal static bool Initialized = false;
		internal static List<VellocetMap> CustomMaps = new();
		internal static int TotalScenes = 0;

		public override void Load()
		{
			Log = base.Log;

			// var map_icon = "/run/media/fatigue/hdd/Games/SteamLibrary/steamapps/common/ANEURISM IV/BepInEx/plugins/custom_map/assets/maps/icon.png";
			// var assetBundlePath = "/run/media/fatigue/hdd/Games/SteamLibrary/steamapps/common/ANEURISM IV/BepInEx/plugins/custom_map/assets/maps/test_bundle";
			// var assetBundlePath2 = "/run/media/fatigue/hdd/Games/SteamLibrary/steamapps/common/ANEURISM IV/BepInEx/plugins/custom_map/assets/maps/office";

			// Texture2D icon = LoadIcon(map_icon);

			// RegisterAssetBundle(assetBundlePath, icon);
			// RegisterAssetBundle(assetBundlePath2, icon);

			SceneManager.activeSceneChanged += (UnityAction<Scene, Scene>)SceneWasChangedToBootLoader;
			Harmony.CreateAndPatchAll(typeof(CustomMapLoader));

			Log.LogInfo($"Plugin custom_map_loader loaded successfully.");
		}

		public static Texture2D LoadIcon(string path)
		{

			byte[] iconBytes = File.ReadAllBytes(path);

			Texture2D icon = new Texture2D(2, 2);
			ImageConversion.LoadImage(icon, iconBytes);

			return icon;
		}

		public static AssetBundle RegisterAssetBundle(string path, Texture2D icon = null)
		{
			var bundle = AssetBundle.LoadFromFile(path);

			foreach(var scenePath in bundle.GetAllScenePaths())
			{
				TotalScenes++;

				var sceneName = Path.GetFileNameWithoutExtension(scenePath);

				VellocetMap customMap = ScriptableObject.CreateInstance<VellocetMap>();
				customMap.buildIndex = OriginalSceneCount + TotalScenes;
				customMap.scene = scenePath;
				customMap.name = sceneName;
				customMap.sceneConsoleName = sceneName.ToLower();
				customMap.screenshot = icon;
				customMap.sceneName = sceneName;

				// Log.LogInfo($"Custom map scene is {customMap.scene}");
				// Log.LogInfo($"Custom map name is {customMap.name}");
				// Log.LogInfo($"Custom map sceneConsoleName is {customMap.sceneConsoleName}");
				// Log.LogInfo($"Custom map sceneName is {customMap.sceneName}");

				CustomMaps.Add(customMap);
			}

			return bundle;
		}

		public void SceneWasChangedToBootLoader(Scene from, Scene to)
		{
			if(to.name == "BootLoader" && !Initialized)
			{
				Initialized = true;
				Log.LogInfo($"Entering BootLoader, running append maps");
				AppendCustomMapsToVellocetSceneManager();
			}
		}

		public void AppendCustomMapsToVellocetSceneManager()
		{
			var vellocetMaps = VellocetSceneManager.Maps;

			VellocetMap[] intermediateMaps = [];

			foreach(var map in vellocetMaps.allMaps)
			{
				intermediateMaps = intermediateMaps.AddToArray(map);
			}

			foreach(var map in CustomMaps)
			{
				intermediateMaps = intermediateMaps.AddToArray(map);
			}

			vellocetMaps.allMaps = intermediateMaps;
		}

		private static int OriginalSceneCount = SceneManager.sceneCountInBuildSettings;

		[HarmonyPostfix]
		[HarmonyPatch(typeof(SceneManagerAPIInternal), nameof(SceneManagerAPIInternal.GetNumScenesInBuildSettings))]
		static void OverrideSceneCount(ref int __result) => __result = OriginalSceneCount + TotalScenes;

		[HarmonyPrefix]
		[HarmonyPatch(typeof(SceneUtility), nameof(SceneUtility.GetScenePathByBuildIndex))]
		static bool OverrideGetScenePathByBuildIndex(int buildIndex, ref string __result)
		{
			if (buildIndex > OriginalSceneCount) {
				// Log.LogInfo($"Build index is {buildIndex} in GetScenePathByBuildIndex");
				var scenePath = CustomMaps.Where(el => el.buildIndex == buildIndex).Select(el => el.scene).SingleOrDefault();
				__result = scenePath;
				return false;
			}
			return true;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(SceneManager), nameof(SceneManager.GetSceneByBuildIndex))]
		static bool OverrideSceneByBuildIndex(int buildIndex, ref Scene __result)
		{
			if (buildIndex > 6) {
				// Log.LogInfo($"Scene build index is {buildIndex} in GetSceneByBuildIndex");
				var sceneName = CustomMaps.Where(el => el.buildIndex == buildIndex).Select(el => el.sceneName).SingleOrDefault();
				__result = SceneManager.GetSceneByName(sceneName);
				return false;
			}
			return true;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(SceneManager), nameof(SceneManager.LoadSceneAsync), argumentTypes: [typeof(int), typeof(LoadSceneMode)])]
		static bool OverrideLoadSceneAsync(ref AsyncOperation __result, int sceneBuildIndex, LoadSceneMode mode)
		{
			if (sceneBuildIndex > 6) {
				// Log.LogInfo($"Scene index is {sceneBuildIndex} on LoadSceneAsync, loading Custom Scene...");
				var sceneName = CustomMaps.Where(el => el.buildIndex == sceneBuildIndex).Select(el => el.sceneName).SingleOrDefault();
				__result = SceneManager.LoadSceneAsync(sceneName, mode);
				return false;
			}
			return true;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(SceneManager), nameof(SceneManager.UnloadSceneAsync), argumentTypes: [typeof(int)])]
		static bool OverrideUnloadSceneAsync(ref AsyncOperation __result, int sceneBuildIndex)
		{
			if (sceneBuildIndex > 6) {
				// Log.LogInfo($"Scene index is {sceneBuildIndex} on LoadSceneAsync, loading Custom Scene...");
				var sceneName = CustomMaps.Where(el => el.buildIndex == sceneBuildIndex).Select(el => el.sceneName).SingleOrDefault();
				__result = SceneManager.UnloadSceneAsync(sceneName);
				return false;
			}
			return true;
		}
	}
}