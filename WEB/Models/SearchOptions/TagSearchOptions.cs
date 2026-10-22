namespace Monic.Web.Models
{
    public class TagSearchOptions : SearchOptions
    {
        public string q { get; set; }

        public Guid? DocumentId { get; set; }

    }
}
