namespace Outlet.Kernel.Shared.Providers;

/// <summary>
/// UNIT OF WORK PATTERN (Secondary Port)
///
/// The Unit of Work pattern:
/// 1. Maintains a list of objects affected by a business transaction
/// 2. Coordinates writing out changes as a single atomic operation
/// 3. Ensures data consistency across multiple repository operations
///
/// WHY UNIT OF WORK?
/// Without it, each repository.Save() would commit immediately:
///   await itemRepo.SaveAsync(item);        // Commit 1
///   await lockfileRepo.SaveAsync(lock);    // Commit 2 (what if this fails?)
///
/// With Unit of Work, all changes commit together:
///   await unitOfWork.ExecuteAsync(async () => {
///       await itemRepo.SaveAsync(item);
///       await lockfileRepo.SaveAsync(lock);
///   });  // Single atomic commit
///
/// CLEAN ARCHITECTURE PERSPECTIVE:
/// - This interface is a SECONDARY PORT (driven port)
/// - It lives in the Hexagon (business logic layer)
/// - The actual implementation is a SECONDARY ADAPTER
/// - Use Cases depend on this interface, not on concrete implementations
///
/// This follows the DEPENDENCY INVERSION PRINCIPLE:
/// High-level modules (Use Cases) don't depend on low-level modules (EF Core,
/// file system transactions); both depend on abstractions (this interface).
///
/// NOTE: Outlet v1 has no database. This port exists so any future persistence
/// (lockfile writes, multi-file installs) is transactional by design.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Executes a unit of work that doesn't return a value.
    /// All operations within the work delegate are committed atomically.
    /// </summary>
    Task ExecuteAsync(Func<Task> work, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a unit of work that returns a value.
    /// All operations within the work delegate are committed atomically.
    /// </summary>
    Task<T> ExecuteAsync<T>(Func<Task<T>> work, CancellationToken cancellationToken = default);
}
