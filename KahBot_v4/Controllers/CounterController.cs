using DataStore.EF.Data;
using Core.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KahBot_v4.Controllers
{
    public class CounterController : ICrudController
    {
        protected readonly KahBotDbContext _context;
        public CounterController(KahBotDbContext context)
        {
            _context = context;
        }


        public async Task<List<Counter>?> Get()
        {
            if (_context.Counters == null)
            {
                return null;
            }
            return await _context.Counters.ToListAsync();
        }
        public async Task<bool> Add(Counter counter)
        {
            if (_context.Counters == null)
            {
                return false;
            }
            _context.Counters.Add(counter);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> Put(Guid guid, Counter counter)
        {
            if (guid != counter.Guid)
            {
                return false;
            }

            _context.Entry(counter).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CounterExists(guid))
                {
                    return false;
                }
                else
                {
                    throw;
                }
            }

            return true;
        }



        private bool CounterExists(Guid guid)
        {
            return (_context.Counters?.Any(e => e.Guid == guid)).GetValueOrDefault();
        }
    }
}
