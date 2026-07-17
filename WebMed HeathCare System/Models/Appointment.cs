using System;
using System.Collections.Generic;

namespace WebMed_HeathCare_System.Models;

public partial class Appointment
{
    public int AppointmentId { get; set; }

    public int PatientId { get; set; }

    public int DoctorId { get; set; }

    public int SlotId { get; set; }

    public DateTime AppointmentDateTime { get; set; }

    public string Status { get; set; } = null!;

    public string ConsultationType { get; set; } = null!;

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Consultation? Consultation { get; set; }

    public virtual Doctor Doctor { get; set; } = null!;

    public virtual Patient Patient { get; set; } = null!;

    public virtual AvailabilitySlot Slot { get; set; } = null!;
}
