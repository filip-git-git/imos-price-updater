namespace IMOS.PriceUpdater.Models;

/// <summary>
///     Represents the outcome of an execution.
/// </summary>
public enum ExecutionOutcome
{
    /// <summary>
    ///     The execution completed successfully.
    /// </summary>
    Success,

    /// <summary>
    ///     The execution failed.
    /// </summary>
    Failed,

    /// <summary>
    ///     The execution was cancelled.
    /// </summary>
    Cancelled
}

/// <summary>
///     Represents the status of an individual row update.
/// </summary>
public enum ExecutionStatus
{
    /// <summary>
    ///     The row was successfully updated.
    /// </summary>
    Updated,

    /// <summary>
    ///     The row was skipped (not found in database).
    /// </summary>
    Skipped,

    /// <summary>
    ///     The row update resulted in an error.
    /// </summary>
    Error
}
