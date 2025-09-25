using SarajevoAir.Api.Entities;
using SarajevoAir.Api.Repositories;

namespace SarajevoAir.Api.Services;

public interface IAqiAdminService
{
    Task<IReadOnlyList<SimpleAqiRecord>> GetAllRecordsAsync(CancellationToken cancellationToken = default);
    Task DeleteRecordAsync(int id, CancellationToken cancellationToken = default);
}

public class AqiAdminService : IAqiAdminService
{
    private readonly IAqiRepository _repository;

    public AqiAdminService(IAqiRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<SimpleAqiRecord>> GetAllRecordsAsync(CancellationToken cancellationToken = default)
        => _repository.GetAllAsync(cancellationToken);

    public Task DeleteRecordAsync(int id, CancellationToken cancellationToken = default)
        => _repository.DeleteAsync(id, cancellationToken);
}
