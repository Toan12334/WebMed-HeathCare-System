using WebMed_HeathCare_System.Interfaces;

namespace WebMed_HeathCare_System.Services
{
    public class HealthCalculatorService : IHealthCalculatorService
    {
        public double CalculateBMI(double heightCm, double weightKg)
        {
            if (heightCm <= 0 || weightKg <= 0) return 0;
            double heightM = heightCm / 100.0;
            return Math.Round(weightKg / (heightM * heightM), 2);
        }

        public string GetBMICategory(double bmi)
        {
            if (bmi < 18.5) return "Underweight";
            if (bmi < 25) return "Normal weight";
            if (bmi < 30) return "Overweight";
            return "Obese";
        }

        public double CalculateCalorieNeeds(double weightKg, double heightCm, int age, string gender, string activityLevel)
        {
            // Mifflin-St Jeor Equation
            double bmr = (10 * weightKg) + (6.25 * heightCm) - (5 * age);
            bmr += (gender.ToLower() == "male") ? 5 : -161;

            double multiplier = activityLevel.ToLower() switch
            {
                "sedentary" => 1.2,
                "light" => 1.375,
                "moderate" => 1.55,
                "active" => 1.725,
                "very_active" => 1.9,
                _ => 1.2
            };

            return Math.Round(bmr * multiplier, 0);
        }
    }
}
