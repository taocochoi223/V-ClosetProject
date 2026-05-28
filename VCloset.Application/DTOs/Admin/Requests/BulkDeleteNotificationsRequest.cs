using System;
using System.Collections.Generic;

namespace VCloset.Application.DTOs.Admin.Requests;

public class BulkDeleteNotificationsRequest
{
    public List<Guid> NotificationIds { get; set; } = new();
}
