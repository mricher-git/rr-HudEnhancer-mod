using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using Game.State;
using HudEnhancer.UMM;
using System;
using UI;
using UI.Builder;
using UI.CarInspector;
using UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace HudEnhancer;

public class HudEnhancer : MonoBehaviour
{
	public enum MapStates { MAINMENU, MAPLOADED, MAPUNLOADING }
	public static MapStates MapState { get; private set; } = MapStates.MAINMENU;
	internal Loader.HudEnhancerSettings Settings;

	private AIControls aiControls;

	public static HudEnhancer Instance
	{
		get { return Loader.Instance; }
	}

	void Start()
	{
		Messenger.Default.Register<MapDidLoadEvent>(this, new Action<MapDidLoadEvent>(this.OnMapDidLoad));
		Messenger.Default.Register<MapWillUnloadEvent>(this, new Action<MapWillUnloadEvent>(this.OnMapWillUnload));

		if (StateManager.Shared.Storage != null)
		{
			OnMapDidLoad(new MapDidLoadEvent());
		}
	}

	private void OnMapDidLoad(MapDidLoadEvent evt)
	{
		Loader.LogDebug("OnMapDidLoad");
		if (MapState == MapStates.MAPLOADED) return;
		MapState = MapStates.MAPLOADED;
		
		aiControls = AIControls.Create();

		//Messenger.Default.Register<WorldDidMoveEvent>(this, new Action<WorldDidMoveEvent>(this.WorldDidMove));
	}

	private void OnMapWillUnload(MapWillUnloadEvent evt)
	{
		Loader.LogDebug("OnMapWillUnload");

		MapState = MapStates.MAPUNLOADING;
		//Messenger.Default.Unregister<WorldDidMoveEvent>(this);
	}

	/*(private void WorldDidMove(WorldDidMoveEvent evt)
	{
		Loader.LogDebug("WorldDidMove");
	}*/

	void OnDestroy()
	{
		Loader.LogDebug("OnDestroy");

		Messenger.Default.Unregister<MapDidLoadEvent>(this);
		Messenger.Default.Unregister<MapWillUnloadEvent>(this);

		if (MapState == MapStates.MAPLOADED)
		{
			OnMapWillUnload(new MapWillUnloadEvent());

			if (aiControls != null) DestroyImmediate(aiControls);
		}

		MapState = MapStates.MAINMENU;
	}

	public void OnSettingsChanged()
	{
		if (MapState != MapStates.MAPLOADED) return;
	}
}
