using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
namespace Bulky.DataAccess.Repository
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly ApplicationDbContext _dbContext;
        internal DbSet<T> dbSet;
        public Repository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
            this.dbSet = _dbContext.Set<T>();
        }
        void IRepository<T>.Add(T entity)
        {
            dbSet.Add(entity);
        }

        T IRepository<T>.Get(Expression<Func<T, bool>> filter, string? includeProperties = null)
        {
            IQueryable<T> query = dbSet;
            query = query.Where(filter);
            if (!string.IsNullOrEmpty(includeProperties))
            {
                foreach (var includeProperty in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProperty);
                }
            }
            return query.FirstOrDefault();
        }

        IEnumerable<T> IRepository<T>.GetAll(Expression<Func<T, bool>>? filter=null,string? includeProperties = null)
        {
            IQueryable<T> query = dbSet;
            if (filter is not null)
            {
                query = query.Where(filter);
            }
            if (!string.IsNullOrEmpty(includeProperties))
            {
                foreach (var includeProperty in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProperty);
                }
            }
            return query.ToList();
        }

        void IRepository<T>.Remove(int id)
        {
            T entity = dbSet.Find(id);
            if (entity != null)
            {
                dbSet.Remove(entity);
            }
            else
            {
                throw new ArgumentException($"Entity with id {id} not found.");
            }

        }

        void IRepository<T>.Remove(T entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity), "Entity cannot be null.");
            }
            dbSet.Remove(entity);

        }

        void IRepository<T>.RemoveRange(IEnumerable<T> entity)
        {
            if (entity == null || !entity.Any())
            {
                throw new ArgumentNullException(nameof(entity), "Entity collection cannot be null or empty.");
            }
            dbSet.RemoveRange(entity);

        }

        void IRepository<T>.Save()
        {
            try
            {
                _dbContext.SaveChanges();
            }
            catch (DbUpdateException ex)
            {
                // Log the exception or handle it as needed
                throw new Exception("An error occurred while saving changes to the database.", ex);
            }
            catch (Exception ex)
            {
                // Handle other exceptions
                throw new Exception("An unexpected error occurred while saving changes.", ex);
            }
        }
    }
}
