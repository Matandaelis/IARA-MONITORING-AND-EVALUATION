using System;
using System.Globalization;
using Monic.Web.Models;

namespace Monic.Web.Code;

// Pure projector: maps the canonical string Answer.Value onto typed analytics columns.
// Never mutates Value; never throws. A value may populate more than one column only when
// parsing genuinely succeeds for both (e.g. "1" sets ValueNumber only, not ValueDate).
public static class AnswerValueProjector
{
    public static void Apply(Answer answer)
    {
        if (answer is null)
        {
            return;
        }

        var raw = answer.Value;

        if (string.IsNullOrWhiteSpace(raw))
        {
            answer.ValueNumber = null;
            answer.ValueDate = null;
            answer.ValueBoolean = null;
            return;
        }

        // Numbers: use invariant culture so "1.5" parses regardless of the client locale.
        if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
        {
            answer.ValueNumber = number;
            answer.ValueDate = null;
            answer.ValueBoolean = null;
            return;
        }

        // Booleans incl. yes/no.
        var lower = raw.Trim().ToLowerInvariant();
        if (lower is "true" or "false" or "yes" or "no")
        {
            answer.ValueNumber = null;
            answer.ValueDate = null;
            answer.ValueBoolean = lower is "true" or "yes";
            return;
        }

        // Dates: adjust to universal so a wall-clock string becomes a comparable instant.
        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal, out var date))
        {
            answer.ValueNumber = null;
            answer.ValueDate = date;
            answer.ValueBoolean = null;
            return;
        }

        // Anything else clears all typed columns; Value stays the canonical string.
        answer.ValueNumber = null;
        answer.ValueDate = null;
        answer.ValueBoolean = null;
    }
}
