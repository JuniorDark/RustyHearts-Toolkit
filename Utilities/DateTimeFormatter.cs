namespace RHToolkit.Utilities
{
    public class DateTimeFormatter
    {
        public static string FormatRemainTime(int remainTime)
        {
            if (remainTime == -1)
            {
                return Resources.Unlimited;
            }
            else
            {
                int days = remainTime / (24 * 60);
                //int hours = remainTime % (24 * 60) / 60;
                //int minutes = remainTime % 60;

                return string.Format(Resources.FormattedDays, days);
            }
        }

        public static string FormatExpireTime(int expireTime)
        {
            if (expireTime == 0)
            {
                return Resources.Never;
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
                formattedTime += string.Format(Resources.FormattedDays, $"{days:F1}");
            if (hours > 0)
                formattedTime += string.Format(Resources.FormattedHours, $"{hours:F1}");
            if (minutes > 0 || formattedTime == string.Empty)
                formattedTime += string.Format(Resources.FormattedMinutes, $"{minutes:F1}");

            return formattedTime.Trim();
        }
    }
}
