using Microsoft.EntityFrameworkCore;
using WebMed_HeathCare_System.Interfaces;
using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Services
{
    public class PharmacyService : IPharmacyService
    {
        private readonly WebMedDbContext _context;

        public PharmacyService(WebMedDbContext context)
        {
            _context = context;
        }

        public async Task<List<Medicine>> SearchMedicinesAsync(string? keyword)
        {
            var query = _context.Medicines.Where(m => m.IsActive);

            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(m => m.Name.Contains(keyword) || m.Category.Contains(keyword) || m.Description!.Contains(keyword));
            }

            return await query.ToListAsync();
        }

        public async Task<Medicine?> GetActiveMedicineAsync(int id)
        {
            return await _context.Medicines.FirstOrDefaultAsync(m => m.MedicineId == id && m.IsActive);
        }

        public async Task<Medicine?> GetMedicineAsync(int id)
        {
            return await _context.Medicines.FindAsync(id);
        }
    }
}
