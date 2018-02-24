using System;

namespace Blogifier.Core.Extensions
{
    public static class DateTimeExtensions
    {
        public static string ToFriendlyDateTimeString(this DateTime date)
        {
            return FriendlyDate(date) + " @ " + date.ToString("t").ToLower();
        }

        public static string ToFriendlyDateString(this DateTime date)
        {
            return FriendlyDate(date);
        }

        static string FriendlyDate(DateTime date)
        {
            string formattedDate = "";
            if (date.Date == DateTime.Today)
            {
                formattedDate = "Today";
            }
            else if (date.Date == DateTime.Today.AddDays(-1))
            {
                formattedDate = "Yesterday";
            }
            else if (date.Date > DateTime.Today.AddDays(-6))
            {
                // *** Show the Day of the week
                formattedDate = date.ToString("dddd").ToString();
            }
            else
            {
                formattedDate = date.ToString("MMMM dd, yyyy");
            }
            return formattedDate;
        }
    }
}
