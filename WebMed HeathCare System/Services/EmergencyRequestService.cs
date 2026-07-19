using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebMed_HeathCare_System.Interfaces;
using WebMed_HeathCare_System.Models;

namespace WebMed_HeathCare_System.Services
{
    public class EmergencyRequestService : IEmergencyRequestService
    {
        private readonly WebMedDbContext _context;

        public EmergencyRequestService(WebMedDbContext context)
        {
            _context = context;
        }

        public async Task<AmbulanceRequest> CreateRequestAsync(int? patientId, string pickupLocation, string emergencyDetails, decimal? latitude, decimal? longitude)
        {
            decimal patientLat = latitude ?? 10.776m;
            decimal patientLng = longitude ?? 106.700m;

            var request = new AmbulanceRequest
            {
                PatientId = patientId,
                PickupLocation = pickupLocation,
                Latitude = patientLat,
                Longitude = patientLng,
                Status = "Pending",
                AssignedAmbulanceVehicle = null,
                AmbulanceLatitude = null,
                AmbulanceLongitude = null,
                Eta = "Awaiting Dispatcher",
                EmergencyDetails = emergencyDetails,
                RequestedAt = DateTime.Now
            };

            _context.AmbulanceRequests.Add(request);
            await _context.SaveChangesAsync();

            return request;
        }

        public async Task<AmbulanceRequest?> GetRequestByIdAsync(int requestId)
        {
            return await _context.AmbulanceRequests.FindAsync(requestId);
        }

        public async Task<object?> GetTrackingDataAsync(int requestId)
        {
            var request = await _context.AmbulanceRequests.FindAsync(requestId);
            if (request == null)
            {
                return null;
            }

            // Simulate ambulance moving by slightly changing coordinates if it's assigned
            if (request.Status == "Assigned" || request.Status == "On the way")
            {
                double elapsedSeconds = (DateTime.Now - request.RequestedAt).TotalSeconds;

                if (elapsedSeconds >= 180)
                {
                    request.Status = "Arrived";
                    request.Eta = "Arrived";
                    request.AmbulanceLatitude = request.Latitude;
                    request.AmbulanceLongitude = request.Longitude;
                }
                else
                {
                    double steps = elapsedSeconds / 180.0; // 0.0 to 1.0

                    decimal destLat = request.Latitude ?? 10.776m;
                    decimal destLng = request.Longitude ?? 106.700m;

                    decimal startLat = destLat - 0.008m;
                    decimal startLng = destLng - 0.008m;

                    request.AmbulanceLatitude = startLat + (destLat - startLat) * (decimal)steps;
                    request.AmbulanceLongitude = startLng + (destLng - startLng) * (decimal)steps;
                    
                    int remainingMinutes = 15 - (int)(elapsedSeconds / 60);
                    if (remainingMinutes < 1) remainingMinutes = 1;
                    request.Eta = $"{remainingMinutes} mins";
                }

                await _context.SaveChangesAsync();
            }

            return new
            {
                latitude = request.AmbulanceLatitude,
                longitude = request.AmbulanceLongitude,
                status = request.Status,
                eta = request.Eta,
                vehicle = request.AssignedAmbulanceVehicle
            };
        }
    }
}
