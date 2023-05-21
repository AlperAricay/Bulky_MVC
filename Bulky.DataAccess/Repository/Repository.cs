using System.Linq.Expressions;
using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Microsoft.EntityFrameworkCore;

namespace Bulky.DataAccess.Repository;

public class Repository<T> : IRepository<T> where T : class
{
    private readonly ApplicationDbContext _db;
    private readonly DbSet<T> _dbSet;

    public Repository(ApplicationDbContext db)
    {
        _db = db;
        _dbSet = _db.Set<T>();
    }
    
    public IEnumerable<T> GetAll(Expression<Func<T, bool>>? filter, string? includeProperties = null)
    {
        IQueryable<T> result = _dbSet;
        if (filter != null) result = result.Where(filter);

        if (!string.IsNullOrEmpty(includeProperties))
        {
            foreach (var includeProperty in includeProperties
                         .Split(new char[] {','},StringSplitOptions.RemoveEmptyEntries))
            {
                result = result.Include(includeProperty);
            }
        }
        return result;
    }

    public T? Get(Expression<Func<T, bool>> filter, string? includeProperties = null, bool tracked = false)
    {
        var result = tracked ? _dbSet : _dbSet.AsNoTracking(); //Prevents from updating automatically

        result = result.Where(filter);
        if (!string.IsNullOrEmpty(includeProperties))
        {
            foreach (var includeProperty in includeProperties
                         .Split(new char[] {','},StringSplitOptions.RemoveEmptyEntries))
            {
                result = result.Include(includeProperty);
            }
        }
        return result.FirstOrDefault();
    }

    public void Add(T entity)
    {
        _dbSet.Add(entity);
    }

    public void Remove(T entity)
    {
        _dbSet.Remove(entity);
    }

    public void Remove(IEnumerable<T> entity)
    {
        _dbSet.RemoveRange(entity);
    }
}