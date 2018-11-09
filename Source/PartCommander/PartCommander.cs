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
//using HighlightingSystem;
using KSP.UI.Screens;
using KSP.Localization;

using KSPe;
using KSPe.IO;
using ClickThroughFix;
using ToolbarControl_NS;


namespace PartCommander
{

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class PartCommander : MonoBehaviour
    {
        internal ToolbarControl toolbarControl = null;

        private List<Part> activeParts = new List<Part>();
        private List<Part> highlightedParts = new List<Part>();
        private string partFilter = "";
        private PartCategories partFilterCategory = PartCategories.none;
        private bool movePrevCategory = false;
        private bool moveNextCategory = false;

        private List<PartCategories> partCats;

        internal bool updateParts = true;

        private PCWindow currentWindow;
        private SettingsWindow settingsWindow;

        private bool visibleUI = true;

        private bool controlsLocked = false;
        private string controlsLockID = "PartCommander_LockID";

        private bool popOut = false;

        internal ModStyle modStyle, modStyleKSP, modStyleUnity;

        internal string showTooltip = "";

        internal Settings settings = new Settings();

        public static PartCommander Instance { get; private set; }
        public PartCommander()
        {
            Instance = this;
        }

        // ------------------------------- Main Events --------------------------------
        internal void Awake()
        {

            settings.Load();

            //modStyle = new ModStyle();

            // Hook into events for Application Launcher

            GameEvents.onGameSceneLoadRequested.Add(onSceneChange);

        }

        public void Start()
        {
            // Add hooks for showing/hiding on F2
            GameEvents.onShowUI.Add(showUI);
            GameEvents.onHideUI.Add(hideUI);

            AddLauncherButtons();

            // Add hooks for updating part list when needed
            GameEvents.onVesselWasModified.Add(triggerUpdateParts);
            GameEvents.onVesselChange.Add(triggerUpdateParts);

            // Load part categories
            partCats = Enum.GetValues(typeof(PartCategories)).Cast<PartCategories>().ToList();
            partCats.Remove(PartCategories.none);
            partCats.Remove(PartCategories.Propulsion);
            partCats.Sort();
            partCats = partCats.OrderBy(o => o.ToString()).ToList();
            partCats.Insert(0, PartCategories.none);

            // Create settings window
            //settingsWindow = new SettingsWindow(modStyle, settings);

        }

        public void triggerUpdateParts(Vessel v)
        {
            updateParts = true;
        }

        public void Update()
        {
            // Detect hotkey
            if (settings.enableHotKey && Input.GetKeyDown(settings.hotKey))
            {
                if (GameSettings.MODIFIER_KEY.GetKey())
                {
                    toggleWindow();
                }
            }

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
                    for (int pi = 0; pi < FlightGlobals.ActiveVessel.Parts.Count; pi++)
                    //foreach (Part p in FlightGlobals.ActiveVessel.Parts)
                    {
                        Part p = FlightGlobals.ActiveVessel.Parts[pi];
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
                // partWindows is a dictionary, can't repace the foreach
                foreach (PCWindow pow in currentWindow.partWindows.Values)
                {
                    // Resurrect the part if necessary
                    if (pow.currentPart == null & pow.currentPartId != 0u)
                    {
                        for (int pi = 0; pi < FlightGlobals.ActiveVessel.Parts.Count; pi++)
                        //foreach (Part p in FlightGlobals.ActiveVessel.Parts)
                        {
                            Part p = FlightGlobals.ActiveVessel.Parts[pi];
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
                        setHighlighting(currentWindow.currentPart, currentWindow.symLock, false);

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
                        setHighlighting(currentWindow.currentPart, currentWindow.symLock, false);
                        currentWindow.currentPart = null;
                        currentWindow.currentPartId = 0u;
                        currentWindow.showPartSelector = true;
                    }
                }

                // Update the part category filter
                if (movePrevCategory)
                {
                    int i = partCats.FindIndex(a => a.Equals(partFilterCategory));
                    i--;
                    if (i < 0)
                    {
                        partFilterCategory = partCats.Last();
                    }
                    else
                    {
                        partFilterCategory = partCats[i];
                    }

                    updateParts = true;
                    movePrevCategory = false;
                    currentWindow.scrollPos.x = currentWindow.scrollPos.y = 0;
                }

                if (moveNextCategory)
                {
                    int i = partCats.FindIndex(a => a.Equals(partFilterCategory));
                    i++;
                    if (i >= partCats.Count)
                    {
                        partFilterCategory = partCats.First();
                    }
                    else
                    {
                        partFilterCategory = partCats[i];
                    }

                    updateParts = true;
                    moveNextCategory = false;
                    currentWindow.scrollPos.x = currentWindow.scrollPos.y = 0;
                }

                resizeWindows();
                windowHover();
                if (updateParts)
                {
                    clearHighlighting(highlightedParts);
                    getActiveParts();
                }


                // If there's only one available part on the vessel, select it automatically.
                if (currentWindow.showPartSelector && activeParts.Count == 1 && partFilter == "" && partFilterCategory == PartCategories.none)
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
            if (modStyle == null)
            {
                modStyleKSP = new ModStyle(false);
                modStyleUnity = new ModStyle(true);
                if (PartCommander.Instance.settings.altSkin)
                {
                    settingsWindow = new SettingsWindow(modStyleUnity, settings);
                    modStyle = modStyleUnity;
                }
                else
                {
                    settingsWindow = new SettingsWindow(modStyleKSP, settings);
                    modStyle = modStyleKSP;
                }
            }

            if (PCScenario.Instance.gameSettings.visibleWindow)
                toolbarControl.SetTrue(false);
            


            // Make sure we have something to show
            if (visibleUI && FlightGlobals.ActiveVessel != null && currentWindow != null && PCScenario.Instance != null && PCScenario.Instance.gameSettings.visibleWindow)
            {
#if false
                if (PartCommander.Instance.settings.altSkin)
                    GUI.skin = modStyleUnity.skin;
                else
#endif
                    GUI.skin = modStyle.skin;
                currentWindow.windowRect = ClickThruBlocker.GUILayoutWindow(currentWindow.windowId, currentWindow.windowRect, mainWindow, "");
                // Set the default location/size for new windows to be the same as this one
                PCScenario.Instance.gameSettings.windowDefaultRect = currentWindow.windowRect;

                // Process any popout windows
                foreach (PCWindow pow in currentWindow.partWindows.Values)
                {
                    pow.windowRect = ClickThruBlocker.GUILayoutWindow(pow.windowId, pow.windowRect, partWindow, "");
                }
                if (showTooltip != "" && showTooltip != null)
                {
                    float minWidth;
                    float maxWidth;
                    modStyle.guiStyles["tooltip"].CalcMinMaxWidth(new GUIContent(showTooltip), out minWidth, out maxWidth);
                    GUI.Label(new Rect(Input.mousePosition.x + 10, Screen.height - Input.mousePosition.y + 20, maxWidth, 20), showTooltip, modStyle.guiStyles["tooltip"]);
                    GUI.depth = 0;
                }

                settingsWindow.draw();

            }
        }

        // Remove the launcher button when the scene changes
        public void onSceneChange(GameScenes scene)
        {
            removeLauncherButtons();
        }

        // Cleanup when the module is destroyed
        protected void OnDestroy()
        {
            PCScenario.Instance.gameSettings.visibleWindow = false;

            removeLauncherButtons();

            if (InputLockManager.lockStack.ContainsKey(controlsLockID))
            {
                InputLockManager.RemoveControlLock(controlsLockID);
            }

            GameEvents.onGameSceneLoadRequested.Remove(onSceneChange);

        }

        // ------------------------------------------ Application Launcher / UI ---------------------------------------

        internal const string MODID = "PartCommander_NS";
        internal const string MODNAME = "Part Commander";

        void AddLauncherButtons()
        {
            toolbarControl = gameObject.AddComponent<ToolbarControl>();
            toolbarControl.AddToAllToolbars(
                    showWindow,
                    hideWindow,
                 ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.MAPVIEW,
                MODID,
                "partCommanderButton",
                KSPe.GameDB.Asset<PartCommander>.Solve("textures/toolbar"),
                KSPe.GameDB.Asset<PartCommander>.Solve("textures/toolbar"),
                File<PartCommander>.Asset.Solve("textures/blizzyToolbar.png"),
                MODNAME
            );
        }

        public void removeLauncherButtons()
        {
            toolbarControl.OnDestroy();
            Destroy(toolbarControl);
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
            if (controlsLocked)
            {
                InputLockManager.RemoveControlLock(controlsLockID);
                controlsLocked = false;
                clearHighlighting(highlightedParts);
            }
        }

        public void toggleWindow()
        {
            PartCommander.Instance.toolbarControl.SetFalse();
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

            settingsWindow.resizeWindow();

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

                if (settingsWindow.windowRect.Contains(mousePos) && settingsWindow.showWindow)
                {
                    overWindow = true;
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
                    clearHighlighting(highlightedParts);
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
                    clearHighlighting(highlightedParts);
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
            if (!currentWindow.partWindows.ContainsKey(id)) return;
            PCWindow currentPOW = currentWindow.partWindows[id];
            if (currentPOW.currentPart == null || currentPOW.currentPart.vessel != FlightGlobals.ActiveVessel)
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

                GUILayout.Label(partTitle, modStyle.guiStyles["titleLabel"]);
            }
            else
            {
                GUILayout.Label(FlightGlobals.ActiveVessel.vesselName, modStyle.guiStyles["titleLabel"]);
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
            if (w.currentPart == null && w.popOutWindow == false)
            {
                if (w.showSearch)
                {
                    string newPartFilter = GUILayout.TextField(partFilter);
                    if (newPartFilter != partFilter)
                    {
                        partFilter = newPartFilter;
                        updateParts = true;
                    }
                    GUILayout.Space(5f);
                }
                else
                {
                    partFilter = "";
                }
                if (w.showFilter)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(2f);
                    if (GUILayout.Button("", modStyle.guiStyles["left"]))
                    {
                        movePrevCategory = true;
                    }
                    GUILayout.Space(3f);
                    if (partFilterCategory == PartCategories.none)
                    {
                        GUILayout.Label("All Categories", modStyle.guiStyles["categoryLabel"], GUILayout.ExpandWidth(true));
                    }
                    else
                    {
                        GUILayout.Label(partFilterCategory.ToString(), modStyle.guiStyles["categoryLabel"], GUILayout.ExpandWidth(true));
                    }
                    GUILayout.Space(3f);
                    if (GUILayout.Button("", modStyle.guiStyles["right"]))
                    {
                        moveNextCategory = true;
                    }
                    GUILayout.Space(2f);
                    GUILayout.EndHorizontal();
                    GUILayout.Space(5f);

                }
                else
                {
                    partFilterCategory = PartCategories.none;
                }
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
                if (GUI.Button(new Rect(7f, 3f, 20f, 20f), new GUIContent("", "Pop off in new window"), modStyle.guiStyles["popoutButton"]))
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
            else
            {
                // Create settings button in upper right corner
                settingsWindow.showWindow = GUI.Toggle(new Rect(w.windowRect.width - 23, 3f, 20f, 20f), settingsWindow.showWindow, new GUIContent("", "Settings"), modStyle.guiStyles["settings"]);
            }

            // Create resize button in bottom right corner
            if (GUI.RepeatButton(new Rect(w.windowRect.width - 23f, w.windowRect.height - 23f, 20f, 20f), "", modStyle.guiStyles["resizeButton"]))
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
            GUI.DragWindow();
        }

        // ----------------------------------- Part Selector -------------------------------
        private void getActiveParts()
        {
            // Build list of active parts
            activeParts.Clear();
            List<Part> hiddenParts = new List<Part>();
            for (int pi = 0; pi < FlightGlobals.ActiveVessel.Parts.Count; pi++)
            //foreach (Part p in FlightGlobals.ActiveVessel.Parts)
            {
                Part p = FlightGlobals.ActiveVessel.Parts[pi];

                if (partFilterCategory == PartCategories.none || p.partInfo.category == partFilterCategory)
                {
                    bool includePart = false;
                    if (!hiddenParts.Contains(p))
                    {
                        // Hide other members of the symmetry
                        if (currentWindow.symLock)
                        {
                            for (int psi = 0; psi < p.symmetryCounterparts.Count; psi++)
                            //foreach (Part symPart in p.symmetryCounterparts)
                            {
                                Part symPart = p.symmetryCounterparts[psi];
                                hiddenParts.Add(symPart);
                            }
                        }
                        for (int pmi = 0; pmi < p.Modules.Count; pmi++)
                        //foreach (PartModule pm in p.Modules)
                        {
                            PartModule pm = p.Modules[pmi];
     
                            if (includePart)
                            {
                                // Part was already included, so break out
                                break;
                            }
                            else
                            {
                                if (pm.Fields != null || pm.Events != null)
                                {
                                    for (int bfi = 0; bfi < pm.Fields.Count; bfi++)
                                    //foreach (BaseField f in pm.Fields)
                                    {
                                        BaseField f = pm.Fields[bfi];
                                        if (f.guiActive && f.guiName != "")
                                        {
                                            if (settings.hideUnAct)
                                            {
                                                if (f.uiControlFlight.GetType().ToString() == "UI_Toggle" || f.uiControlFlight.GetType().ToString() == "UI_FloatRange")
                                                {
                                                    includePart = true;
                                                    break;
                                                }
                                            }
                                            else
                                            {
                                                includePart = true;
                                                break;
                                            }
                                        }
                                    }
                                    if (!includePart)
                                    {
                                        // For some reason, using the count (which is commented out)
                                        // gets null events
                                        //for (int bei = 0; bei < pm.Events.Count; bei++)
                                        foreach (BaseEvent e in pm.Events)
                                        {
                                            //BaseEvent e = pm.Events[bei];
                                            if (e.guiActive && e.active)
                                            {
                                                includePart = true;
                                                break;
                                            }
                                        }
                                    }
                                    Log.Info("3l");
                                    if (includePart)
                                    {
                                        activeParts.Add(p);
                                    }
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

            for (int pi = 0; pi < activeParts.Count; pi++)
            //foreach (Part p in activeParts)
            {
                Part p = activeParts[pi];

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
            if (p != null)
            {
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
            }
            return (optionsCount);
        }

        // Routines for displaying/setting KSPFields
        private int showFields(Part p, bool symLock, string multiEngineMode)
        {
            int fieldCount = 0;
            for (int pmi = 0; pmi < p.Modules.Count; pmi++)
            //foreach (PartModule pm in p.Modules)
            {
                PartModule pm = p.Modules[pmi];
                if (pm.Fields != null)
                {
                    if (checkEngineMode(multiEngineMode, pm))
                    {
                        for (int fi = 0; fi < pm.Fields.Count; fi++)
                        //foreach (BaseField f in pm.Fields)
                        {
                            BaseField f = pm.Fields[fi];
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

            if (GUILayout.Button(f.guiName + ": " + Localizer.Format(curText)))
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
                for (int spi = 0; spi < p.symmetryCounterparts.Count; spi++)
                //foreach (Part symPart in p.symmetryCounterparts)
                {
                    Part symPart = p.symmetryCounterparts[spi];

                    for (int pmi = 0; pmi < symPart.Modules.Count; pmi++)
                    //foreach (PartModule symPM in symPart.Modules)
                    {
                        PartModule symPM = symPart.Modules[pmi];

                        if (symPM.GetType() == pm.GetType())
                        {
                            if (checkEngineMode(multiEngineMode, symPM))
                            {
                                for (int bfi = 0; bfi < symPM.Fields.Count; bfi++)
                                //foreach (BaseField symF in symPM.Fields)
                                {
                                    BaseField symF = symPM.Fields[bfi];
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
            for (int pmi = 0; pmi < p.Modules.Count; pmi++)
            //foreach (PartModule pm in p.Modules)
            {
                PartModule pm = p.Modules[pmi];
                if (pm.Events != null)
                {
                    if (checkEngineMode(multiEngineMode, pm))
                    {
                        // For some reason, using the count (commented out) gets null events
                        //for (int bei = 0; bei < pm.Events.Count; bei++)
                        foreach (BaseEvent e in pm.Events)
                        {
                            //BaseEvent e = pm.Events[bei];
                            if (e != null && e.active && e.guiActive)
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
            GUIStyle bStyle = new GUIStyle(GUI.skin.button);

            GUILayout.BeginHorizontal();


            Color btnNrml = new Color(34f / 255f, 199f / 255f, 222f / 255f, 1);
            Texture2D BtnNrmlTex = new Texture2D(1, 1);
            BtnNrmlTex.SetPixel(0, 0, btnNrml);
            BtnNrmlTex.Apply();
#if false
            Color btnHover = new Color(60f / 255f, 205f / 255f, 226f / 255f, 1);
            Texture2D BtnHoverText = new Texture2D(1, 1);
            BtnHoverText.SetPixel(0, 0, btnHover);
            BtnHoverText.Apply();

            Color btnActive = new Color(0f / 255f, 138f / 255f, 157f / 255f, 1);
            Texture2D BtnActiveText = new Texture2D(1, 1);
            BtnActiveText.SetPixel(0, 0, btnActive);
            BtnActiveText.Apply();
#endif


            Color oldColor = GUI.color;
            GUI.color = btnNrml;

#if false
            bStyle.normal.background = BtnNrmlTex;

            bStyle.onActive.background = BtnActiveText;
            bStyle.active.background = BtnActiveText;
            //bStyle.focused.background = BtnNrmlTex;
            bStyle.hover.background = BtnHoverText;
            bStyle.onHover.background = BtnHoverText;
            //bStyle.onFocused.background = BtnNrmlTex;
            bStyle.onNormal.background = BtnNrmlTex;

#endif
            if (GUILayout.Button("", bStyle, GUILayout.Width(50), GUILayout.Height(15)))
            {               
                if (symLock)
                {
                    for (int i = 0; i < FlightGlobals.ActiveVessel.Parts.Count; i++)
                    {
                        Part symPart = FlightGlobals.ActiveVessel.Parts[i];
                        if (FlightGlobals.ActiveVessel.Parts[i].isSymmetryCounterPart(p))
                        {

                            for (int pmi = 0; pmi < symPart.Modules.Count; pmi++)
                            {

                                PartModule symPM = symPart.Modules[pmi];
                                if (symPM.GetType() == pm.GetType())
                                {
                                    if (checkEngineMode(multiEngineMode, symPM))
                                    {
                                        foreach (BaseEvent symE in symPM.Events)
                                        {
                                            if (symE != null && symE.active && symE.guiActive && e.id == symE.id)
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
                e.Invoke();
#if false

                    for (int pi = 0; pi < p.symmetryCounterparts.Count; pi++)                    
                    //foreach (Part symPart in p.symmetryCounterparts)
                    {
                        Part symPart = p.symmetryCounterparts[pi];
                        for (int pmi = 0; pmi < symPart.Modules.Count; pmi++)
                        {

                            PartModule symPM = symPart.Modules[pmi];
                            if (symPM.GetType() == pm.GetType())
                            {
                                if (checkEngineMode(multiEngineMode, symPM))
                                {
                                    foreach (BaseEvent symE in symPM.Events)
                                    {
                                        // exception occurs here

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
                e.Invoke();
#endif

            }
            //  GUI.skin = HighLogic.Skin;
            GUI.color = oldColor;
            GUILayout.Label(e.guiName);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        // Display Resources
        private int showResources(Part p)
        {
            int resourceCount = 0;
            for (int pri = 0; pri < p.Resources.Count; pri++)
            //foreach (PartResource pr in p.Resources)
            {
                PartResource pr = p.Resources[pri];
                // if (pr.isActiveAndEnabled)
                //                {
                GUILayout.Label(pr.resourceName + ": " + string.Format("{0:N2}", Math.Round(pr.amount, 2)) + " / " + string.Format("{0:N2}", pr.maxAmount));
                resourceCount++;
                //                }
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
                updateParts = true;
                if (w.currentPart != null)
                {
                    // reset part highlighting
                    clearHighlighting(highlightedParts);
                    setHighlighting(w.currentPart, w.symLock, true);
                }
            }

            GUILayout.Space(5f);

            if (w.currentPart == null)
            {
                if (w.popOutWindow == false)
                {
                    bool newAlphaSort = GUILayout.Toggle(w.alphaSort, new GUIContent("", "Alphabetical Sort"), modStyle.guiStyles["azButton"]);
                    if (newAlphaSort != w.alphaSort)
                    {
                        updateParts = true;
                        w.alphaSort = newAlphaSort;
                    }
                    GUILayout.Space(5f);
                    bool newSearch = GUILayout.Toggle(w.showSearch, new GUIContent("", "Search"), modStyle.guiStyles["search"]);
                    if (newSearch != w.showSearch)
                    {
                        partFilter = "";
                        updateParts = true;
                        w.showSearch = newSearch;
                    }
                    GUILayout.Space(5f);
                    bool newFilter = GUILayout.Toggle(w.showFilter, new GUIContent("", "Category Filter"), modStyle.guiStyles["filter"]);
                    if (newFilter != w.showFilter)
                    {
                        partFilterCategory = PartCategories.none;
                        updateParts = true;
                        w.showFilter = newFilter;
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

        private void setHighlighting(Part p, bool symLock, bool highlight, bool clear = true)
        {
            if (p == null)
            {
                return;
            }

            // if (GameSettings.EDGE_HIGHLIGHTING_PPFX)
            {
#if false
                Transform model = null;
                try
                {
                    model = p.FindModelTransform("model");
                }
                catch (Exception ex)
                {
                    Debug.Log("[PartCommander] caught exception " + ex.Message);
                }
                if (model != null)
#endif
                //                {

                //  Highlighter h = model.gameObject.GetComponent<Highlighter>();
                // if (h != null)
                {
                    if (highlight)
                    {
                        p.highlighter.ConstantOn(XKCDColors.Orange);
                        if (!highlightedParts.Exists(x => x == p))
                        {
                            highlightedParts.Add(p);
                        }
                    }
                    else
                    {
                        p.highlighter.ConstantOff();
                        if (highlightedParts.Exists(x => x == p))
                        {

                            if (clear) highlightedParts.Remove(p);
                        }
                    }

                    if (symLock)
                    {
                        for (int pi = 0; pi < p.symmetryCounterparts.Count; pi++)
                        //foreach (Part symPart in p.symmetryCounterparts)
                        {
                            Part symPart = p.symmetryCounterparts[pi];
#if false
                                Transform symModel = null;
                                try
                                {
                                    symModel = symPart.FindModelTransform("model");
                                }
                                catch (Exception ex)
                                {
                                    Debug.Log("[PartCommander] caught exception " + ex.Message);
                                }
                                if (symModel != null)
#endif
                            {
                                //Highlighter symH = symModel.gameObject.GetComponent<Highlighter>();
                                //if (symH != null)
                                {
                                    if (highlight)
                                    {
                                        // Highlight the secondary symmetrical parts in a different colour
                                        symPart.highlighter.ConstantOn(XKCDColors.Yellow);
                                        if (!highlightedParts.Exists(x => x == symPart))
                                        {

                                            highlightedParts.Add(symPart);
                                        }
                                    }
                                    else
                                    {
                                        symPart.highlighter.ConstantOff();
                                        if (highlightedParts.Exists(x => x == symPart))
                                        {

                                            if (clear) highlightedParts.Remove(symPart);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                //                }
            }
#if false
            else
            {
                if (highlight)
                {
                    p.SetHighlight(true, false);
                    if (!highlightedParts.Exists(x => x == p))
                    {

                        highlightedParts.Add(p);
                    }
                    if (symLock)
                    {
                        foreach (Part symPart in p.symmetryCounterparts)
                        {
                            symPart.SetHighlight(true, false);
                            if (!highlightedParts.Exists(x => x == symPart))
                            {

                                highlightedParts.Add(symPart);
                            }

                        }
                    }
                }
                else
                {
                    p.SetHighlight(false, false);
                    if (highlightedParts.Exists(x => x == p))
                    {

                        if (clear) highlightedParts.Remove(p);
                    }
                    if (symLock)
                    {
                        foreach (Part symPart in p.symmetryCounterparts)
                        {
                            symPart.SetHighlight(false, false);
                            if (highlightedParts.Exists(x => x == symPart))
                            {

                                if (clear) highlightedParts.Remove(symPart);
                            }

                        }
                    }
                }
            }
#endif
        }

        private void clearHighlighting(List<Part> ap)
        {
            for (int pi = 0; pi < ap.Count; pi++)
            //foreach (Part p in ap)
            {
                setHighlighting(ap[pi], true, false, false);
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