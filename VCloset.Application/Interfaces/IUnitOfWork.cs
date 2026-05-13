using System;
using System.Threading.Tasks;
using VCloset.Domain.Entities;

namespace VCloset.Application.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IGenericRepository<User> Users { get; }
    Task<int> SaveChangesAsync();
}
