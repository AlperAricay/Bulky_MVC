using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;

namespace Bulky.DataAccess.Repository;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _db;
    public ICategoryRepository CategoryRepo { get; }
    public IProductRepository ProductRepo { get; }
    public ICompanyRepository CompanyRepo { get; }
    public IShoppingCartRepository ShoppingCartRepo { get; }
    public IApplicationUserRepository ApplicationUserRepo { get; }
    public IOrderHeaderRepository OrderHeaderRepo { get; }
    public IOrderDetailRepository OrderDetailRepo { get; }

    public UnitOfWork(ApplicationDbContext db)
    {
        _db = db;
        CategoryRepo = new CategoryRepository(_db);
        ProductRepo = new ProductRepository(_db);
        CompanyRepo = new CompanyRepository(_db);
        ShoppingCartRepo = new ShoppingCartRepository(_db);
        ApplicationUserRepo = new ApplicationUserRepository(_db);
        OrderHeaderRepo = new OrderHeaderRepository(_db);
        OrderDetailRepo = new OrderDetailRepository(_db);
    }

    public void Save()
    {
        _db.SaveChanges();
    }
}