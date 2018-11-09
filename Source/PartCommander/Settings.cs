using System;
using UnityEngine;

using KSPe.IO.Data;

namespace PartCommander
{
    public class Settings
    {
        private readonly PluginConfiguration CONFIG = PluginConfiguration.CreateForType<PartCommander>();

        internal bool enableHotKey = true;
        internal KeyCode hotKey = KeyCode.P;
        internal bool hideUnAct = false;
        internal bool altSkin = false;
        internal int fontSize = 12;

		internal void Load()
		{
			CONFIG.load();
			this.enableHotKey = CONFIG.GetValue<bool>("enableHotKey", this.enableHotKey);
			this.hotKey = CONFIG.GetValue<KeyCode>("hotKey", this.hotKey);
			this.hideUnAct = CONFIG.GetValue<bool>("hideUnAct", this.hideUnAct);
			this.altSkin = CONFIG.GetValue<bool>("altSkin", this.altSkin);
			this.fontSize = CONFIG.GetValue<int>("fontSize", this.fontSize);
		}

		internal void Save()
		{
			CONFIG.SetValue("enableHotKey", this.enableHotKey);
			CONFIG.SetValue("hotKey", this.hotKey);
			CONFIG.SetValue("hideUnAct", this.hideUnAct);
			CONFIG.SetValue("altSkin", this.altSkin);
			CONFIG.SetValue("fontSize", this.fontSize);
			CONFIG.save();
		}
	}
}
