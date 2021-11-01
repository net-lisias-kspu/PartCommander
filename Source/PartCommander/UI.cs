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
using Asset = KSPe.IO.Asset<PartCommander.Startup>;

namespace PartCommander
{
	public static class UI
	{
		public static class Icon
		{
			private const string DIR = "textures";

			private static Texture2D _toolbar = null;
			public static Texture2D toolbar => _toolbar ?? (_toolbar = Asset.Texture2D.LoadFromFile(DIR, "toolbar"));

			private static Texture2D _blizzyToolbar = null;
			public static Texture2D blizzyToolbar => _blizzyToolbar ?? (_blizzyToolbar = Asset.Texture2D.LoadFromFile(DIR, "blizzyToolbar"));
		}
	}
}
