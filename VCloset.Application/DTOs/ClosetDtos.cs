using System;
using System.Collections.Generic;

namespace VCloset.Application.DTOs;

public class ClosetDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public int ItemCount { get; set; }
    public List<string> ThumbnailUrls { get; set; } = new List<string>();
    public DateTime CreatedAt { get; set; }
}

public class CreateClosetRequestDto
{
    public string Name { get; set; } = null!;
}
