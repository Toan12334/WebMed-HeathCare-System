using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Interfaces
{
    public interface IInsuranceEnrollmentService
    {
        Task<PatientInsurance?> GetActiveInsuranceAsync(int patientId, int planId);
        Task<PatientInsurance?> GetPendingInsuranceAsync(int patientId, int planId);
        Task<PatientInsurance> CreatePendingEnrollmentAsync(int patientId, InsurancePlan plan, Pricing pricing, string? paymentMethod);
        Task<PatientInsurance?> GetPatientInsuranceAsync(int patientInsuranceId);
        Task<Payment?> GetInsurancePaymentAsync(int patientInsuranceId);
        Task<bool> CompleteInsurancePaymentAsync(int patientInsuranceId, int patientId);
        Task<bool> FailInsurancePaymentAsync(int patientInsuranceId, int patientId);
    }
}
