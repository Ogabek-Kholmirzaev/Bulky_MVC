using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;

namespace BulkyBook.DataAccess.Repository;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _db;
    public ICategoryRepository CategoryRepository { get; }
    public IProductRepository ProductRepository { get; }
    public ICompanyRepository CompanyRepository { get; }
    public IShoppingCartRepository ShoppingCartRepository { get; }

    public UnitOfWork(ApplicationDbContext db)
    {
        _db = db;
        CategoryRepository = new CategoryRepository(_db);
        ProductRepository = new ProductRepository(_db);
        CompanyRepository = new CompanyRepository(_db);
        ShoppingCartRepository = new ShoppingCartRepository(_db);
    }

    public void Save()
    {
        _db.SaveChanges();
    }
}