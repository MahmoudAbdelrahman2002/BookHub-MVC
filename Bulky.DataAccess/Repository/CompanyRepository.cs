using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.DataAccess.Repository
{
    public class CompanyRepository : Repository<Company>, ICompanyRepository
    {
        // Constructor that takes ApplicationDbContext as a parameter
        public ApplicationDbContext _dbContext;
        public CompanyRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }

        void ICompanyRepository.Update(Company company)
        {
            var objFromDb = _dbContext.Companies.FirstOrDefault(s => s.Id == company.Id);
            if (objFromDb != null)
            {
                objFromDb.Name = company.Name;
                objFromDb.StreetAddress = company.StreetAddress;
                objFromDb.City = company.City;
                objFromDb.State = company.State;
                objFromDb.PostalCode = company.PostalCode;
                objFromDb.PhoneNumber = company.PhoneNumber;
                _dbContext.SaveChanges();
            }
            else
            {
                throw new Exception("Company not found");
            }


        }
    }
}
