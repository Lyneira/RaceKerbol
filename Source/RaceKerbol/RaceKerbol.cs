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

namespace RaceKerbol {
	/**
	 * Tracks distance traveled over land this session, calculates score
	 * and shows score and other useful data for Race Kerbol in a small window.
	 * 
	 * Author: Lyneira
	 * License: GPLv3
	 */

	// Needed to run a partless plugin
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class RaceKerbol : MonoBehaviour {
		// Tracking
		private bool landed;
		private double lastFrameDistance;

		private double landDistance;
		private double score;
		private double scoreMultiplier = 1.0d;
		private double highScore;
		private double targetDistance = 0.0d;
		private Vessel target = null;

		// UI
		private static ApplicationLauncherButton stockToolbarButton = null;
		private static IButton blizzyToolbarButton = null;
		private static Rect windowRect;
		private static bool windowVisible;
		private static string buttonTexturePath = "RaceKerbol/RK";
		private static GUIStyle textNormal, textMultiplierActive;

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
			score = 0.0d;
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
			Vessel v = FlightGlobals.ActiveVessel;
			scoreMultiplier = 1.0d;
			if (v.Landed) {
				Vector3d vel = v.srf_velocity;
				if (landed) {
					// Only add to the total if the vessel is landed and was also landed in the previous frame
					landDistance += lastFrameDistance;
					double lastFrameScore = lastFrameDistance;

					// Add a bonus for significant slopes.
					RaycastHit raycastHit;
					if (Physics.Raycast (v.vesselTransform.position, -FlightGlobals.getUpAxis (v.mainBody, v.vesselTransform.position), out raycastHit, (float)FlightGlobals.getAltitudeAtPos (v.vesselTransform.position, v.mainBody) + 600f, 32768))
					{
						scoreMultiplier = sigmoidScoreMultiplier(Vector3d.Angle(raycastHit.normal, v.mainBody.GetSurfaceNVector(v.latitude, v.longitude)));
						lastFrameScore *= scoreMultiplier;
					}

					// Update score and highscore if needed
					score += lastFrameScore;
					if (score > highScore) {
						highScore = score;
					}
				}
				lastFrameDistance = Math.Sqrt(vel.x * vel.x + vel.y * vel.y + vel.z * vel.z) * TimeWarp.fixedDeltaTime;
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

			// Update for next distance calc
			landed = v.Landed;
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
		 * Callback: resets total, keep highscore
		 */
		private void onVesselChange(Vessel v) {
			if (score > highScore) {
				highScore = score;
			}
			landDistance = 0.0d;
			score = 0.0d;
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

		private static double sigmoidScoreMultiplier(double angle) {
			double cutoff = 15.0d;
			if (angle <= cutoff) {
				return 1.0d;
			}
			double startMultiplier = 1.0d; // Score multiplier starts here at the cutoff
			double maxMultiplierBonus = 1.0d; // Asymptote
			double halfwayPoint = 30.0d; // Angle above cutoff at which startMultiplier is increased by half of maxMultiplierBonus
			double y = (angle - cutoff) / halfwayPoint;

			return startMultiplier + maxMultiplierBonus * y / (1.0d + y);
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
				blizzyToolbarButton = ToolbarManager.Instance.add("RaceKerbol", "RaceKerbolButton");
				blizzyToolbarButton.TexturePath = buttonTexturePath;
				blizzyToolbarButton.ToolTip = "Race Kerbol";
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
			windowRect = GUILayout.Window(this.GetHashCode(), windowRect, onWindow, "Race Kerbol", GUILayout.MinWidth(320));
			ClampToScreen(windowRect);
		}

		/**
		 * Draw our window 
		 */
		private void onWindow(int windowId) {
			if (textNormal == null) {
				textNormal = GUI.skin.label;
				textMultiplierActive = new GUIStyle(textNormal);
				textMultiplierActive.normal.textColor = Color.cyan;
				textMultiplierActive.fontStyle = FontStyle.Bold;
			}
			GUIStyle scoreStyle = textNormal;
			if (scoreMultiplier > 1.0d) {
				scoreStyle = textMultiplierActive;
			}

			GUILayout.BeginVertical();

			GUILayout.Label("Gain score by traveling on the ground. You get a bonus for traveling on steep slopes.");

			GUILayout.BeginHorizontal();
			GUILayout.Label("Score:", GUILayout.ExpandWidth(true));
			GUILayout.Label(score.ToString("F0"), scoreStyle);
			GUILayout.EndHorizontal();

			double scoreMultiplierPercent = scoreMultiplier * 100;
			GUILayout.BeginHorizontal();
			GUILayout.Label("Multiplier:", GUILayout.ExpandWidth(true));
			GUILayout.Label(scoreMultiplierPercent.ToString("F0") + " %", scoreStyle);
			GUILayout.EndHorizontal();

			if (score < highScore) {
				GUILayout.BeginHorizontal();
				GUILayout.Label("Session high score:", GUILayout.ExpandWidth(true));
				GUILayout.Label(highScore.ToString("F0"));
				GUILayout.EndHorizontal();
			}

			if (target != null) {
				GUILayout.BeginHorizontal();
				GUILayout.Label("Surface distance to " + target.RevealName() + ":", GUILayout.ExpandWidth(true));
				GUILayout.Label(distanceReadable(targetDistance));
				GUILayout.EndHorizontal();
			}

			// Debug
			/*
			GUILayout.BeginHorizontal();
			GUILayout.Label("Land Distance:", GUILayout.ExpandWidth(true));
			GUILayout.Label(distanceReadable(landDistance));
			GUILayout.EndHorizontal();

			Vessel v = FlightGlobals.ActiveVessel;
			Vector3d vel = v.srf_velocity;

			GUILayout.BeginHorizontal();
			GUILayout.Label("Surface Velocity:", GUILayout.ExpandWidth(true));
			GUILayout.Label(vel.ToString());
			GUILayout.EndHorizontal();

			RaycastHit raycastHit;
			if (Physics.Raycast (v.vesselTransform.position, -FlightGlobals.getUpAxis (v.mainBody, v.vesselTransform.position), out raycastHit, (float)FlightGlobals.getAltitudeAtPos (v.vesselTransform.position, v.mainBody) + 600f, 32768))
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label("Terrain Normal:", GUILayout.ExpandWidth(true));
				GUILayout.Label(raycastHit.normal.ToString());
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				GUILayout.Label("Terrain Slope:", GUILayout.ExpandWidth(true));
				GUILayout.Label(Vector3d.Angle(raycastHit.normal, v.mainBody.GetSurfaceNVector(v.latitude, v.longitude)).ToString("F3"));
				GUILayout.EndHorizontal();
			}
			*/

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
			KSP.IO.PluginConfiguration config = KSP.IO.PluginConfiguration.CreateForType<RaceKerbol>();
			config.load();
			windowRect = config.GetValue("windowRect", new Rect());
			windowVisible = config.GetValue("windowVisible", false);
		}

		private void saveSettings() {
			KSP.IO.PluginConfiguration config = KSP.IO.PluginConfiguration.CreateForType<RaceKerbol>();
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

