using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Linq;
using System;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using WebMed_HeathCare_System.Interfaces;
using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Controllers
{
    public class InsuranceGuideController : Controller
    {
        private readonly IInsurancePlanRepository _insurancePlanRepository;
        private readonly ICoverageRepository _coverageRepository;
        private readonly IPricingRepository _pricingRepository;
        private readonly WebMedDbContext _context;

        public InsuranceGuideController(
            IInsurancePlanRepository insurancePlanRepository,
            ICoverageRepository coverageRepository,
            IPricingRepository pricingRepository,
            WebMedDbContext context)
        {
            _insurancePlanRepository = insurancePlanRepository;
            _coverageRepository = coverageRepository;
            _pricingRepository = pricingRepository;
            _context = context;
        }

        // GET: /InsuranceGuide
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var plans = await _insurancePlanRepository.GetAvailablePlansAsync();
            return View(plans);
        }

        // GET: /InsuranceGuide/Details/{id}
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var plan = await _insurancePlanRepository.GetPlanByIdAsync(id);
            if (plan == null)
            {
                return NotFound();
            }

            var coverage = await _coverageRepository.GetCoverageDetailsAsync(id);
            var pricing = await _pricingRepository.GetPricingInformationAsync(id);
            var benefits = await _insurancePlanRepository.GetBenefitsAsync(id);

            var viewModel = new InsurancePlanDetailsViewModel
            {
                Plan = plan,
                Coverage = coverage,
                Pricing = pricing,
                Benefits = benefits
            };

            // Pass all other plans to the view so the user can select one to compare
            ViewBag.OtherPlans = (await _insurancePlanRepository.GetAvailablePlansAsync())
                .Where(p => p.PlanId != id).ToList();

            return View(viewModel);
        }

        // GET: /InsuranceGuide/Compare
        [HttpGet]
        public async Task<IActionResult> Compare(int plan1, int plan2)
        {
            // Plan 1 Details
            var p1 = await _insurancePlanRepository.GetPlanByIdAsync(plan1);
            if (p1 == null) return NotFound();

            var plan1ViewModel = new InsurancePlanDetailsViewModel
            {
                Plan = p1,
                Coverage = await _coverageRepository.GetCoverageDetailsAsync(plan1),
                Pricing = await _pricingRepository.GetPricingInformationAsync(plan1),
                Benefits = await _insurancePlanRepository.GetBenefitsAsync(plan1)
            };

            // Plan 2 Details
            var p2 = await _insurancePlanRepository.GetPlanByIdAsync(plan2);
            if (p2 == null) return NotFound();

            var plan2ViewModel = new InsurancePlanDetailsViewModel
            {
                Plan = p2,
                Coverage = await _coverageRepository.GetCoverageDetailsAsync(plan2),
                Pricing = await _pricingRepository.GetPricingInformationAsync(plan2),
                Benefits = await _insurancePlanRepository.GetBenefitsAsync(plan2)
            };

            var compareViewModel = new InsurancePlanCompareViewModel
            {
                Plan1 = plan1ViewModel,
                Plan2 = plan2ViewModel
            };

            return View(compareViewModel);
        }

        // GET: /InsuranceGuide/Enroll
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Enroll(int planId)
        {
            var plan = await _insurancePlanRepository.GetPlanByIdAsync(planId);
            if (plan == null) return NotFound();

            var pricing = await _pricingRepository.GetPricingInformationAsync(planId);
            var coverage = await _coverageRepository.GetCoverageDetailsAsync(planId);

            var viewModel = new InsurancePlanDetailsViewModel
            {
                Plan = plan,
                Pricing = pricing,
                Coverage = coverage
            };

            return View(viewModel);
        }

        // POST: /InsuranceGuide/ProcessEnrollment
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ProcessEnrollment(int planId, string paymentMethod)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
            {
                return Challenge();
            }

            var plan = await _insurancePlanRepository.GetPlanByIdAsync(planId);
            if (plan == null) return NotFound();

            var pricing = await _pricingRepository.GetPricingInformationAsync(planId);
            if (pricing == null) return BadRequest("Plan pricing not found");

            // Check if patient already has active insurance for this plan
            var existingInsurance = await _context.PatientInsurances
                .FirstOrDefaultAsync(pi => pi.PatientId == userId && pi.PlanId == planId && pi.Status == "Active");

            if (existingInsurance != null)
            {
                TempData["ErrorMessage"] = "You are already enrolled in this insurance plan!";
                return RedirectToAction("Index");
            }

            // Check if patient already has a pending payment insurance for this plan
            var pendingInsurance = await _context.PatientInsurances
                .FirstOrDefaultAsync(pi => pi.PatientId == userId && pi.PlanId == planId && pi.Status == "PendingPayment");

            if (pendingInsurance != null)
            {
                return RedirectToAction("Payment", new { patientInsuranceId = pendingInsurance.PatientInsuranceId });
            }

            // Create PatientInsurance record
            var patientInsurance = new PatientInsurance
            {
                PatientId = userId,
                PlanId = planId,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddMonths(plan.DurationMonths),
                Status = "PendingPayment"
            };

            _context.PatientInsurances.Add(patientInsurance);
            await _context.SaveChangesAsync();

            // Create Payment record
            var payment = new Payment
            {
                UserId = userId,
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

            return RedirectToAction("Payment", new { patientInsuranceId = patientInsurance.PatientInsuranceId });
        }

        // GET: /InsuranceGuide/Payment/{patientInsuranceId}
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Payment(int patientInsuranceId)
        {
            var patientInsurance = await _context.PatientInsurances
                .Include(pi => pi.Plan)
                .FirstOrDefaultAsync(pi => pi.PatientInsuranceId == patientInsuranceId);

            if (patientInsurance == null) return NotFound();

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId) || patientInsurance.PatientId != userId)
            {
                return Forbid();
            }

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.PaymentType == "Insurance" && p.AssociatedId == patientInsuranceId);

            if (payment == null) return NotFound();

            ViewBag.Payment = payment;
            return View(patientInsurance);
        }

        // POST: /InsuranceGuide/ProcessInsurancePayment
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ProcessInsurancePayment(int patientInsuranceId)
        {
            var patientInsurance = await _context.PatientInsurances.FindAsync(patientInsuranceId);
            if (patientInsurance == null) return NotFound();

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId) || patientInsurance.PatientId != userId)
            {
                return Forbid();
            }

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.PaymentType == "Insurance" && p.AssociatedId == patientInsuranceId);

            if (payment == null) return NotFound();

            // Simulate gateway success
            patientInsurance.Status = "Active";
            
            payment.PaymentStatus = "Completed";
            payment.TransactionReference = "TXN-INS-" + new Random().Next(100000, 999999);
            payment.PaidAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Insurance enrollment payment processed successfully! Your plan is now Active.";
            return RedirectToAction("Index");
        }

        // POST: /InsuranceGuide/FailInsurancePayment
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> FailInsurancePayment(int patientInsuranceId)
        {
            var patientInsurance = await _context.PatientInsurances.FindAsync(patientInsuranceId);
            if (patientInsurance == null) return NotFound();

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId) || patientInsurance.PatientId != userId)
            {
                return Forbid();
            }

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.PaymentType == "Insurance" && p.AssociatedId == patientInsuranceId);

            if (payment == null) return NotFound();

            payment.PaymentStatus = "Failed";
            await _context.SaveChangesAsync();

            TempData["ErrorMessage"] = "Insurance payment simulation failed. Please retry payment or contact support.";
            return RedirectToAction("Payment", new { patientInsuranceId });
        }
    }
}
