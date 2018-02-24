using Blogifier.Core.Data.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Blogifier.Core.Data.Repositories
{
    public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        protected readonly DbSet<TEntity> Entities;

        public Repository(DbContext context)
        {
            this.Entities = context.Set<TEntity>();
        }

        public IEnumerable<TEntity> All()
        {
            return this.Entities.ToList();
        }

        public IEnumerable<TEntity> Find(Expression<Func<TEntity, bool>> predicate)
        {
            return this.Entities.Where(predicate);
        }

        public TEntity Single(Expression<Func<TEntity, bool>> predicate)
        {
            return this.Entities.SingleOrDefault(predicate);
        }

        public void Add(TEntity entity)
        {         
            this.Entities.Add(entity);
        }

        public void AddRange(IEnumerable<TEntity> entities)
        {
            this.Entities.AddRange(entities);
        }

        public void Remove(TEntity entity)
        {
            this.Entities.Remove(entity);
        }

        public void RemoveRange(IEnumerable<TEntity> entities)
        {
            this.Entities.RemoveRange(entities);
        }
    }
}
