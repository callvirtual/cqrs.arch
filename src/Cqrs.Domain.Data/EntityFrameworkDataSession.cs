using System.Threading;
using System.Threading.Tasks;
using Cqrs.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Cqrs.Domain.Data
{
    public class EntityFrameworkDataSession : IDataSession
    {
        private readonly ApplicationDbContext _dbContext;

        public EntityFrameworkDataSession(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
            
            if (!_dbContext.Database.IsInMemory())
                _dbContext.Database.BeginTransaction();
        }

        /// <inheritdoc />
        public async Task CommitAsync(CancellationToken cancellation)
        {
            if (_dbContext.Database.IsInMemory())
                return;
            
            if (_dbContext.Database.CurrentTransaction != null)
            {
                await _dbContext.SaveChangesAsync(cancellation);
                _dbContext.Database.CommitTransaction();
            }
        }

        /// <inheritdoc />
        public Task RollbackAsync(CancellationToken cancellation)
        {
            if (_dbContext.Database.IsInMemory())
                return Task.CompletedTask;
            
            if (_dbContext.Database.CurrentTransaction != null)
                _dbContext.Database.RollbackTransaction();

            return Task.CompletedTask;
        }
        
        /// <inheritdoc />
        public void Dispose()
        {
            if (_dbContext.Database.CurrentTransaction != null)
                _dbContext.Database.RollbackTransaction();
        }
    }
}