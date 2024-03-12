using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using Game.State;
using HarmonyLib;
using HudEnhancer.UMM;
using Model;
using System;
using System.Linq;
using UI;
using UI.CarInspector;
using UI.Common;
using UnityEngine;

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

[HarmonyPatch(typeof(CarInspector), nameof(CarInspector.SelectConsist))]
 static class SelectConsistPatch
{
	public static bool Prefix(Car ____car, Window ____window)
	{
		if (GameInput.IsControlDown)
		{
			if (!____car.IsLocomotive)
			{
				var car = ____car.EnumerateCoupled(Car.LogicalEnd.A).First();
				if (car != null && car.IsLocomotive)
				{
					TrainController.Shared.SelectedCar = car;

					if (GameInput.IsShiftDown)
					{
						____window.CloseWindow();
					}
					return false;
				}
			}
		}
		return true;
	}
}
