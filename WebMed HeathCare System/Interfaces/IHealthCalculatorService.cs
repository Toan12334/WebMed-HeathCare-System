namespace WebMed_HeathCare_System.Interfaces
{
    public interface IHealthCalculatorService
    {
        double CalculateBMI(double heightCm, double weightKg);
        string GetBMICategory(double bmi);
        double CalculateCalorieNeeds(double weightKg, double heightCm, int age, string gender, string activityLevel);
    }
}
