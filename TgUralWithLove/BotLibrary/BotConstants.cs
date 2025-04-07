using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotLib
{
    public static class BotConstants
    {
        internal static readonly string LastCommandMark = "_LastCmdMrk";
        public static int SoonDays = 5;
        public static int SoonTake = 10;
        public static int PoiSearchRadiusKm = 5;
        public static int NearPoiTake = 10;

        public enum CallbackDataMarks { mrkfev }


        public enum UBotCommands { start, help, mkrf_events, uwl_choise, nearpoi }
        public enum BotModeKinds { polling, webhook }
    }
}
