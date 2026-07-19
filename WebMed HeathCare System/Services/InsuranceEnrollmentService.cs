using Microsoft.EntityFrameworkCore;
using WebMed_HeathCare_System.Interfaces;
using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Services
{
    public class InsuranceEnrollmentService : IInsuranceEnrollmentService
    {
        private readonly WebMedDbContext _context;

        public InsuranceEnrollmentService(WebMedDbContext context)
        {
            _context = context;
        }

        public async Task<PatientInsurance?> GetActiveInsuranceAsync(int patientId, int planId)
        {
            return await _context.PatientInsurances
                .FirstOrDefaultAsync(pi => pi.PatientId == patientId && pi.PlanId == planId && pi.Status == "Active");
        }

        public async Task<PatientInsurance?> GetPendingInsuranceAsync(int patientId, int planId)
        {
            return await _context.PatientInsurances
                .FirstOrDefaultAsync(pi => pi.PatientId == patientId && pi.PlanId == planId && pi.Status == "PendingPayment");
        }

        public async Task<PatientInsurance> CreatePendingEnrollmentAsync(int patientId, InsurancePlan plan, Pricing pricing, string? paymentMethod)
        {
            var patientInsurance = new PatientInsurance
            {
                PatientId = patientId,
                PlanId = plan.PlanId,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddMonths(plan.DurationMonths),
                Status = "PendingPayment"
            };

            _context.PatientInsurances.Add(patientInsurance);
            await _context.SaveChangesAsync();

            var payment = new Payment
            {
                UserId = patientId,
                Amount = pricing.Premium,
                PaymentType = "Insurance",
                PaymentMethod = paymentMethod ?? "CreditCard",
                TransactionReference = null,
                PaymentStatus = "Pending",
                AssociatedId = patientInsurance.PatientInsuranceId,
                PaidAt = DateTime.Now
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            return patientInsurance;
        }

        public async Task<PatientInsurance?> GetPatientInsuranceAsync(int patientInsuranceId)
        {
            return await _context.PatientInsurances
                .Include(pi => pi.Plan)
                .FirstOrDefaultAsync(pi => pi.PatientInsuranceId == patientInsuranceId);
        }

        public async Task<Payment?> GetInsurancePaymentAsync(int patientInsuranceId)
        {
            return await _context.Payments
                .FirstOrDefaultAsync(p => p.PaymentType == "Insurance" && p.AssociatedId == patientInsuranceId);
        }

        public async Task<bool> CompleteInsurancePaymentAsync(int patientInsuranceId, int patientId)
        {
            var patientInsurance = await _context.PatientInsurances.FindAsync(patientInsuranceId);
            if (patientInsurance == null || patientInsurance.PatientId != patientId)
            {
                return false;
            }

            var payment = await GetInsurancePaymentAsync(patientInsuranceId);
            if (payment == null)
            {
                return false;
            }

            patientInsurance.Status = "Active";
            payment.PaymentStatus = "Completed";
            payment.TransactionReference = "TXN-INS-" + new Random().Next(100000, 999999);
            payment.PaidAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> FailInsurancePaymentAsync(int patientInsuranceId, int patientId)
        {
            var patientInsurance = await _context.PatientInsurances.FindAsync(patientInsuranceId);
            if (patientInsurance == null || patientInsurance.PatientId != patientId)
            {
                return false;
            }

            var payment = await GetInsurancePaymentAsync(patientInsuranceId);
            if (payment == null)
            {
                return false;
            }

            payment.PaymentStatus = "Failed";
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
