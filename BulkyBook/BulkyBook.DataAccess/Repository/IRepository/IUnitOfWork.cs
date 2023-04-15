﻿using BulkyBook.DataAccess.Data;

namespace BulkyBook.DataAccess.Repository.IRepository;

public interface IUnitOfWork
{
    ICategoryRepository CategoryRepository { get; }
    IProductRepository ProductRepository { get; }

    void Save();
}