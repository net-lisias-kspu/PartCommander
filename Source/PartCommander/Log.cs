
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
