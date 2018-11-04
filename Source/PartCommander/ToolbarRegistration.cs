using UnityEngine;
using ToolbarControl_NS;

namespace PartCommander
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class RegisterToolbar : MonoBehaviour
    {
        void Start()
        {
            ToolbarControl.RegisterMod(PartCommander.MODID, PartCommander.MODNAME);
        }
    }
}