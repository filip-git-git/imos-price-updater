using IMOS.PriceUpdater.Models;

namespace IMOS.PriceUpdater.Services;

/// <summary>
///     Service for managing execution history persistence.
/// </summary>
public interface IHistoryService
{
    /// <summary>
    ///     Gets the execution history filtered by date range and outcome.
    /// </summary>
    /// <param name="fromDate">Optional start date filter.</param>
    /// <param name="toDate">Optional end date filter.</param>
    /// <param name="outcome">Optional outcome filter.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of execution history records.</returns>
    Task<List<ExecutionHistory>> GetHistoryAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        ExecutionOutcome? outcome = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets a specific execution history record by ID.
    /// </summary>
    /// <param name="id">The execution history ID.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The execution history record or null if not found.</returns>
    Task<ExecutionHistory?> GetHistoryByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the detailed results for a specific execution.
    /// </summary>
    /// <param name="executionId">The execution ID.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of execution history details.</returns>
    Task<List<ExecutionHistoryDetail>> GetHistoryDetailsAsync(Guid executionId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Saves a new execution history record.
    /// </summary>
    /// <param name="history">The execution history to save.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task SaveExecutionAsync(ExecutionHistory history, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Saves the details for a specific execution.
    /// </summary>
    /// <param name="executionId">The execution ID.</param>
    /// <param name="details">The details to save.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task SaveExecutionDetailsAsync(
        Guid executionId,
        List<ExecutionHistoryDetail> details,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deletes an execution history entry.
    /// </summary>
    /// <param name="id">The execution history ID to delete.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task DeleteHistoryEntryAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the total count of history entries.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The total count.</returns>
    Task<int> GetHistoryCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Cleans up old history entries, keeping only the most recent ones.
    /// </summary>
    /// <param name="keepCount">Number of entries to keep.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task CleanupOldHistoryAsync(int keepCount, CancellationToken cancellationToken = default);
}
