using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Interfaces
{
    public interface IConsultationService
    {
        Task<Appointment?> GetAppointmentForSessionAsync(int appointmentId);
        Task<List<object>> SearchMedicinesAsync(string keyword);
        Task<bool> SubmitConsultationAsync(int appointmentId, int doctorId, string? diagnosis, string? treatmentPlan, string? prescriptionJson);
        Task<Consultation?> GetConsultationByAppointmentAsync(int appointmentId);
        Task<Prescription?> GetPrescriptionByConsultationAsync(int consultationId);
    }
}
