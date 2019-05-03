using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Generic.Repository.Extension.Filter;
using Generic.Repository.Extension.Page;
using Generic.Repository.Extension.Validation;
using Generic.Repository.Models.Filter;
using Generic.Repository.Models.Page;
using Generic.Repository.Models.Page.PageConfig;
using Microsoft.EntityFrameworkCore;

namespace Generic.Repository.Repository
{
    public class BaseRepositoryAsync<TValue, TFilter> : IBaseRepositoryAsync<TValue, TFilter>
    where TValue : class
    where TFilter : class, IFilter
    {
        #region Attr
        public IEnumerable<string> includesString { get; set; }
        public IEnumerable<Expression<Func<TValue, object>>> includesExp { get; set; }
        protected readonly DbContext _context;
        protected readonly bool _useCommit;

        #endregion

        #region Ctor
        protected BaseRepositoryAsync(DbContext context)
        {
            _context = context;
        }

        protected BaseRepositoryAsync(DbContext context, bool useCommit)
        {
            _useCommit = useCommit;
            _context = context;
        }

        #endregion

        #region QUERY
        public virtual async Task<IReadOnlyList<TValue>> GetAllAsync(bool EnableAsNoTracking) => await GetAllQueryable(EnableAsNoTracking).ToListAsync();

        public virtual async Task<IReadOnlyList<TValue>> GetAllByAsync(Expression<Func<TValue, bool>> predicate, bool EnableAsNoTracking) => predicate != null ?
            await GetAllQueryable(EnableAsNoTracking).Where(predicate).ToListAsync() : await GetAllQueryable(EnableAsNoTracking).ToListAsync();

        public virtual async Task<IReadOnlyList<TValue>> FilterAllAsync(TFilter filter, bool EnableAsNoTracking) => await GetAllByAsync(filter.GenerateLambda<TValue, TFilter>(), EnableAsNoTracking);

        public virtual async Task<TValue> GetByAsync(Expression<Func<TValue, bool>> predicate, bool EnableAsNoTracking) => !predicate.IsNull(nameof(GetByAsync), nameof(predicate)) ? await GetAllQueryable(EnableAsNoTracking).SingleOrDefaultAsync(predicate) : null;

        public virtual async Task<IPage<TValue>> GetPageAsync(IPageConfig config, bool EnableAsNoTracking) => await Task.Run(() => GetAllQueryable(EnableAsNoTracking).ToPage<TValue>(config));

        #endregion

        #region COMMAND - (CREAT, UPDATE, DELETE) With CancellationToken
        public virtual async Task<TValue> CreateAsync(TValue entity, CancellationToken token)
        {
            entity.IsNull(nameof(CreateAsync), nameof(entity));
            SetState(EntityState.Added, entity);
            if (!_useCommit)
            {
                await CommitAsync(token).ConfigureAwait(false);
            }
            return entity;
        }

        public virtual async Task CreateAsync(IEnumerable<TValue> entityList, CancellationToken token)
        {
            entityList.IsNull(nameof(CreateAsync), nameof(entityList));
            await _context.AddRangeAsync(entityList);
            if (!_useCommit)
            {
                await CommitAsync(token).ConfigureAwait(false);
            }
        }

        public virtual async Task UpdateAsync(TValue entity, CancellationToken token)
        {
            entity.IsNull(nameof(UpdateAsync), nameof(entity));
            SetState(EntityState.Modified, entity);
            if (!_useCommit)
            {
                await CommitAsync(token).ConfigureAwait(false);
            }
        }

        public virtual async Task UpdateAsync(IEnumerable<TValue> entityList, CancellationToken token)
        {
            entityList.IsNull(nameof(UpdateAsync), nameof(entityList));
            _context.UpdateRange(entityList);
            if (!_useCommit)
            {
                await CommitAsync(token).ConfigureAwait(false);
            }
        }

        public virtual async Task DeleteAsync(TValue entity, CancellationToken token)
        {
            entity.IsNull(nameof(DeleteAsync), nameof(entity));
            _context.Remove(entity);
            if (!_useCommit)
            {
                await CommitAsync(token).ConfigureAwait(false);
            }
        }

        public virtual async Task DeleteAsync(IEnumerable<TValue> entityList, CancellationToken token)
        {
            entityList.IsNull(nameof(DeleteAsync), nameof(entityList));
            _context.RemoveRange(entityList);
            if (!_useCommit)
            {
                await CommitAsync(token).ConfigureAwait(false);
            }
        }
        #endregion

        #region COMMAND - (CREAT, UPDATE, DELETE) Without CancellationToken
        public virtual async Task<TValue> CreateAsync(TValue entity) => await CreateAsync(entity, default(CancellationToken)).ConfigureAwait(false);

        public virtual async Task CreateAsync(IEnumerable<TValue> entityList) => await CreateAsync(entityList, default(CancellationToken)).ConfigureAwait(false);

        public virtual async Task UpdateAsync(TValue entity) => await UpdateAsync(entity, default(CancellationToken)).ConfigureAwait(false);

        public virtual async Task UpdateAsync(IEnumerable<TValue> entityList) => await UpdateAsync(entityList, default(CancellationToken)).ConfigureAwait(false);

        public virtual async Task DeleteAsync(TValue entity) => await DeleteAsync(entity, default(CancellationToken)).ConfigureAwait(false);

        public virtual async Task DeleteAsync(IEnumerable<TValue> entityList) => await DeleteAsync(entityList, default(CancellationToken)).ConfigureAwait(false);
        #endregion

        #region COMMIT
        public Task CommitAsync() => CommitAsync(default(CancellationToken));

        public Task CommitAsync(CancellationToken cancellationToken) => _context.SaveChangesAsync(cancellationToken);
        #endregion

        #region Protected Methods
        protected IQueryable<TValue> SetIncludes(IQueryable<TValue> query) => includesString != null && includesString.Any() ?
                includesString.Aggregate(query, (current, include) => current.Include(include)) : includesExp != null && includesExp.Any() ?
                includesExp.Aggregate(query, (current, include) => current.Include(include)) : query;

        protected IQueryable<TValue> GetAllQueryable(bool EnableAsNoTracking)
        {
            var query = SetIncludes(_context.Set<TValue>());
            if (EnableAsNoTracking)
            {
                query = query.AsNoTracking();
            }
            return query;
        }

        protected void SetState(EntityState state, TValue item) => _context.Attach(item).State = state;
        #endregion
    }

    public class BaseRepositoryAsync<TValue, TResult, TFilter> : BaseRepositoryAsync<TValue, TFilter>, IBaseRepositoryAsync<TValue, TResult, TFilter>
    where TValue : class
    where TResult : class
    where TFilter : class, IFilter
    {
        #region Ctor
        protected BaseRepositoryAsync(DbContext context, Func<IEnumerable<TValue>, IEnumerable<TResult>> mapperList, Func<TValue, TResult> mapperData) : base(context)
        {
            if (!mapperList.IsNull(nameof(BaseRepositoryAsync<TValue, TResult, TFilter>), nameof(mapperList)) && !mapperData.IsNull(nameof(BaseRepositoryAsync<TValue, TResult, TFilter>), nameof(mapperData)))
            {
                this.mapperList = mapperList;
                this.mapperData = mapperData;
            }
        }
        protected BaseRepositoryAsync(DbContext context, bool useCommit, Func<IEnumerable<TValue>, IEnumerable<TResult>> mapperList, Func<TValue, TResult> mapperData) : base(context, useCommit)
        {
            if (!mapperList.IsNull(nameof(BaseRepositoryAsync<TValue, TResult, TFilter>), nameof(mapperList)) && !mapperData.IsNull(nameof(BaseRepositoryAsync<TValue, TResult, TFilter>), nameof(mapperData)))
            {
                this.mapperList = mapperList;
                this.mapperData = mapperData;
            }
        }

        public Func<IEnumerable<TValue>, IEnumerable<TResult>> mapperList { get; set; }
        public Func<TValue, TResult> mapperData { get; set; }

        #region QUERY
        public new virtual async Task<IReadOnlyList<TResult>> GetAllAsync(bool EnableAsNoTracking) => mapperList(await GetAllQueryable(EnableAsNoTracking).ToListAsync()).ToList();

        public new virtual async Task<IReadOnlyList<TResult>> GetAllByAsync(Expression<Func<TValue, bool>> predicate, bool EnableAsNoTracking) => predicate != null ?
             mapperList(await GetAllQueryable(EnableAsNoTracking).Where(predicate).ToListAsync()).ToList() : mapperList(await GetAllQueryable(EnableAsNoTracking).ToListAsync()).ToList();

        public new virtual async Task<IReadOnlyList<TResult>> FilterAllAsync(TFilter filter, bool EnableAsNoTracking) => await GetAllByAsync(filter.GenerateLambda<TValue, TFilter>(), EnableAsNoTracking);

        public new virtual async Task<TResult> GetByAsync(Expression<Func<TValue, bool>> predicate, bool EnableAsNoTracking) => !predicate.IsNull(nameof(GetByAsync), nameof(predicate)) ? mapperData(await GetAllQueryable(EnableAsNoTracking).SingleOrDefaultAsync(predicate)) : null;

        public new virtual async Task<IPage<TResult>> GetPageAsync(IPageConfig config, bool EnableAsNoTracking) => await Task.Run(() => GetAllQueryable(EnableAsNoTracking).ToPage<TValue, TResult>(mapperList, config));

        #endregion
    }
}