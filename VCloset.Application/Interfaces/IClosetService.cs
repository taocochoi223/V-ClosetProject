using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VCloset.Application.DTOs;

namespace VCloset.Application.Interfaces;

public interface IClosetService
{
    Task<List<ClosetDto>> GetClosetsAsync(int userInternalId);
    Task<ClosetDto> CreateClosetAsync(int userInternalId, CreateClosetRequestDto dto);
}
