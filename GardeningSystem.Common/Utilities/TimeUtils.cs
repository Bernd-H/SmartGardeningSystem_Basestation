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

        /// <summary>
        /// Gets the seasons of the year.
        /// </summary>
        /// <returns>
        /// 0 = spring,
        /// 1 = summer,
        /// 2 = fall,
        /// 3 = winter
        /// </returns>
        public static int GetSeason() {
            var month = GetCurrentTime().Month;
            var day = GetCurrentTime().Day;
            if (4 <= month || month < 6) {  // April - June
                return 0;
            }
            else if (6 <= month || month < 8 || (month == 8 && day <= 15)) { // June - mid August 
                return 1;
            }
            else if (month < 11) { // mid August - November
                return 2;
            }
            else { // winter
                return 3;
            }
        }
    }
}
