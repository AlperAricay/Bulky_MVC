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
    
    public IEnumerable<T> GetAll()
    {
        return _dbSet;
    }

    public T? Get(Expression<Func<T, bool>> filter)
    {
        var result = _dbSet.Where(filter);
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