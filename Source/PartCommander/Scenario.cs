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
// Scenario.cs
// Used to store persistent settings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PartCommander
{
    public class PCScenario : ScenarioModule
    {

        public static PCScenario Instance { get; private set; }
        public ModSettings gameSettings { get; private set; }

        public PCScenario()
        {
            Instance = this;
            gameSettings = new ModSettings();
        }

        public override void OnLoad(ConfigNode gameNode)
        {
            base.OnLoad(gameNode);
            gameSettings.Load(gameNode);
        }

        public override void OnSave(ConfigNode gameNode)
        {
            gameSettings.Save(gameNode);
            base.OnSave(gameNode);
        }

    }
}

