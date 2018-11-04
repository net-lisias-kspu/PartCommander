using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

using ClickThroughFix;


namespace PartCommander
{
    internal class SettingsWindow
    {
        internal bool showWindow;
        internal bool resizingWindow;
        internal Rect windowRect;
        internal Rect dragRect;
        internal Vector2 scrollPos = new Vector2(0f, 0f);
        internal int windowId;
        internal ModStyle modStyle;
        internal Settings settings;
        internal bool settingHotKey = false;

        internal SettingsWindow(ModStyle m, Settings s)
        {
            modStyle = m;
            settings = s;
            showWindow = false;
            windowRect = new Rect((Screen.width - 250) / 2, (Screen.height - 300) / 2, 250, 300);
            windowId = GUIUtility.GetControlID(FocusType.Passive);
        }

        internal void draw()
        {
            if (showWindow)
            {
                windowRect = ClickThruBlocker.GUILayoutWindow(windowId, windowRect, drawWindow, "");
            }
        }

        internal void drawWindow(int id)
        {
            if (PartCommander.Instance.settings.altSkin)
            {
                GUI.skin = PartCommander.Instance.modStyleUnity.skin;
            }
            else
            {
                GUI.skin = PartCommander.Instance.modStyle.skin;
            }
            GUILayout.BeginVertical();
            GUILayout.Label("Settings", modStyle.guiStyles["titleLabel"]);
            GUILayout.EndVertical();
            if (Event.current.type == EventType.Repaint)
            {
                dragRect = GUILayoutUtility.GetLastRect();
            }
            GUILayout.BeginVertical();
            scrollPos = GUILayout.BeginScrollView(scrollPos);

            GUILayout.BeginHorizontal();
            bool newHideUnAct = GUILayout.Toggle(settings.hideUnAct, "Hide unactionable parts", modStyle.guiStyles["toggleText"]);
            if (newHideUnAct != settings.hideUnAct)
            {
                PartCommander.Instance.updateParts = true;
                settings.hideUnAct = newHideUnAct;
                settings.Save();
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(5f);
            GUILayout.BeginHorizontal();

            GUILayout.Label("Font Size:", modStyle.guiStyles["settingsLabel"]);
            bool fontChanged = false;
            GUILayout.FlexibleSpace();
            if (settings.fontSize <= 12)
                GUI.enabled = false;
            if (GUILayout.Button("<", modStyle.guiStyles["settingsButton"]))
            {
                settings.fontSize--;
                fontChanged = true;
            }
            GUI.enabled = true;
            string s = GUILayout.TextField(settings.fontSize.ToString(), modStyle.guiStyles["settingsLabel"]);
            if (settings.fontSize >= 20)
                GUI.enabled = false;
            if (GUILayout.Button(">", modStyle.guiStyles["settingsButton"]))
            {
                settings.fontSize++;
                fontChanged = true;
            }
            GUI.enabled = true;
            if (fontChanged)
            {
                settings.Save();
                PartCommander.Instance.modStyle.UpdateFontSize(settings.fontSize);
                PartCommander.Instance.modStyleUnity.UpdateFontSize(settings.fontSize);

            }
            GUILayout.EndHorizontal();
            GUILayout.Space(5f);
            GUILayout.BeginHorizontal();

            bool newAltSkin = GUILayout.Toggle(settings.altSkin, "Use alternate skin", modStyle.guiStyles["toggleText"]);
            if (newAltSkin != settings.altSkin)
            {
                settings.altSkin = newAltSkin;
                settings.Save();
                if (PartCommander.Instance.settings.altSkin)
                {
                    modStyle = PartCommander.Instance.modStyleUnity;
                    PartCommander.Instance.modStyle = PartCommander.Instance.modStyleUnity;

                }
                else
                {
                    modStyle = PartCommander.Instance.modStyleKSP;
                    PartCommander.Instance.modStyle = PartCommander.Instance.modStyle;
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(5f);
            GUILayout.BeginHorizontal();

            bool newEnableHotKey = GUILayout.Toggle(settings.enableHotKey, "Enable hot key", modStyle.guiStyles["toggleText"]);
            if (newEnableHotKey != settings.enableHotKey)
            {
                settings.enableHotKey = newEnableHotKey;
                settings.Save();
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(5f);
            GUILayout.BeginHorizontal();

            if (settingHotKey)
            {
                GUILayout.Label("Type a new hot key...", modStyle.guiStyles["settingsLabel"]);
                if (Event.current.isKey)
                {
                    settings.hotKey = Event.current.keyCode;
                    settings.Save();
                    settingHotKey = false;
                }
            }
            else
            {
                if (settings.enableHotKey)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Mod + ");
                    if (GUILayout.Button(new GUIContent(settings.hotKey.ToString(), "Click to set new hot key"), modStyle.guiStyles["settingsButton"]))
                    {
                        settingHotKey = true;
                    }
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();
            GUILayout.Space(25f);
            GUILayout.EndVertical();
            
            if (GUI.Button(new Rect(windowRect.width - 18, 3f, 15f, 15f), new GUIContent("", "Close"), modStyle.guiStyles["closeButton"]))
            {
                showWindow = false;
            }
            // Create resize button in bottom right corner
            if (GUI.RepeatButton(new Rect(windowRect.width - 23, windowRect.height - 23, 20, 20), "", modStyle.guiStyles["resizeButton"]))
            {
                resizingWindow = true;
            }
            GUI.DragWindow();

        }

        internal void resizeWindow()
        {
            if (Input.GetMouseButtonUp(0))
            {
                resizingWindow = false;
            }

            if (resizingWindow)
            {
                windowRect.width = Input.mousePosition.x - windowRect.x + 10;
                windowRect.width = Mathf.Clamp(windowRect.width, modStyle.minWidth, Screen.width);
                windowRect.height = (Screen.height - Input.mousePosition.y) - windowRect.y + 10;
                windowRect.height = Mathf.Clamp(windowRect.height, modStyle.minHeight, Screen.height);
            }
        }
    }
}
