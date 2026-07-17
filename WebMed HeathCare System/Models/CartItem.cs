namespace WebMed_HeathCare_System.Models
{
    public class CartItem
    {
        public int MedicineId { get; set; }
        public string Name { get; set; } = null!;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public bool IsPrescriptionRequired { get; set; }
    }
}
