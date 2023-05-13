namespace Bulky.DataAccess.Repository.IRepository;

public interface IUnitOfWork
{
    ICategoryRepository CategoryRepo { get; }
    void Save();
}