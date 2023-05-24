using BulkyBook.DataAccess.Data;

namespace BulkyBook.DataAccess.Repository.IRepository;

public interface IUnitOfWork
{
    ICategoryRepository CategoryRepository { get; }
    IProductRepository ProductRepository { get; }
    ICompanyRepository CompanyRepository { get; }
    IShoppingCartRepository ShoppingCartRepository { get; }
    IOrderHeaderRepository OrderHeaderRepository { get; }
    IOrderDetailRepository OrderDetailRepository { get; }
    IApplicationUserRepository ApplicationUserRepository { get; }

    void Save();
}