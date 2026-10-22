namespace Monic.Web.Models
{
    public class CommentSearchOptions : SearchOptions
    {
        public string SubjectType { get; set; }

        public Guid? SubjectId { get; set; }

        public Guid? CreatedById { get; set; }

    }
}
