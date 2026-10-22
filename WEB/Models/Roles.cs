namespace Monic.Web.Models
{
    /// <summary>
    /// System roles. Workflow alignment (M&E business logic):
    /// DataEntry → Submit · Verifier → Verify · Approver → Approve · Oversight → read-all + audit ·
    /// Manager → design + full workflow fallback · Finance → budget/disbursement management ·
    /// Questionnaires/Folders are module capability flags. Administrator bypasses all checks.
    /// </summary>
    public enum Roles
    {
        Administrator,
        Manager,
        Oversight,
        Questionnaires,
        Folders,
        DataEntry,
        Verifier,
        Approver,
        Finance
    }
}
