namespace Monic.Web.Models
{
    public class NotificationSearchOptions : SearchOptions
    {
        public string q { get; set; }

        public bool? IsRead { get; set; }

        public string Type { get; set; }

    }
}
