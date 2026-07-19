using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Interfaces
{
    public interface IPharmacyService
    {
        Task<List<Medicine>> SearchMedicinesAsync(string? keyword);
        Task<Medicine?> GetActiveMedicineAsync(int id);
        Task<Medicine?> GetMedicineAsync(int id);
    }
}
