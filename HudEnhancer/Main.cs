using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;
using UnityModManagerNet;

namespace HudEnhancer.UMM;

#if DEBUG
[EnableReloading]
#endif
public static class Loader
{
	public static UnityModManager.ModEntry ModEntry { get; private set; }
	public static Harmony HarmonyInstance { get; private set; }
	public static HudEnhancer Instance { get; private set; }
	
	internal static HudEnhancerSettings Settings;
	
	private static bool Load(UnityModManager.ModEntry modEntry)
	{
		if (ModEntry != null || Instance != null)
		{
			modEntry.Logger.Warning("HudEnhancer is already loaded!");
			return false;
		}

		ModEntry = modEntry;
		Settings = UnityModManager.ModSettings.Load<HudEnhancerSettings>(modEntry);
		ModEntry.OnUnload = Unload;
		ModEntry.OnToggle = OnToggle;
		ModEntry.OnGUI = OnGUI;
		ModEntry.OnSaveGUI = Settings.Save;

		HarmonyInstance = new Harmony(modEntry.Info.Id);
		return true;
	}
	
	public static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
	{
		if (value)
		{
			try
			{
				HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
				var go = new GameObject("[HudEnhancer]");
				Instance = go.AddComponent<HudEnhancer>();
				UnityEngine.Object.DontDestroyOnLoad(go);
				Instance.Settings = Settings;
			}
			catch (Exception ex)
			{
				modEntry.Logger.LogException($"Failed to load {modEntry.Info.DisplayName}:", ex);
				HarmonyInstance?.UnpatchAll(modEntry.Info.Id);
				if (Instance != null) UnityEngine.Object.DestroyImmediate(Instance.gameObject);
				Instance = null;
				return false;
			}
		}
		else
		{
			HarmonyInstance.UnpatchAll(modEntry.Info.Id);
			if (Instance != null) UnityEngine.Object.DestroyImmediate(Instance.gameObject);
			Instance = null;
		}

		return true;
	}

	private static bool Unload(UnityModManager.ModEntry modEntry)
	{
		return true;
	}

	public class HudEnhancerSettings : UnityModManager.ModSettings, IDrawable
	{
		public override void Save(UnityModManager.ModEntry modEntry)
		{
			Save(this, modEntry);
		}

		public void OnChange()
		{
			Instance?.OnSettingsChanged();
		}
	}
	
	private static void OnGUI(UnityModManager.ModEntry modEntry)
	{
		Settings.Draw(modEntry);
	}
	
		public static void Log(string str)
	{
		ModEntry?.Logger.Log(str);
	}

	public static void LogDebug(string str)
	{
#if DEBUG
		ModEntry?.Logger.Log(str);
#endif
	}
}
