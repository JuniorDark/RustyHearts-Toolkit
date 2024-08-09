namespace RHToolkit.Utilities
{
    public class DateTimeFormatter
    {
        public static string FormatRemainTime(int remainTime)
        {
            if (remainTime == -1)
            {
                return "Unlimited";
            }
            else
            {
                int days = remainTime / (24 * 60);
                //int hours = remainTime % (24 * 60) / 60;
                //int minutes = remainTime % 60;

                return $"{days} Days";
            }
        }

        public static string FormatExpireTime(int expireTime)
        {
            if (expireTime == 0)
            {
                return "Never";
            }
            else
            {
                DateTime epochTime = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                DateTime expireDateTime = epochTime.AddSeconds(expireTime);
                return expireDateTime.ToString("MMMM dd, yyyy hh:mm:ss tt");
            }
        }

        public static DateTime ConvertFromEpoch(int epochTime)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(epochTime);
        }

        public static DateTime ConvertIntToDate(int dateInt)
        {
            int year = dateInt / 10000;
            int month = (dateInt % 10000) / 100;
            int day = dateInt % 100;

            return new DateTime(year, month, day);
        }

        public static int ConvertDateToInt(DateTime date)
        {
            return date.Year * 10000 + date.Month * 100 + date.Day;
        }

        public static string FormatMinutesToDate(double minutes)
        {
            const double minutesInDay = 1440;
            const double minutesInHour = 60;

            double days = Math.Floor(minutes / minutesInDay);
            minutes %= minutesInDay;

            double hours = Math.Floor(minutes / minutesInHour);
            minutes %= minutesInHour;

            string formattedTime = string.Empty;
            if (days > 0)
                formattedTime += $"{days:F1} Day(s) ";
            if (hours > 0)
                formattedTime += $"{hours:F1} Hour(s) ";
            if (minutes > 0 || formattedTime == string.Empty)
                formattedTime += $"{minutes:F1} Minute(s)";

            return formattedTime.Trim();
        }
    }
}
