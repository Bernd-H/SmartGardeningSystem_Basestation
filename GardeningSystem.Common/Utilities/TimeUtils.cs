using System;

namespace GardeningSystem.Common.Utilities {
    public static class TimeUtils {

        /// <summary>
        /// Gets set in Program.cs, when the application gets started.
        /// </summary>
        public static DateTime ApplicationStartTime;

        public static double GetUpTimeInMinutes() {
            return (GetCurrentTime() - ApplicationStartTime).TotalMinutes;
        }

        public static DateTime GetCurrentTime() => DateTime.Now;
    }
}
