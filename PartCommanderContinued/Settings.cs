using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSPPluginFramework;

namespace PartCommanderContinued
{
    public class Settings : ConfigNodeStorage
    {
        internal Settings(String FilePath) : base(FilePath) { }

        [Persistent]
        internal bool enableHotKey = true;

        [Persistent]
        internal KeyCode hotKey = KeyCode.P;

        [Persistent]
        internal bool hideUnAct = false;

        [Persistent]
        internal bool altSkin = false;

        [Persistent]
        internal int fontSize = 12;
    }
}
