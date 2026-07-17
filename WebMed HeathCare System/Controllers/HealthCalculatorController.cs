using Microsoft.AspNetCore.Mvc;
using WebMed_HeathCare_System.Interfaces;
using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Controllers
{
    public class HealthCalculatorController : Controller
    {
        private readonly IHealthCalculatorService _calculatorService;

        public HealthCalculatorController(IHealthCalculatorService calculatorService)
        {
            _calculatorService = calculatorService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Calculate(double height, double weight, int age, string gender, string activityLevel, string calcType)
        {
            if (height <= 0 || weight <= 0 || age <= 0)
            {
                ViewBag.Error = "Please enter valid positive numbers for height, weight, and age.";
                return View("Index");
            }

            if (calcType == "BMI")
            {
                double bmi = _calculatorService.CalculateBMI(height, weight);
                ViewBag.BMI = bmi;
                ViewBag.BMICategory = _calculatorService.GetBMICategory(bmi);
            }
            else if (calcType == "Calories")
            {
                double calories = _calculatorService.CalculateCalorieNeeds(weight, height, age, gender, activityLevel);
                ViewBag.Calories = calories;
            }

            ViewBag.Height = height;
            ViewBag.Weight = weight;
            ViewBag.Age = age;
            ViewBag.Gender = gender;
            ViewBag.ActivityLevel = activityLevel;
            ViewBag.CalcType = calcType;

            return View("Index");
        }
    }
}
