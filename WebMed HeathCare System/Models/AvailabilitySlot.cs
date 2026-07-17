using System;
using System.Collections.Generic;

namespace WebMed_HeathCare_System.Models;

public partial class AvailabilitySlot
{
    public int SlotId { get; set; }

    public int DoctorId { get; set; }

    public DateTime StartDateTime { get; set; }

    public DateTime EndDateTime { get; set; }

    public bool IsBooked { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual Doctor Doctor { get; set; } = null!;
}
