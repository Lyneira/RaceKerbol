/*
Copyright 2015 Lyneira

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace LandDistance {
	/**
	 * Measures the distance traveled over land this session and shows it in a small window.
	 * 
	 * Author: Lyneira
	 * License: GPLv3
	 */

	// Needed to run a partless plugin
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class LandDistance : MonoBehaviour {
		// Logging
		private float lastFixedUpdate = 0.0f;
		private float logInterval = 5.0f;

		// Tracking
		private bool landed;
		private double longitude;
		private double latitude;
		private double altitude;
		private double landDistance;
		private double highScore;
		private double targetDistance = 0.0d;
		private Vessel target = null;

		// UI
		private static ApplicationLauncherButton stockToolbarButton = null;
		private static IButton blizzyToolbarButton = null;
		private static Rect windowRect;
		private static bool windowVisible;
		private static string buttonTexturePath = "RaceKerbol/LD";

		// ***** Overrides *****
		// These get called implicitly by the ksp engine

		/*
         * Override: Called after the scene is loaded.
         */
		public void Awake() {
			// Remember settings
			loadSettings();

			if (ToolbarManager.ToolbarAvailable) {
				setupBlizzyToolbarButton();
			} else {
				setupStockToolbarButton();
			}
		}

		/**
		 * Override: Called after Awake.
		 */
		public void Start() {
			GameEvents.onVesselChange.Add(this.onVesselChange);
			landDistance = 0.0d;
			highScore = 0.0d;
			landed = false;
			if (windowVisible) {
				// Run our toggle method to activate window
				windowVisible = false;
				onButtonToggle();
			}
		}

		/**
		 * Override: Called at a fixed time interval determined by the physics time step.
		 */
		public void FixedUpdate() {
			if ((Time.time - lastFixedUpdate) > logInterval) {
				lastFixedUpdate = Time.time;
				/* Debug
				Debug.Log("LandDistance [" + this.GetInstanceID().ToString("X")	+ "][" + Time.time.ToString("0.0000") +
					"]: FixedUpdate - Position: [" + longitude.ToString() + ", " + latitude.ToString() + ", " + altitude.ToString() +
					"] - Landed: " + landed.ToString() + " - LandDistance: " + landDistance.ToString() + " - High Score: " + highScore);
				*/
			}
			updateState();
		}

		/**
		 * Override: Called when the game is leaving the scene (or exiting). Perform any clean up work here.
		 */
		public void OnDestroy() {
			// Save settings
			saveSettings();

			GameEvents.onVesselChange.Remove(this.onVesselChange);

			if (ToolbarManager.ToolbarAvailable) {
				removeBlizzyToolbarButton();
			} else {
				removeStockToolbarButton();
			}
		}

		// ***** Tracking *****

		/**
		 * Tracks distance covered while landed and updates stored parameters with vessel's new position.
		 */
		private void updateState() {
			Vessel v = FlightGlobals.ActiveVessel;
			if (landed && v.Landed) {
				// Only add to the total if the vessel is landed and was also landed in the previous frame
				double averageAltitude = altitude + (v.altitude - altitude) / 2.0d;
				landDistance += haversine(latitude, longitude, averageAltitude, v);

				// Update highscore if needed
				if (landDistance > highScore) {
					highScore = landDistance;
				}
			}
			if (windowVisible) {
				if (v.targetObject is Vessel) {
					// Calculate distance to target vessel if one is selected
					target = (Vessel) v.targetObject;
					if (target.mainBody == v.mainBody) {
						targetDistance = haversine(v.latitude, v.longitude, 0.0d, target);
					} else {
						target = null;
					}
				} else {
					target = null;
				}
			}

			// Update stuff for next distance calc
			landed = v.Landed;
			longitude = v.longitude;
			latitude = v.latitude;
			altitude = v.altitude;
		}

		/**
		 * Callback: resets total, keep highscore
		 */
		private void onVesselChange(Vessel v) {
			if (landDistance > highScore) {
				highScore = landDistance;
			}
			landDistance = 0.0d;
			landed = false;
		}

		private static double toRadians(double x) {
			return Math.PI * x / 180.0d;
		}

		/**
		 *  Uses the "haversine" formula to calculate great-circle distance to v from the given coordinates and altitude.
		 */
		private static double haversine(double latitude, double longitude, double altitude, Vessel v) {
			double radius = v.mainBody.Radius + altitude;
			double la1 = toRadians(latitude);
			double la2 = toRadians(v.latitude);
			double dLa = toRadians(v.latitude - latitude);
			double dLo = toRadians(v.longitude - longitude);

			double a = Math.Pow(Math.Sin(dLa / 2.0d), 2) +
				Math.Cos(la1) * Math.Cos(la2) *
				Math.Pow(Math.Sin(dLo / 2.0d), 2);
			double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
			return radius * c;
		}

		// ***** GUI *****

		/**
		 * Called in Awake
		 */
		private void setupStockToolbarButton() {
			if (ApplicationLauncher.Ready) {
				OnGUIAppLauncherReady();
			} else {
				GameEvents.onGUIApplicationLauncherReady.Add(OnGUIAppLauncherReady);
			}
			GameEvents.onGUIApplicationLauncherDestroyed.Add(OnGUIAppLauncherDestroyed);
		}

		/**
		 * Called in OnDestroy
		 */
		private void removeStockToolbarButton() {
			GameEvents.onGUIApplicationLauncherReady.Remove(OnGUIAppLauncherReady);
			GameEvents.onGUIApplicationLauncherReady.Remove(OnGUIAppLauncherDestroyed);
			OnGUIAppLauncherDestroyed();
		}

		/**
		 * Callback: Adds button to the toolbar
		 */
		private void OnGUIAppLauncherReady() {
			stockToolbarButton = ApplicationLauncher.Instance.AddModApplication(
				onButtonToggle,
				onButtonToggle,
				null, null,
				null, null,
				ApplicationLauncher.AppScenes.FLIGHT,
				GameDatabase.Instance.GetTexture(buttonTexturePath, false));
		}

		/**
		 * Callback: Removes button from the toolbar
		 */
		private void OnGUIAppLauncherDestroyed() {
			if (stockToolbarButton != null) {
				ApplicationLauncher.Instance.RemoveModApplication(stockToolbarButton);
				stockToolbarButton = null;
			}
		}

		private void setupBlizzyToolbarButton() {
			if (blizzyToolbarButton == null) {
				blizzyToolbarButton = ToolbarManager.Instance.add("LandDistance", "LandDistanceButton");
				blizzyToolbarButton.TexturePath = buttonTexturePath;
				blizzyToolbarButton.ToolTip = "Land Distance";
				blizzyToolbarButton.Visibility = new GameScenesVisibility(GameScenes.FLIGHT);
				blizzyToolbarButton.OnClick += (e) => onButtonToggle();
			}
		}

		private void removeBlizzyToolbarButton() {
			blizzyToolbarButton.Destroy();
			blizzyToolbarButton = null;
		}

		/**
		 * Callback: 
		 */
		private void onButtonToggle() {
			windowVisible = !windowVisible;
			if (windowVisible) {
				RenderingManager.AddToPostDrawQueue(0, onDraw);
			} else {
				RenderingManager.RemoveFromPostDrawQueue(0, onDraw);
			}
		}

		private void onDraw() {
			GUI.skin = HighLogic.Skin;
			windowRect = GUILayout.Window(this.GetHashCode(), windowRect, onWindow, "Land Distance", GUILayout.MinWidth(320));
			ClampToScreen(windowRect);
		}

		/**
		 * Draw our window 
		 */
		private void onWindow(int windowId) {
			GUILayout.BeginVertical();

			GUILayout.Label("Tracks the distance traveled while on the ground.");

			GUILayout.BeginHorizontal();
			GUILayout.Label("Land Distance:", GUILayout.ExpandWidth(true));
			GUILayout.Label(distanceReadable(landDistance));
			GUILayout.EndHorizontal();

			if (landDistance < highScore) {
				GUILayout.BeginHorizontal();
				GUILayout.Label("Session high score:", GUILayout.ExpandWidth(true));
				GUILayout.Label(distanceReadable(highScore));
				GUILayout.EndHorizontal();
			}

			if (target != null) {
				GUILayout.BeginHorizontal();
				GUILayout.Label("Surface distance to " + target.RevealName() + ":", GUILayout.ExpandWidth(true));
				GUILayout.Label(distanceReadable(targetDistance));
				GUILayout.EndHorizontal();
			}

			GUILayout.EndVertical();

			GUI.DragWindow();
		}

		/**
		 * Show nicely formatted distance reading
		 */
		private static string distanceReadable(double distance) {
			string result;
			string suffix;
			if (distance > 10000) {
				result = (distance / 1000d).ToString("F3");
				suffix = " km";
			} else {
				result = distance.ToString("F0");
				suffix = " m";
			}
			return result + suffix;
		}

		private void loadSettings() {
			KSP.IO.PluginConfiguration config = KSP.IO.PluginConfiguration.CreateForType<LandDistance>();
			config.load();
			windowRect = config.GetValue("windowRect", new Rect());
			windowVisible = config.GetValue("windowVisible", false);
		}

		private void saveSettings() {
			KSP.IO.PluginConfiguration config = KSP.IO.PluginConfiguration.CreateForType<LandDistance>();
			config.SetValue("windowRect", windowRect);
			config.SetValue("windowVisible", windowVisible);
			config.save();
		}

		public static void ClampToScreen(Rect window) {
			window.x = Mathf.Clamp(window.x, -window.width + 20, Screen.width - 20);
			window.y = Mathf.Clamp(window.y, -window.height + 20, Screen.height - 20);
		}
	}
}

