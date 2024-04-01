namespace RHGMTool.Utilities
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
    }
}
