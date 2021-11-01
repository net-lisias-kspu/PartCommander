/*
	This file is part of Part Commander /L Unleashed
		© 2018-2021 Lisias T : http://lisias.net <support@lisias.net>

	THIS FILE is licensed to you under:

		* WTFPL - http://www.wtfpl.net
			* Everyone is permitted to copy and distribute verbatim or modified
 				copies of this license document, and changing it is allowed as long
				as the name is changed.

	THIS FILE is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
*/
using KSPe.Util.Log;

namespace PartCommander
{
    internal static class Log
    {
		private readonly static Logger LOG = Logger.CreateForType<PartCommander>();

		internal static void Info(string msg, params object[] @params)
		{
			LOG.info(msg, @params);
		}
	}
}
