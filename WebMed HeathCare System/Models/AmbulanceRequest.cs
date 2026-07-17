using System;
using System.Collections.Generic;

namespace WebMed_HeathCare_System.Models;

public partial class AmbulanceRequest
{
    public int RequestId { get; set; }

    public int PatientId { get; set; }

    public string PickupLocation { get; set; } = null!;

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public string? EmergencyDetails { get; set; }

    public string Status { get; set; } = null!;

    public string? AssignedAmbulanceVehicle { get; set; }

    public decimal? AmbulanceLatitude { get; set; }

    public decimal? AmbulanceLongitude { get; set; }

    public string? Eta { get; set; }

    public DateTime RequestedAt { get; set; }

    public virtual Patient Patient { get; set; } = null!;
}
