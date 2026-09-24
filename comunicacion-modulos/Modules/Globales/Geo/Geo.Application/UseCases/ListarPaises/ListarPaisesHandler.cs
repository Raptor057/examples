using Common.Abstractions;
using Geo.Contracts;
using Geo.Domain.Repositories;

namespace Geo.Application.UseCases.ListarPaises;

public sealed class ListarPaisesHandler : IInteractor<ListarPaisesRequest, ListarPaisesResponse>
{
    private readonly IPaisRepository _paises;

    public ListarPaisesHandler(IPaisRepository paises) => _paises = paises;

    public async Task<ListarPaisesResponse> Handle(ListarPaisesRequest request, CancellationToken cancellationToken)
    {
        var paises = await _paises.ListAllAsync(cancellationToken);
        var dto = paises.Select(p => new PaisInfo(p.Codigo, p.Nombre)).ToList();
        return new ListarPaisesSuccess(dto);
    }
}
