namespace Bulky.DataAccess.Repository.IRepository;

public interface IUnitOfWork
{
    ICategoryRepository CategoryRepo { get; }
    IProductRepository ProductRepo { get; }
    ICompanyRepository CompanyRepo { get; }
    IShoppingCartRepository ShoppingCartRepo { get; }
    IApplicationUserRepository ApplicationUserRepo { get; }
    IOrderHeaderRepository OrderHeaderRepo { get; }
    IOrderDetailRepository OrderDetailRepo { get; }
    void Save();
}