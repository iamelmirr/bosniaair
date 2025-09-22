using SarajevoAir.Application.Dtos;

namespace SarajevoAir.Application.Interfaces;

public interface IShareService
{
    Task<ShareResponseDto> GenerateShareContentAsync(ShareRequestDto request, CancellationToken cancellationToken = default);
}