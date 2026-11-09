using TecFast.Application.Abstracciones.Repositorios;
using TecFast.Domain.Abstracciones;
using Microsoft.EntityFrameworkCore;

namespace TecFast.Infrastructure.Persistencia;

public class RepositorioBaseEf<TEntity> : IRepositorio<TEntity> where TEntity : Entidad
{
    protected ContextoTecFast Db { get; }

    public RepositorioBaseEf(ContextoTecFast db) => Db = db;

    public TEntity? GetById(Guid id) => Db.Set<TEntity>().Find(id);
    public void Add(TEntity entity) => Db.Set<TEntity>().Add(entity);
    public void Remove(TEntity entity) => Db.Set<TEntity>().Remove(entity);
    public void SaveChanges() => Db.SaveChanges();
}




