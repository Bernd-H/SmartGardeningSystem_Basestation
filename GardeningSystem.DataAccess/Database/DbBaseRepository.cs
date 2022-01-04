using System;
using System.Threading;
using System.Threading.Tasks;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.DataObjects;
using NLog;

namespace GardeningSystem.DataAccess.Database {
    public abstract class DbBaseRepository<T> : IDisposable where T : class, IDO {

        protected DatabaseContext context { get; private set; }

        protected static SemaphoreSlim LOCKER = new SemaphoreSlim(1);

        private ILogger Logger;

        public DbBaseRepository(ILoggerService loggerService) {
            Logger = loggerService.GetLogger<DbBaseRepository<T>>();
            context = new DatabaseContext();
        }

        protected async Task<int> AddToTable(T o, CancellationToken cancellationToken = default) {
            await LOCKER.WaitAsync();
            Logger.Trace($"[AddToTable]Adding object with id {o.Id} to table {typeof(T).Name}.");

            context.Add(o);
            int numberOfWrittenStateEntries = await context.SaveChangesAsync(cancellationToken);

            LOCKER.Release();
            return numberOfWrittenStateEntries;
        }

        protected async Task<int> RemoveFromTable(T o, CancellationToken cancellationToken = default) {
            await LOCKER.WaitAsync();
            Logger.Trace($"[RemoveFromTable]Removing object with id {o.Id} from table {nameof(T)}.");

            context.Remove(o);
            int numberOfWrittenStateEntries = await context.SaveChangesAsync(cancellationToken);
            
            LOCKER.Release();
            return numberOfWrittenStateEntries;
        }

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

        public void Dispose() {
            context?.Dispose();
        }
    }
}
