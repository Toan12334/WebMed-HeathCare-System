using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WebMed_HeathCare_System.Interfaces;
using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Services
{
    public class ConsultationService : IConsultationService
    {
        private readonly WebMedDbContext _context;

        public ConsultationService(WebMedDbContext context)
        {
            _context = context;
        }

        public async Task<Appointment?> GetAppointmentForSessionAsync(int appointmentId)
        {
            return await _context.Appointments
                .Include(a => a.Patient).ThenInclude(p => p.PatientNavigation)
                .Include(a => a.Doctor).ThenInclude(d => d.DoctorNavigation)
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);
        }

        public async Task<List<object>> SearchMedicinesAsync(string keyword)
        {
            return await _context.Medicines
                .Where(m => m.IsActive && m.Name.Contains(keyword))
                .Select(m => new { m.MedicineId, m.Name, m.Price })
                .Take(5)
                .Cast<object>()
                .ToListAsync();
        }

        public async Task<bool> SubmitConsultationAsync(int appointmentId, int doctorId, string? diagnosis, string? treatmentPlan, string? prescriptionJson)
        {
            var appointment = await _context.Appointments.FindAsync(appointmentId);
            if (appointment == null || appointment.DoctorId != doctorId)
            {
                return false;
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                appointment.Status = "Completed";

                var consultation = new Consultation
                {
                    AppointmentId = appointmentId,
                    Diagnosis = diagnosis ?? "No diagnosis entered.",
                    TreatmentPlan = treatmentPlan,
                    ConductedAt = DateTime.Now
                };

                _context.Consultations.Add(consultation);
                await _context.SaveChangesAsync();

                if (!string.IsNullOrWhiteSpace(prescriptionJson))
                {
                    var items = JsonSerializer.Deserialize<List<PrescriptionInputItem>>(prescriptionJson);
                    if (items != null && items.Any())
                    {
                        var prescription = new Prescription
                        {
                            ConsultationId = consultation.ConsultationId,
                            DoctorId = appointment.DoctorId,
                            PatientId = appointment.PatientId,
                            IssuedDate = DateTime.Now,
                            Notes = "Consultation Prescription"
                        };

                        _context.Prescriptions.Add(prescription);
                        await _context.SaveChangesAsync();

                        foreach (var item in items)
                        {
                            _context.PrescriptionItems.Add(new PrescriptionItem
                            {
                                PrescriptionId = prescription.PrescriptionId,
                                MedicineId = item.MedicineId,
                                Dosage = item.Dosage ?? "As directed",
                                Frequency = item.Frequency ?? "Once daily",
                                DurationDays = item.DurationDays > 0 ? item.DurationDays : 7
                            });
                        }

                        await _context.SaveChangesAsync();
                    }
                }

                await transaction.CommitAsync();
                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        public async Task<Consultation?> GetConsultationByAppointmentAsync(int appointmentId)
        {
            return await _context.Consultations.FirstOrDefaultAsync(c => c.AppointmentId == appointmentId);
        }

        public async Task<Prescription?> GetPrescriptionByConsultationAsync(int consultationId)
        {
            return await _context.Prescriptions
                .Include(p => p.PrescriptionItems)
                .ThenInclude(i => i.Medicine)
                .FirstOrDefaultAsync(p => p.ConsultationId == consultationId);
        }
    }
}
