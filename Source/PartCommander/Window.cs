/*
	This file is part of Part Commander /L Unleashed
		© 2018-2021 Lisias T : http://lisias.net <support@lisias.net>
		© 2016-2018 LinugGuruGamer
		© 2015-2016 seanmcdougall

	Part Commander /L Unleashed is licensed as follows:

		* GPL 3.0 : https://www.gnu.org/licenses/gpl-3.0.txt

	Part Commandere /L Unleashed is distributed in the hope that
	it will be useful, but WITHOUT ANY WARRANTY; without even the implied
	warranty of	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.

	You should have received a copy of the GNU General Public License 3.0 along
	with Part Commander /L Unleashed. If not, see <https://www.gnu.org/licenses/>.

*/
// Window.cs
// Stores settings for a particular vessel/part window

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PartCommander
{
    public class PCWindow
    {
        internal bool popOutWindow = false;
        internal Rect windowRect;
        internal Rect dragRect;
        internal bool resizingWindow = false;
        internal Part currentPart = null;
        internal uint currentPartId;
        internal bool symLock = true;
        internal bool alphaSort = false;
        internal bool showSearch = false;
        internal bool showPartSelector = true;
        internal bool showResources = true;
        internal bool showTemp = false;
        internal bool showAero = false;
        internal bool showFilter = false;
        internal int windowId;
        internal bool togglePartSelector = false;
        internal Vector2 oldScrollPos = new Vector2(0f, 0f);
        internal Vector2 scrollPos = new Vector2(0f, 0f);
        internal Part selectPart = null;
        internal Dictionary<int, PCWindow> partWindows;

        public PCWindow(float x, float y, float width, float height, bool popOut)
        {
            windowRect = new Rect(x, y, width, height);
            windowId = GUIUtility.GetControlID(FocusType.Passive);
            partWindows = new Dictionary<int, PCWindow>();
            popOutWindow = popOut;
        }

        public PCWindow(Rect r, bool popOut)
        {
            windowRect = r;
            windowId = GUIUtility.GetControlID(FocusType.Passive);
            partWindows = new Dictionary<int, PCWindow>();
            popOutWindow = popOut;
        }

        public PCWindow(bool popOut)
        {
            windowRect = PCScenario.Instance.gameSettings.windowDefaultRect;
            windowId = GUIUtility.GetControlID(FocusType.Passive);
            partWindows = new Dictionary<int, PCWindow>();
            popOutWindow = popOut;
        }

    }
}
