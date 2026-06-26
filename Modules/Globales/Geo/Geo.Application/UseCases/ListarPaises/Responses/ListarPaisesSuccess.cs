using Common.Results;
using Geo.Contracts;

namespace Geo.Application.UseCases.ListarPaises;

public sealed record ListarPaisesSuccess(IReadOnlyList<PaisInfo> Paises)
    : ListarPaisesResponse, ISuccess;
