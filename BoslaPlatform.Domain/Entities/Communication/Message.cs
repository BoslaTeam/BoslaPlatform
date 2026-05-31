using BoslaPlatform.Domain.Common;
using BoslaPlatform.Domain.Entities;
using BoslaPlatform.Domain.Models.Booking;
using BoslaPlatform.Domain.Models.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoslaPlatform.Domain.Models.Conversations
{
    public class Message: AuditableEntity
    {
        public Guid ConversationId { get; set; }
        public Guid SenderId { get; set; }
        public string MessageText { get; set; } = string.Empty;
        public Guid? AppointmentId { get; set; }
        // Navigation
        public Conversation Conversation { get; set; } = null!;
        public Appointment? Appointment { get; set; }
        public User Sender { get; set; }

    }
}
