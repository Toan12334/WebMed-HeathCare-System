namespace WebMed_HeathCare_System.Models
{
    public class PrescriptionInputItem
    {
        public int MedicineId { get; set; }
        public string? Dosage { get; set; }
        public string? Frequency { get; set; }
        public int DurationDays { get; set; }
    }
}
