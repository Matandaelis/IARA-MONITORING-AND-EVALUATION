namespace Monic.Web.Models
{
    public class BudgetLineSearchOptions : SearchOptions
    {
        public Guid? ProjectId { get; set; }

        public Guid? BudgetCategoryId { get; set; }

        public string q { get; set; }

    }
}
