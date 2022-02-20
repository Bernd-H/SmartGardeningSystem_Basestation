using System;
using System.Threading;
using System.Threading.Tasks;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.DataObjects;
using NLog;

namespace GardeningSystem.DataAccess.Database {

    /// <summary>
    /// Base class of all database repositories.
    /// </summary>
    /// <typeparam name="T">Data object of the repository.</typeparam>
    public abstract class DbBaseRepository<T> : IDisposable where T : class, IDO {

        protected DatabaseContext context { get; private set; }

        /// <summary>
        /// SemaphoreSlim to lock concurrent table accesses.
        /// </summary>
        protected static SemaphoreSlim LOCKER = new SemaphoreSlim(1);

        private ILogger Logger;

        public DbBaseRepository(ILoggerService loggerService, IDatabaseContext databaseContext) {
            Logger = loggerService.GetLogger<DbBaseRepository<T>>();
            context = databaseContext as DatabaseContext;
        }

        /// <summary>
        /// Adds a new object to the table with the type <typeparamref name="T"/>..
        /// </summary>
        /// <param name="o">Object to add.</param>
        /// <param name="cancellationToken">A System.Threading.CancellationToken to stop the asynchron task, when requested.</param>
        /// <returns>A task that represents the asynchronous add operation. The task result contains
        /// the number of state entries written to the database.</returns>
        protected async Task<int> AddToTable(T o, CancellationToken cancellationToken = default) {
            await LOCKER.WaitAsync();
            Logger.Trace($"[AddToTable]Adding object with id {o.Id} to table {typeof(T).Name}.");

            context.Add(o);
            int numberOfWrittenStateEntries = await context.SaveChangesAsync(cancellationToken);

            LOCKER.Release();
            return numberOfWrittenStateEntries;
        }

        /// <summary>
        /// Removes a object from the table with the type <typeparamref name="T"/>..
        /// </summary>
        /// <param name="o">Object to remove.</param>
        /// <param name="cancellationToken">A System.Threading.CancellationToken to stop the asynchron task, when requested.</param>
        /// <returns>A task that represents the asynchronous remove operation. The task result contains
        /// the number of state entries written to the database.</returns>
        protected async Task<int> RemoveFromTable(T o, CancellationToken cancellationToken = default) {
            await LOCKER.WaitAsync();
            Logger.Trace($"[RemoveFromTable]Removing object with id {o.Id} from table {nameof(T)}.");

            context.Remove(o);
            int numberOfWrittenStateEntries = await context.SaveChangesAsync(cancellationToken);
            
            LOCKER.Release();
            return numberOfWrittenStateEntries;
        }

        /// <summary>
        /// Updates a existing object from the table with the type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="o">Updated object.</param>
        /// <param name="cancellationToken">A System.Threading.CancellationToken to stop the asynchron task, when requested.</param>
        /// <returns>A task that represents the asynchronous update operation. The task result contains
        /// a boolean that is true when an object (with the same Id as the updated object) got successfully updated.</returns>
        protected async Task<bool> UpdateObject(T o, CancellationToken cancellationToken = default) {
            await LOCKER.WaitAsync();
            Logger.Trace($"[RemoveFromTable]Removing object with id {o.Id} from table {nameof(T)}.");
            int numberOfWrittenStateEntries = 0;

            var entity = context.Find(typeof(T), o.Id);
            if (entity != null) {
                // udpate
                context.Entry(entity).CurrentValues.SetValues(o);

                numberOfWrittenStateEntries = await context.SaveChangesAsync(cancellationToken);
            }

            LOCKER.Release();
            return numberOfWrittenStateEntries == 1;
        }

        /// <inheritdoc/>
        public void Dispose() {
            context?.Dispose();
        }
    }
}
