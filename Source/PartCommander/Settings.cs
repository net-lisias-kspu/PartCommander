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
