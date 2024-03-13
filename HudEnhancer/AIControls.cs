using GalaSoft.MvvmLight.Messaging;
using Game;
using Game.Events;
using Game.Messages;
using Game.State;
using Model;
using Model.AI;
using Model.OpsNew;
using Model.Ops.Definition;
using System;
using System.Collections.Generic;
using UI;
using UI.Builder;
using UI.CarInspector;
using UI.Common;
using UnityEngine;
using UnityEngine.UI;
using HarmonyLib;
using TMPro;

namespace HudEnhancer
{
	internal class AIControls : MonoBehaviour
	{
		//private CarInspector _carInspector;
		private Car _car;
		private RectTransform aiModeContentRectTransform;
		private RectTransform aiControlsContentRectTransform;
		private LocomotiveControlsUIAdapter selectedControls;
		private GameObject aiModeControls;
		private GameObject aiControls;

		AutoEngineerPersistence persistence;

		private UIBuilderAssets _builderAssets;
		private UIBuilderAssets BuilderAssets
		{
			get
			{
				if (_builderAssets == null)
					_builderAssets = FindObjectOfType<CarInspector>().BuilderAssets;

				return _builderAssets;
			}
		}

		public static AIControls Create()
		{
			var selectedControls = FindObjectOfType<LocomotiveControlsUIAdapter>();
			selectedControls.controls.sizeDelta = new Vector2(364f, 280f);
			selectedControls.controls.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.UpperLeft;
			var aiControls = selectedControls.controls.gameObject.AddComponent<AIControls>();

			return aiControls;
		}

		void Awake()
		{
			selectedControls = GetComponentInParent<LocomotiveControlsUIAdapter>();

			// AI Mode Controls
			aiModeControls = new GameObject("AI Mode Controls", typeof(RectTransform));
			var aiModeRect = aiModeControls.GetComponent<RectTransform>();
			aiModeRect.pivot = Vector2.zero;
			aiModeRect.SetSizeWithCurrentAnchors(new Vector2(364f, 42f));
			aiModeControls.transform.SetParent(selectedControls.controls.transform, false);
			aiModeControls.transform.SetSiblingIndex(2);

			var controlsBg = selectedControls.trainBrakeSlider.transform.GetChild(0);
			RectTransform bgImageTransform = Instantiate(controlsBg, aiModeRect, false).GetComponent<RectTransform>();
			bgImageTransform.name = "Control Background";
			bgImageTransform.SetFrameFillParent();

			aiModeContentRectTransform = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
			aiModeContentRectTransform.SetParent(aiModeRect, false);
			aiModeContentRectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0f, 364f);
			aiModeContentRectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 6f, 30f);

			// AI Controls
			aiControls = new GameObject("AI Controls", typeof(RectTransform));
			var aiControlsRect = aiControls.GetComponent<RectTransform>();
			aiControlsRect.pivot = Vector2.zero;
			aiControlsRect.SetSizeWithCurrentAnchors(new Vector2(364f, 192f));
			aiControls.transform.SetParent(selectedControls.controls.transform, false);

			bgImageTransform = Instantiate(controlsBg, aiControlsRect, false).GetComponent<RectTransform>();
			bgImageTransform.name = "Control Background";
			bgImageTransform.SetFrameFillParent();

			aiControlsContentRectTransform = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
			aiControlsContentRectTransform.SetParent(aiControlsRect, false);
			aiControlsContentRectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0f, 364f);
			aiControlsContentRectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 6f, 180f);
		}

		void Start()
		{
			Populate();
			Messenger.Default.Register<SelectedCarChanged>(this, delegate (SelectedCarChanged _)
			{
				_car = TrainController.Shared.SelectedLocomotive;
				if (_car != null)
				{
					Populate();
				}
			});
		}

		public void Populate()
		{
			_car = TrainController.Shared?.SelectedLocomotive;
			if (_car != null)
			{
				persistence = new AutoEngineerPersistence(_car.KeyValueObject);
				var modePanel = UIPanel.Create(aiModeContentRectTransform, BuilderAssets,  PopulateAIModePanel);
				var controlPanel = UIPanel.Create(aiControlsContentRectTransform, BuilderAssets, PopulateAIControlsPanel);
			}
		}

		void OnDestroy()
		{
			Messenger.Default.Unregister<SelectedCarChanged>(this);

			if (aiModeControls != null) DestroyImmediate(aiModeControls);
			if (aiControls != null) DestroyImmediate(aiControls);

			if (Preferences.SimplifiedControls)
			{
				selectedControls.simplifiedControls.gameObject.SetActive(true);
			}
			else
			{
				selectedControls.realisticControls.gameObject.SetActive(true);
			}
			selectedControls.controls.sizeDelta = new Vector2(364f, 200f);
			selectedControls.controls.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.LowerLeft;
		}

		private void PopulateAIModePanel(UIPanelBuilder builder)
		{
			var mode = Mode();
			builder.AddField("Mode", builder.ButtonStrip(builder2 =>
			{
				builder.FieldLabelWidth = 100f;
				builder2.AddButtonSelectable("Manual", mode == AutoEngineerMode.Off, () => SetOrdersValue(AutoEngineerMode.Off, null, null, null));
				builder2.AddButtonSelectable("Road", mode == AutoEngineerMode.Road, () => SetOrdersValue(AutoEngineerMode.Road, null, null, null));
				builder2.AddButtonSelectable("Yard", mode == AutoEngineerMode.Yard, () => SetOrdersValue(AutoEngineerMode.Yard, null, null, null));

				if (mode == AutoEngineerMode.Off)
				{
					aiControls.gameObject.SetActive(false);
					if (Preferences.SimplifiedControls)
					{
						selectedControls.simplifiedControls.gameObject.SetActive(true);
						selectedControls.realisticControls.gameObject.SetActive(false);
					}
					else
					{
						selectedControls.simplifiedControls.gameObject.SetActive(false);
						selectedControls.realisticControls.gameObject.SetActive(true);
					}
				}
				else
				{
					aiControls.gameObject.SetActive(true);
					selectedControls.simplifiedControls.gameObject.SetActive(false);
					selectedControls.realisticControls.gameObject.SetActive(false);
				}
			}));

			builder.AddObserver(persistence.ObserveOrders(_ =>
			{
				if (Mode() == mode)
					return;
				builder.Rebuild();
			}, false));
		}

		private void PopulateAIControlsPanel(UIPanelBuilder builder)
		{
			builder.FieldLabelWidth = 100f;
			builder.Spacing = 6f;

			var mode = Mode();

			builder.AddObserver(persistence.ObserveOrders(_ =>
			{
				if (Mode() == mode)
					return;
				builder.Rebuild();
			}, false));

			if (persistence.Orders.Enabled)
			{
				builder.AddField("Direction", builder.ButtonStrip((builder3 =>
				{
					builder3.AddObserver(persistence.ObserveOrders(_ => builder3.Rebuild(), false));
					builder3.AddButtonSelectable("Reverse", !persistence.Orders.Forward, () => SetOrdersValue(null, false, null, null));
					builder3.AddButtonSelectable("Forward", persistence.Orders.Forward, () => SetOrdersValue(null, true, null, null));
				})));

				if (mode == AutoEngineerMode.Road)
				{
					int num = MaxSpeedMphForMode(mode);
					RectTransform control = builder.AddSlider(() => persistence.Orders.MaxSpeedMph / 5, () => persistence.Orders.MaxSpeedMph.ToString(), value => SetOrdersValue(null, null, (int)(value * 5.0), null), maxValue: num / 5f, wholeNumbers: true);
					builder.AddField("Max Speed", control);
				}

				if (mode == AutoEngineerMode.Yard)
				{
					RectTransform control = builder.ButtonStrip(builder4 =>
					{
						builder4.AddButton("<size=16>Stop</size>", () => SetOrdersValue(null, null, null, 0.0f));
						//builder4.AddButton("\u00BD", () => SetOrdersValue(null, null, null, 6.1f));
						builder4.AddButton("<size=16>1</size>", () => SetOrdersValue(null, null, null, 12.2f));
						builder4.AddButton("<size=16>2</size>", () => SetOrdersValue(null, null, null, 24.4f));
						builder4.AddButton("<size=16>5</size>", () => SetOrdersValue(null, null, null, 61f));
						builder4.AddButton("<size=16>10</size>", () => SetOrdersValue(null, null, null, 122f));
						builder4.AddButton("<size=16>20</size>", () => SetOrdersValue(null, null, null, 244f));
						builder4.AddButton("<size=16>1k</size>", () => SetOrdersValue(null, null, null, 12200f));
					}, 4);
					builder.AddField("Car Lengths", control);
				}

				builder.AddObserver(persistence.ObservePassengerModeStatusChanged(() => builder.Rebuild()));
				string passengerModeStatus = persistence.PassengerModeStatus;
				if (mode == AutoEngineerMode.Road && !string.IsNullOrEmpty(passengerModeStatus))
					builder.AddField("Station Stops", passengerModeStatus).Tooltip("AI Passenger Stops", "When stations are checked on passenger cars in the train, the AI engineer will perform stops as those stations are encountered.");
				builder.AddObserver(persistence.ObservePlannerStatusChanged(() => builder.Rebuild()));
				builder.AddField("Status", persistence.PlannerStatus);
				BuildContextualOrders(builder, persistence);

				builder.AddExpandingVerticalSpacer();
			}
		}

		static int MaxSpeedMphForMode(AutoEngineerMode mode)
		{
			switch (mode)
			{
				case AutoEngineerMode.Off:
					return 0;
				case AutoEngineerMode.Road:
					return 45;
				case AutoEngineerMode.Yard:
					return 15;
				default:
					throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
			}
		}

		AutoEngineerMode Mode()
		{
			Orders orders = persistence.Orders;
			if (!orders.Enabled)
				return AutoEngineerMode.Off;
			return !orders.Yard ? AutoEngineerMode.Road : AutoEngineerMode.Yard;
		}

		void SendAutoEngineerCommand(
			AutoEngineerMode mode,
			bool forward,
			int maxSpeedMph,
			float? distance)
		{
			StateManager.ApplyLocal(new AutoEngineerCommand(_car.id, mode, forward, maxSpeedMph, distance));
		}

		void SetOrdersValue(
			AutoEngineerMode? mode,
			bool? forward,
			int? maxSpeedMph,
			float? distance)
		{
			Orders orders = persistence.Orders;
			if (!orders.Enabled && mode.HasValue && mode.Value != AutoEngineerMode.Off && !maxSpeedMph.HasValue)
			{
				float f = _car.velocity * 2.23694f;
				float num = Mathf.Abs(f);
				maxSpeedMph = (double)f > 0.10000000149011612 ? Mathf.CeilToInt(num / 5f) * 5 : 0;
				forward = (double)f >= -0.10000000149011612;
			}
			AutoEngineerMode? nullable = mode;
			AutoEngineerMode autoEngineerMode = AutoEngineerMode.Yard;
			if (nullable.GetValueOrDefault() == autoEngineerMode & nullable.HasValue)
				maxSpeedMph = MaxSpeedMphForMode(AutoEngineerMode.Yard);
			AutoEngineerMode mode1 = mode ?? Mode();
			int maxSpeedMphMin = Mathf.Min(maxSpeedMph ?? orders.MaxSpeedMph, MaxSpeedMphForMode(mode1));
			SendAutoEngineerCommand(mode1, forward ?? orders.Forward, maxSpeedMphMin, distance);
		}

		void BuildContextualOrders(UIPanelBuilder builder, AutoEngineerPersistence persistence)
		{
			builder.AddObserver(persistence.ObserveContextualOrdersChanged(() => builder.Rebuild()));
			List<ContextualOrder> contextualOrders = persistence.ContextualOrders;
			if (contextualOrders.Count <= 0)
				return;
			builder.ButtonStrip(builder2 =>
			{
				builder2.Spacer();
				foreach (ContextualOrder contextualOrder in contextualOrders)
				{
					ContextualOrder co = contextualOrder;
					(string, string) valueTuple;
					switch (co.Order)
					{
						case ContextualOrder.OrderValue.PassSignal:
							valueTuple = ("Pass Signal", "Pass the signal at restricted speed.");
							break;
						case ContextualOrder.OrderValue.PassFlare:
							valueTuple = ("Pass Fusee", "Pass the fusee.");
							break;
						case ContextualOrder.OrderValue.ResumeSpeed:
							valueTuple = ("Resume Speed", "Discard speed restriction.");
							break;
						default:
							valueTuple = ("(Error)", "");
							break;
					}
					(string str2, string message2) = valueTuple;
					builder2.AddButton(str2, () => StateManager.ApplyLocal(new AutoEngineerContextualOrder(_car.id, co.Order, co.Context))).Tooltip(str2, message2);
				}
				builder2.Spacer();
			});
		}
	}

	[HarmonyPatch(typeof(LocomotiveControlsUIAdapter), nameof(LocomotiveControlsUIAdapter.UpdateCarText))]
	public static class UpdateCarTextPatch
	{
		public static void Postfix(TMP_Text ___locomotiveNameLabel)
		{
			var _car = LocomotiveControlsUIAdapter.Locomotive;
			Car fuelCar;
			if (_car is SteamLocomotive loco && loco.FuelCar != null)
			{
				fuelCar = loco.FuelCar();
			}
			else if (_car is DieselLocomotive)
			{
				fuelCar = _car;
			}
			else
			{
				return;
			}

			int count = fuelCar.Definition.LoadSlots.Count;
			var loadString = "<size=14>";
			for (int i = 0; i < count; i++)
			{
				CarLoadInfo? loadInfo = fuelCar.GetLoadInfo(i);
				if (loadInfo != null)
				{
					CarLoadInfo value = loadInfo.Value;
					Load load = CarPrototypeLibrary.instance.LoadForId(value.LoadId);
					if (load != null)
					{
						var perc = value.Quantity / fuelCar.Definition.LoadSlots[i].MaximumCapacity;
						var colorStart = "";
						var colorEnd = "</color>";
						if (perc < 0.2f)
							colorStart = "<color=red>";
						else if (perc < 0.3f)
							colorStart = "<color=yellow>";

						loadString += $", {colorStart}{value.LoadString(load)}{colorEnd}";
					}
				}
			}
			loadString += "</size>";
			___locomotiveNameLabel.text += loadString;
			
		}
	}
}
