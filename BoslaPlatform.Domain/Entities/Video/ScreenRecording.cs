using BoslaPlatform.Domain.Common;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Models.Booking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoslaPlatform.Domain.Models.Video
{
    public class ScreenRecording:AuditableEntity
    {
        public Guid AppointmentId { get; set; }
        public string Url { get; set; } = string.Empty;
        public RecordingStatus Status { get; set; }
        public long? FileSizeBytes { get; set; }
        public int? DurationSeconds { get; set; }
        public RecordingAccessControl AccessControl { get; set; }
        public RecordingStorageProvider StorageProvider { get; set; }
        public string? AgoraRecordingId { get; set; }
        public string? AgoraRecordingSid { get; set; }

        // Navigation
        public Appointment Appointment { get; set; } = null!;
    }
}
