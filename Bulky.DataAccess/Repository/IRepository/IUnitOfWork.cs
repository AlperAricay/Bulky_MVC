namespace Bulky.DataAccess.Repository.IRepository;

public interface IUnitOfWork
{
    ICategoryRepository CategoryRepo { get; }
    IProductRepository ProductRepo { get; }
    ICompanyRepository CompanyRepo { get; }
    IShoppingCartRepository ShoppingCartRepo { get; }
    IApplicationUserRepository ApplicationUserRepo { get; }
    void Save();
}