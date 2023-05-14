using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;

namespace Bulky.DataAccess.Repository;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _db;
    public ICategoryRepository CategoryRepo { get; }
    public IProductRepository ProductRepo { get; }

    public UnitOfWork(ApplicationDbContext db)
    {
        _db = db;
        CategoryRepo = new CategoryRepository(_db);
        ProductRepo = new ProductRepository(_db);
    }

    public void Save()
    {
        _db.SaveChanges();
    }
}