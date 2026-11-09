using TecFast.Domain.Abstracciones;

namespace TecFast.Application.Abstracciones.Repositorios;

public interface IRepositorio<TEntity> where TEntity : Entidad
{
    TEntity? GetById(Guid id);
    void Add(TEntity entity);
    void Remove(TEntity entity);
    void SaveChanges();
}




