namespace ryvo_interview_be.Domain;

/// <summary>
/// Lifecycle of a <see cref="TimesheetWeek"/>:
/// Draft -> Submitted -> Approved, or Submitted -> Rejected -> (editable) Draft-like.
/// Editing is allowed in Draft and Rejected, and blocked in Submitted and Approved.
/// </summary>
public enum TimesheetStatus
{
    Draft,
    Submitted,
    Approved,
    Rejected
}
