using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.DataAccess.Repository
{
    public class ShoppingCartRepository : Repository<ShoppingCart>, IShoppingCart
    {
        private readonly ApplicationDbContext _db;
        public ShoppingCartRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
            _db = dbContext;
        }
        public void Update(ShoppingCart obj)
        {
            // Update the ShoppingCart object in the database
            _db.ShoppingCarts.Update(obj);
            // Save changes to the database
            _db.SaveChanges();
        }
    }
}
