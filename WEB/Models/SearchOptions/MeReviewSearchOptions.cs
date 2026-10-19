using System;

namespace Monic.Web.Models
{
    public class MeReviewSearchOptions : SearchOptions
    {
        public string q { get; set; }
        public string Domain { get; set; }
        public string Status { get; set; }
        public ReviewTargetType? TargetType { get; set; }
        public Guid? TargetEntityId { get; set; }
        public Guid? CreatedById { get; set; }
        public int? MinScore { get; set; }
        public int? MaxScore { get; set; }
    }
}
