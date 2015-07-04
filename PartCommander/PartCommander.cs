﻿// 
//     Part Commander
// 
//     Copyright (C) 2015 Sean McDougall
// 
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
// 
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see <http://www.gnu.org/licenses/>.
// 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using HighlightingSystem;

namespace PartCommander
{

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class PartCommander : MonoBehaviour
    {
        internal ApplicationLauncherButton launcherButton = null;

        private List<Part> activeParts = new List<Part>();
        private string partFilter = "";
        private bool updateParts = true;

        private PCWindow currentWindow;

        private bool visibleUI = true;

        private bool controlsLocked = false;
        private string controlsLockID = "PartCommander_LockID";

        private bool popOut = false;

        private ModStyle modStyle;

        private string showTooltip = "";

        public static PartCommander Instance { get; private set; }
        public PartCommander()
        {
            Instance = this;
        }

        // ------------------------------- Main Events --------------------------------
        public void Awake()
        {
            modStyle = new ModStyle();

            // Hook into events for Application Launcher
            GameEvents.onGUIApplicationLauncherReady.Add(OnGUIApplicationLauncherReady);
            GameEvents.onGameSceneLoadRequested.Add(onSceneChange);
        }

        public void Start()
        {
            // Add hooks for showing/hiding on F2
            GameEvents.onShowUI.Add(showUI);
            GameEvents.onHideUI.Add(hideUI);

            // Load Application Launcher
            if (launcherButton == null)
            {
                OnGUIApplicationLauncherReady();
            }

            // Add hooks for updating part list when needed
            GameEvents.onVesselWasModified.Add(triggerUpdateParts);
            GameEvents.onVesselChange.Add(triggerUpdateParts);
        }

        public void triggerUpdateParts(Vessel v)
        {
            updateParts = true;
        }

        public void Update()
        {
            // Only proceed if a vessel is active, physics have stablized, and window is visible
            if (FlightGlobals.ActiveVessel != null && FlightGlobals.ActiveVessel.HoldPhysics == false && PCScenario.Instance != null && visibleUI && PCScenario.Instance.gameSettings.visibleWindow)
            {
                // Check to see if we already have a saved window, if not then create a new one
                if (!PCScenario.Instance.gameSettings.vesselWindows.ContainsKey(FlightGlobals.ActiveVessel.id))
                {
                    PCScenario.Instance.gameSettings.vesselWindows.Add(FlightGlobals.ActiveVessel.id, new PCWindow(false));
                }
                // Load the saved window
                currentWindow = PCScenario.Instance.gameSettings.vesselWindows[FlightGlobals.ActiveVessel.id];
                // If we don't have a selected part but we do have an id, then resurrect it
                if (currentWindow.currentPart == null && currentWindow.currentPartId != 0u)
                {
                    foreach (Part p in FlightGlobals.ActiveVessel.Parts)
                    {
                        if (p.flightID == currentWindow.currentPartId)
                        {
                            currentWindow.currentPart = p;
                            break;
                        }
                    }
                    // If we still don't have a part, then the id must be invalid or the part is gone.  Clear it out.
                    if (currentWindow.currentPart == null)
                    {
                        currentWindow.currentPartId = 0u;
                    }
                }

                // Load any popout windows
                foreach (PCWindow pow in currentWindow.partWindows.Values)
                {
                    // Resurrect the part if necessary
                    if (pow.currentPart == null & pow.currentPartId != 0u)
                    {
                        foreach (Part p in FlightGlobals.ActiveVessel.Parts)
                        {
                            if (p.flightID == pow.currentPartId)
                            {
                                pow.currentPart = p;
                                break;
                            }
                        }
                        // If we still don't have a part, then the id must be invalid or the part is gone.  Clear it out.
                        if (pow.currentPart == null)
                        {
                            currentWindow.partWindows.Remove(pow.windowId);
                        }
                    }
                }
                // If a new popout window was requested, then create it
                if (popOut)
                {
                    if (currentWindow.currentPart != null)
                    {
                        PCWindow pow = new PCWindow((Screen.width - currentWindow.windowRect.width) / 2, (Screen.height - currentWindow.windowRect.height) / 2, currentWindow.windowRect.width, currentWindow.windowRect.height, true);
                        pow.currentPart = currentWindow.currentPart;
                        pow.currentPartId = currentWindow.currentPartId;
                        pow.symLock = currentWindow.symLock;
                        pow.showAero = currentWindow.showAero;
                        pow.showTemp = currentWindow.showTemp;
                        currentWindow.partWindows.Add(pow.windowId, pow);
                    }
                    popOut = false;
                }

                // The part selector button was clicked in the gui
                if (currentWindow.togglePartSelector)
                {
                    // toggle part selector
                    currentWindow.showPartSelector = !currentWindow.showPartSelector;
                    if (currentWindow.showPartSelector)
                    {
                        // Showing part selector now... clear out any selected part info.
                        currentWindow.currentPart = null;
                        currentWindow.currentPartId = 0u;

                        // Restore old scroll position
                        currentWindow.scrollPos = currentWindow.oldScrollPos;
                    }
                    else
                    {
                        // Should now have a selected part, but make sure it's not null and turn the part selector back on if it is.
                        if (currentWindow.currentPart == null)
                        {
                            currentWindow.showPartSelector = true;
                        }
                    }
                    currentWindow.togglePartSelector = false;
                    updateParts = true;
                }

                // Make sure the selected part still exists and is part of the active vessel, otherwise clear it out and reenable the part selector.
                if (currentWindow.currentPart == null)
                {
                    currentWindow.showPartSelector = true;
                    currentWindow.currentPartId = 0u;
                }
                else
                {
                    if (currentWindow.currentPart.vessel != FlightGlobals.ActiveVessel)
                    {
                        currentWindow.currentPart = null;
                        currentWindow.currentPartId = 0u;
                        currentWindow.showPartSelector = true;
                        setHighlighting(currentWindow.currentPart, currentWindow.symLock, false);
                    }
                }

                resizeWindows();
                windowHover();
                if (updateParts)
                {
                    clearHighlighting(activeParts);
                    getActiveParts();
                }


                // If there's only one available part on the vessel, select it automatically.
                if (currentWindow.showPartSelector && activeParts.Count == 1 && partFilter == "")
                {
                    currentWindow.selectPart = activeParts.First();
                }

                // A part was selected in the gui
                if (currentWindow.selectPart != null)
                {
                    if (currentWindow.selectPart.vessel == FlightGlobals.ActiveVessel)
                    {
                        currentWindow.currentPart = currentWindow.selectPart;
                        currentWindow.currentPartId = currentWindow.selectPart.flightID;
                        currentWindow.showPartSelector = false;
                        partFilter = "";
                        // Save old scroll position
                        currentWindow.oldScrollPos = currentWindow.scrollPos;
                        currentWindow.scrollPos.x = currentWindow.scrollPos.y = 0;
                    }
                    currentWindow.selectPart = null;
                }

            }
        }

        public void OnGUI()
        {
            // Make sure we have something to show
            if (visibleUI && FlightGlobals.ActiveVessel != null && currentWindow != null && PCScenario.Instance != null && PCScenario.Instance.gameSettings.visibleWindow)
            {
                GUI.skin = modStyle.skin;
                currentWindow.windowRect = GUILayout.Window(currentWindow.windowId, currentWindow.windowRect, mainWindow, "");
                // Set the default location/size for new windows to be the same as this one
                PCScenario.Instance.gameSettings.windowDefaultRect = currentWindow.windowRect;

                // Process any popout windows
                foreach (PCWindow pow in currentWindow.partWindows.Values)
                {
                    pow.windowRect = GUILayout.Window(pow.windowId, pow.windowRect, partWindow, "");
                }
                if (showTooltip != "" && showTooltip != null)
                {
                    GUI.Label(new Rect(Input.mousePosition.x + 10, Screen.height - Input.mousePosition.y + 20, showTooltip.Length * 10, 20), showTooltip, modStyle.guiStyles["tooltip"]);
                    GUI.depth = 0;
                }

            }
        }

        // Remove the launcher button when the scene changes
        public void onSceneChange(GameScenes scene)
        {
            removeLauncherButton();
        }

        // Cleanup when the module is destroyed
        protected void OnDestroy()
        {
            PCScenario.Instance.gameSettings.visibleWindow = false;
            GameEvents.onGUIApplicationLauncherReady.Remove(OnGUIApplicationLauncherReady);
            GameEvents.onGameSceneLoadRequested.Remove(onSceneChange);
            removeLauncherButton();

            if (InputLockManager.lockStack.ContainsKey(controlsLockID))
            {
                InputLockManager.RemoveControlLock(controlsLockID);
            }

        }

        // ------------------------------------------ Application Launcher / UI ---------------------------------------
        private void OnGUIApplicationLauncherReady()
        {
            if (launcherButton == null)
            {
                launcherButton = ApplicationLauncher.Instance.AddModApplication(showWindow, hideWindow, null, null, null, null, ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.MAPVIEW, modStyle.GetImage("PartCommander/textures/toolbar", 38, 38));
            }
        }

        public void removeLauncherButton()
        {
            if (launcherButton != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(launcherButton);
            }
        }

        public void showUI() // triggered on F2
        {
            visibleUI = true;
        }

        public void hideUI() // triggered on F2
        {
            visibleUI = false;
        }

        public void showWindow()  // triggered by toolbar
        {
            PCScenario.Instance.gameSettings.visibleWindow = true;
        }

        public void hideWindow() // triggered by toolbar
        {
            PCScenario.Instance.gameSettings.visibleWindow = false;
        }

        private void resizeWindows()
        {
            // Resize main window
            resizeWindow(currentWindow);

            // Resize popout windows
            foreach (PCWindow pow in currentWindow.partWindows.Values)
            {
                resizeWindow(pow);
            }
        }

        private void resizeWindow(PCWindow w)
        {
            if (Input.GetMouseButtonUp(0))
            {
                w.resizingWindow = false;
            }

            if (w.resizingWindow)
            {
                w.windowRect.width = Input.mousePosition.x - w.windowRect.x + 10;
                w.windowRect.width = Mathf.Clamp(w.windowRect.width, modStyle.minWidth, Screen.width);
                w.windowRect.height = (Screen.height - Input.mousePosition.y) - w.windowRect.y + 10;
                w.windowRect.height = Mathf.Clamp(w.windowRect.height, modStyle.minHeight, Screen.height);
            }
        }

        private void windowHover()
        {
            // Lock camera controls and highlight active part when over window
            Vector2 mousePos = Input.mousePosition;
            mousePos.y = Screen.height - mousePos.y;

            bool overWindow = false;
            Part overPart = null;
            bool overSymLock = true;

            if (currentWindow.windowRect.Contains(mousePos))
            {
                overWindow = true;
                if (currentWindow.showPartSelector == false && currentWindow.currentPart != null)
                {
                    overPart = currentWindow.currentPart;
                    overSymLock = currentWindow.symLock;
                }

            }
            else
            {
                foreach (PCWindow pow in currentWindow.partWindows.Values)
                {
                    if (pow.windowRect.Contains(mousePos))
                    {
                        overWindow = true;
                        overPart = pow.currentPart;
                        overSymLock = pow.symLock;
                    }
                }
            }

            if (controlsLocked)
            {
                if (visibleUI && PCScenario.Instance.gameSettings.visibleWindow && overWindow)
                {
                    if (overPart != null)
                    {
                        setHighlighting(overPart, overSymLock, true);
                    }
                }
                else
                {
                    InputLockManager.RemoveControlLock(controlsLockID);
                    controlsLocked = false;
                    clearHighlighting(activeParts);
                    if (overPart != null)
                    {
                        GameEvents.onPartActionUIDismiss.Fire(overPart);
                    }

                }
            }
            else
            {
                if (visibleUI && PCScenario.Instance.gameSettings.visibleWindow && overWindow)
                {
                    InputLockManager.SetControlLock(ControlTypes.CAMERACONTROLS, controlsLockID);
                    controlsLocked = true;

                    if (overPart != null)
                    {
                        setHighlighting(overPart, overSymLock, true);
                        GameEvents.onPartActionUICreate.Fire(overPart);
                    }
                }
                else
                {
                    clearHighlighting(activeParts);
                }
            }
        }

        // ----------------------------------- Main Window Logic ---------------------------
        public void mainWindow(int id)
        {
            drawWindow(currentWindow);
        }

        public void partWindow(int id)
        {
            PCWindow currentPOW = currentWindow.partWindows[id];
            if (currentPOW.currentPart == null)
            {
                currentWindow.partWindows.Remove(id);
                return;
            }
            drawWindow(currentPOW);
        }

        public void drawWindow(PCWindow w)
        {
            int optionsCount = 0;

            GUILayout.BeginVertical();
            if (w.popOutWindow)
            {
                string partTitle = (w.symLock && w.currentPart.symmetryCounterparts.Count() > 0) ? w.currentPart.partInfo.title + " (x" + (w.currentPart.symmetryCounterparts.Count() + 1) + ")" : w.currentPart.partInfo.title;
                GUILayout.Label(partTitle, modStyle.guiStyles["centeredLabel"]);
            }
            else
            {
                GUILayout.Label(FlightGlobals.ActiveVessel.vesselName, modStyle.guiStyles["centeredLabel"]);
            }
            GUILayout.EndVertical();
            if (Event.current.type == EventType.Repaint)
            {
                w.dragRect = GUILayoutUtility.GetLastRect();
            }
            GUILayout.BeginVertical();

            if (w.currentPart != null && w.popOutWindow == false)
            {
                string partSelectorLabel = (w.symLock && w.currentPart.symmetryCounterparts.Count() > 0) ? w.currentPart.partInfo.title + " (x" + (w.currentPart.symmetryCounterparts.Count() + 1) + ")" : w.currentPart.partInfo.title;
                // Part selector
                if (GUILayout.Button(partSelectorLabel))
                {
                    w.togglePartSelector = true;
                }
            }
            // Main area
            w.scrollPos = GUILayout.BeginScrollView(w.scrollPos);

            if (w.showPartSelector && w.popOutWindow == false)
            {
                optionsCount = showParts();
            }
            else
            {
                optionsCount = showOptions(w.currentPart, w.symLock, w.showResources, w.showTemp, w.showAero);
            }

            if (optionsCount == 0)
            {
                GUILayout.Label("Nothing to display.");
            }

            GUILayout.EndScrollView();
            GUILayout.Space(5f);
            if (w.currentPart == null && w.popOutWindow == false && w.search)
            {
                GUILayout.BeginHorizontal();
                string newPartFilter = GUILayout.TextField(partFilter);
                if (newPartFilter != partFilter)
                {
                    partFilter = newPartFilter;
                    updateParts = true;
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(5f);
            }
            else
            {
                partFilter = "";
            }

            GUILayout.BeginHorizontal();
            GUILayout.Space(2f);
            showSettings(w);
            GUILayout.EndHorizontal();
            GUILayout.Space(3f);
            GUILayout.EndVertical();

            // Create part popout button in upper left corner
            if (w.currentPart != null && activeParts.Count > 0 && w.popOutWindow == false)
            {
                if (GUI.Button(new Rect(7, 3, 20, 20), new GUIContent("", "Pop off in new window"), modStyle.guiStyles["popoutButton"]))
                {
                    w.togglePartSelector = true;
                    popOut = true;
                }
            }

            if (w.popOutWindow)
            {
                // Create close button in upper right corner
                if (GUI.Button(new Rect(w.windowRect.width - 18, 3f, 15f, 15f), new GUIContent("", "Close"), modStyle.guiStyles["closeButton"]))
                {
                    currentWindow.partWindows.Remove(w.windowId);
                }
            }

            // Create resize button in bottom right corner
            if (GUI.RepeatButton(new Rect(w.windowRect.width - 23, w.windowRect.height - 23, 20, 20), new GUIContent("", "Click and drag to resize"), modStyle.guiStyles["resizeButton"]))
            {
                w.resizingWindow = true;
            }

            if (Event.current.type == EventType.Repaint)
            {
                if (GUI.tooltip != null && GUI.tooltip != "")
                {
                    // Filter out the tooltips that come from the part-list hover hack
                    uint testIt;
                    if (uint.TryParse(GUI.tooltip, out testIt))
                    {
                        showTooltip = "";
                    }
                    else
                    {
                        showTooltip = GUI.tooltip;
                    }
                    
                }
                else
                {
                    showTooltip = "";
                }

            }


            // Make window draggable by title
            //GUI.DragWindow(new Rect(0, 0, 10000, 100));
            GUI.DragWindow(w.dragRect);
        }

        // ----------------------------------- Part Selector -------------------------------
        private void getActiveParts()
        {
            // Build list of active parts
            activeParts.Clear();
            List<Part> hiddenParts = new List<Part>();
            foreach (Part p in FlightGlobals.ActiveVessel.Parts)
            {
                bool includePart = false;
                if (!hiddenParts.Contains(p))
                {
                    // Hide other members of the symmetry
                    if (currentWindow.symLock)
                    {
                        foreach (Part symPart in p.symmetryCounterparts)
                        {
                            hiddenParts.Add(symPart);
                        }
                    }
                    foreach (PartModule pm in p.Modules)
                    {
                        if (includePart)
                        {
                            // Part was already included, so break out
                            break;
                        }
                        else
                        {
                            if (pm.Fields != null || pm.Events != null)
                            {
                                foreach (BaseField f in pm.Fields)
                                {
                                    if (f.guiActive && f.guiName != "")
                                    {
                                        includePart = true;
                                        break;
                                    }
                                }
                                if (!includePart)
                                {
                                    foreach (BaseEvent e in pm.Events)
                                    {
                                        if (e.guiActive && e.active)
                                        {
                                            includePart = true;
                                            break;
                                        }
                                    }
                                }
                                if (includePart)
                                {
                                    activeParts.Add(p);
                                }
                            }
                        }
                    }
                }
            }
            if (partFilter != "")
            {
                activeParts = activeParts.FindAll(partMatch);
            }
            if (currentWindow.alphaSort)
            {
                activeParts = activeParts.OrderBy(o => o.partInfo.title).ToList();
            }
            updateParts = false;
        }

        private bool partMatch(Part p)
        {
            if (p.partInfo.title.Contains(partFilter, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private int showParts()
        {
            GUILayout.Space(10f);

            foreach (Part p in activeParts)
            {
                string partTitle = (currentWindow.symLock && p.symmetryCounterparts.Count() > 0) ? p.partInfo.title + " (x" + (p.symmetryCounterparts.Count() + 1) + ")" : p.partInfo.title;
                if (GUILayout.Button(new GUIContent(partTitle, p.flightID.ToString())))
                {
                    currentWindow.selectPart = p;
                }
                if (controlsLocked)
                {
                    if (Event.current.type == EventType.Repaint)
                    {
                        if (GUI.tooltip == p.flightID.ToString())
                        {
                            setHighlighting(p, currentWindow.symLock, true);
                        }
                        else
                        {
                            setHighlighting(p, currentWindow.symLock, false);
                        }
                    }
                }
            }

            return activeParts.Count();
        }

        // ----------------------------------- Selected Part Logic -------------------------

        private int showOptions(Part p, bool symLock, bool showRes, bool showTemp, bool showAero)
        {
            int optionsCount = 0;
            string multiEngineMode = getEngineMode(p);
            optionsCount += showFields(p, symLock, multiEngineMode);
            optionsCount += showEvents(p, symLock, multiEngineMode);
            if (showRes)
            {
                optionsCount += showResources(p);
            }
            if (showTemp)
            {
                optionsCount += showTemperatureInfo(p);
            }
            if (showAero)
            {
                optionsCount += showAeroInfo(p);
            }
            return (optionsCount);
        }

        // Routines for displaying/setting KSPFields
        private int showFields(Part p, bool symLock, string multiEngineMode)
        {
            int fieldCount = 0;
            foreach (PartModule pm in p.Modules)
            {
                if (pm.Fields != null)
                {
                    if (checkEngineMode(multiEngineMode, pm))
                    {
                        foreach (BaseField f in pm.Fields)
                        {
                            if (f.guiActive && f.guiName != "")
                            {
                                fieldCount++;

                                if (f.uiControlFlight.GetType().ToString() == "UI_Toggle")
                                {
                                    showToggleField(p, symLock, pm, f, multiEngineMode);
                                }
                                else if (f.uiControlFlight.GetType().ToString() == "UI_FloatRange")
                                {
                                    showSliderField(p, symLock, pm, f, multiEngineMode);

                                }
                                else
                                {
                                    GUILayout.Label(f.GuiString(f.host));
                                }
                            }
                        }
                    }
                }
            }
            return fieldCount;
        }

        private void showSliderField(Part p, bool symLock, PartModule pm, BaseField f, string multiEngineMode)
        {
            UI_FloatRange fr = (UI_FloatRange)f.uiControlFlight;
            GUILayout.Label(f.GuiString(f.host));
            float curVal = (float)f.GetValue(f.host);
            curVal = Mathf.Clamp(curVal, fr.minValue, fr.maxValue);
            curVal = GUILayout.HorizontalSlider(curVal, fr.minValue, fr.maxValue);
            GUILayout.Space(10f);
            curVal = Mathf.CeilToInt(curVal / fr.stepIncrement) * fr.stepIncrement;
            setPartModuleFieldValue(p, symLock, pm, f, multiEngineMode, curVal);
        }

        private void showToggleField(Part p, bool symLock, PartModule pm, BaseField f, string multiEngineMode)
        {
            UI_Toggle t = (UI_Toggle)f.uiControlFlight;
            bool curVal = (bool)f.GetValue(f.host);
            string curText = curVal ? t.enabledText : t.disabledText;

            if (GUILayout.Button(f.guiName + ": " + curText))
            {
                curVal = !curVal;
                setPartModuleFieldValue(p, symLock, pm, f, multiEngineMode, curVal);
            }

        }

        private void setPartModuleFieldValue<T>(Part p, bool symLock, PartModule pm, BaseField f, string multiEngineMode, T curVal)
        {
            f.SetValue(curVal, f.host);
            if (symLock)
            {
                foreach (Part symPart in p.symmetryCounterparts)
                {
                    foreach (PartModule symPM in symPart.Modules)
                    {
                        if (symPM.GetType() == pm.GetType())
                        {
                            if (checkEngineMode(multiEngineMode, symPM))
                            {
                                foreach (BaseField symF in symPM.Fields)
                                {
                                    if (symF.guiActive && f.name == symF.name)
                                    {
                                        symF.SetValue(curVal, symF.host);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        // Routines for displaying KSPEvents
        private int showEvents(Part p, bool symLock, string multiEngineMode)
        {
            int eventCount = 0;
            foreach (PartModule pm in p.Modules)
            {
                if (pm.Events != null)
                {
                    if (checkEngineMode(multiEngineMode, pm))
                    {
                        foreach (BaseEvent e in pm.Events)
                        {
                            if (e.active && e.guiActive)
                            {
                                eventCount++;
                                showEvent(p, symLock, pm, e, multiEngineMode);
                            }
                        }
                    }
                }
            }
            return eventCount;
        }

        private void showEvent(Part p, bool symLock, PartModule pm, BaseEvent e, string multiEngineMode)
        {
            if (GUILayout.Button(e.guiName))
            {
                e.Invoke();
                if (symLock)
                {
                    foreach (Part symPart in p.symmetryCounterparts)
                    {
                        foreach (PartModule symPM in symPart.Modules)
                        {
                            if (symPM.GetType() == pm.GetType())
                            {
                                if (checkEngineMode(multiEngineMode, symPM))
                                {
                                    foreach (BaseEvent symE in symPM.Events)
                                    {
                                        if (symE.active && symE.guiActive && e.id == symE.id)
                                        {
                                            symE.Invoke();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        // Display Resources
        private int showResources(Part p)
        {
            int resourceCount = 0;
            foreach (PartResource pr in p.Resources)
            {
                if (pr.isActiveAndEnabled)
                {
                    GUILayout.Label(pr.resourceName + ": " + string.Format("{0:N2}", Math.Round(pr.amount, 2)) + " / " + string.Format("{0:N2}", pr.maxAmount));
                    resourceCount++;
                }
            }
            return resourceCount;
        }

        // Display Temperature Info
        private int showTemperatureInfo(Part p)
        {
            if (PhysicsGlobals.ThermalDataDisplay)
            {
                GUILayout.Label("Thermal Mass: " + string.Format("{0:N2}", Math.Round(p.thermalMass, 2)));
                GUILayout.Label("Skin T.Mass: " + string.Format("{0:N2}", Math.Round(p.skinThermalMass, 2)));
                GUILayout.Label("Temp: " + string.Format("{0:N2}", Math.Round(p.temperature, 2)) + " / " + string.Format("{0:N2}", Math.Round(p.maxTemp)));
                GUILayout.Label("Skin Temp: " + string.Format("{0:N2}", Math.Round(p.skinTemperature, 2)) + " / " + string.Format("{0:N2}", Math.Round(p.skinMaxTemp)));
                GUILayout.Label("Cond Flux: " + string.Format("{0:N2}", Math.Round(p.thermalConductionFlux, 2)));
                GUILayout.Label("Conv Flux: " + string.Format("{0:N2}", Math.Round(p.thermalConvectionFlux, 2)));
                GUILayout.Label("Rad Flux: " + string.Format("{0:N2}", Math.Round(p.thermalRadiationFlux, 2)));
                GUILayout.Label("Int Flux: " + string.Format("{0:N2}", Math.Round(p.thermalInternalFlux, 2)));
                GUILayout.Label("SkinToInt Flux: " + string.Format("{0:N2}", Math.Round(p.skinToInternalFlux, 2)));
            }
            else
            {
                GUILayout.Label("Temp: " + string.Format("{0:N2}", Math.Round(p.temperature, 2)) + " / " + string.Format("{0:N2}", Math.Round(p.maxTemp)));
                GUILayout.Label("Skin Temp: " + string.Format("{0:N2}", Math.Round(p.skinTemperature, 2)) + " / " + string.Format("{0:N2}", Math.Round(p.skinMaxTemp)));
            }
            return (1);
        }

        // Display Aerodynamic Info
        private int showAeroInfo(Part p)
        {
            if (PhysicsGlobals.AeroDataDisplay)
            {
                GUILayout.Label("Mach: " + string.Format("{0:N2}", Math.Round(p.machNumber, 2)));
                GUILayout.Label("Drag: " + string.Format("{0:N2}", Math.Round(p.dragScalar, 2)));
                // TODO: figure out where the other values are stored
            }
            else
            {
                GUILayout.Label("Mach: " + string.Format("{0:N2}", Math.Round(p.machNumber, 2)));
                GUILayout.Label("Drag: " + string.Format("{0:N2}", Math.Round(p.dragScalar, 2)));
            }
            return (1);
        }

        // Display settings
        private void showSettings(PCWindow w)
        {
            bool oldSymLock = w.symLock;
            w.symLock = GUILayout.Toggle(w.symLock, new GUIContent("", "Symmetry Lock"), modStyle.guiStyles["symLockButton"]);
            if (w.symLock != oldSymLock)
            {
                if (w.currentPart != null)
                {
                    // reset part highlighting
                    clearHighlighting(activeParts);
                    setHighlighting(w.currentPart, w.symLock, true);
                }
            }

            GUILayout.Space(5f);

            if (w.currentPart == null)
            {
                if (w.popOutWindow == false)
                {
                    w.alphaSort = GUILayout.Toggle(w.alphaSort, new GUIContent("", "Alphabetical Sort"), modStyle.guiStyles["azButton"]);
                    GUILayout.Space(5f);
                    bool newSearch = GUILayout.Toggle(w.search, new GUIContent("", "Search"), modStyle.guiStyles["search"]);
                    if (newSearch != w.search)
                    {
                        partFilter = "";
                        updateParts = true;
                        w.search = newSearch;
                    }
                }
            }
            else
            {
                w.showResources = GUILayout.Toggle(w.showResources, new GUIContent("", "Resources Display"), modStyle.guiStyles["resourcesButton"]);
                GUILayout.Space(5f);
                w.showTemp = GUILayout.Toggle(w.showTemp, new GUIContent("", "Temperature Display"), modStyle.guiStyles["tempButton"]);
                GUILayout.Space(5f);
                w.showAero = GUILayout.Toggle(w.showAero, new GUIContent("", "Aerodynamics Display"), modStyle.guiStyles["aeroButton"]);
            }

        }

        // ----------------------------------- Part Highlighting -----------------------------------

        private void setHighlighting(Part p, bool symLock, bool highlight)
        {
            if (GameSettings.EDGE_HIGHLIGHTING_PPFX)
            {
                Transform model = p.FindModelTransform("model");
                Highlighter h = model.gameObject.GetComponent<Highlighter>();
                if (h != null)
                {
                    if (highlight)
                    {
                        h.ConstantOn(XKCDColors.Orange);
                    }
                    else
                    {
                        h.ConstantOff();
                    }

                    if (symLock)
                    {
                        foreach (Part symPart in p.symmetryCounterparts)
                        {
                            Transform symModel = symPart.FindModelTransform("model");
                            Highlighter symH = symModel.gameObject.GetComponent<Highlighter>();
                            if (symH != null)
                            {
                                if (highlight)
                                {
                                    // Highlight the secondary symmetrical parts in a different colour
                                    symH.ConstantOn(XKCDColors.Yellow);
                                }
                                else
                                {
                                    symH.ConstantOff();
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                p.SetHighlight(highlight, false);
                if (symLock)
                {
                    foreach (Part symPart in p.symmetryCounterparts)
                    {
                        symPart.SetHighlight(highlight, false);
                    }
                }

            }

        }

        private void clearHighlighting(List<Part> ap)
        {
            foreach (Part p in ap)
            {
                setHighlighting(p, true, false);
            }
        }

        // ----------------------------------- Multi-Engine Mode -----------------------------------

        private string getEngineMode(Part p)
        {
            string multiEngineMode = null;
            MultiModeEngine mme = p.GetComponent<MultiModeEngine>();
            if (mme != null)
            {
                multiEngineMode = mme.mode;
            }

            return multiEngineMode;
        }

        private static bool checkEngineMode(string multiEngineMode, PartModule pm)
        {
            bool modeMatches = true;
            ModuleEnginesFX mefx = null;
            if (multiEngineMode != null && pm.GetType().ToString() == "ModuleEnginesFX")
            {
                mefx = (ModuleEnginesFX)pm;
                modeMatches = (multiEngineMode == mefx.engineID) ? true : false;
            }
            return modeMatches;
        }

    }
}