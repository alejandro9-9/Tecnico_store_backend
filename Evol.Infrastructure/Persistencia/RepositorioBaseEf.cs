using Evol.Application.Abstracciones.Repositorios;
using Evol.Domain.Abstracciones;
using Microsoft.EntityFrameworkCore;

namespace Evol.Infrastructure.Persistencia;

public class RepositorioBaseEf<TEntity> : IRepositorio<TEntity> where TEntity : Entidad
{
    protected ContextoEvol Db { get; }

    public RepositorioBaseEf(ContextoEvol db) => Db = db;

    public TEntity? GetById(Guid id) => Db.Set<TEntity>().Find(id);
    public void Add(TEntity entity) => Db.Set<TEntity>().Add(entity);
    public void Remove(TEntity entity) => Db.Set<TEntity>().Remove(entity);
    public void SaveChanges() => Db.SaveChanges();
}




