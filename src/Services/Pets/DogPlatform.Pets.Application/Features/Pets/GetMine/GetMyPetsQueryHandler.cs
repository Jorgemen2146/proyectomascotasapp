using DogPlatform.Pets.Application.Common;
using DogPlatform.Pets.Application.Queries;
using DogPlatform.Pets.Application.Security;
using DogPlatform.SharedKernel.Primitives;
using FluentValidation;
using MediatR;

namespace DogPlatform.Pets.Application.Features.Pets.GetMine;

public sealed class GetMyPetsQueryHandler
    : IRequestHandler<GetMyPetsQuery, Result<PagedResult<MyPetResponse>>>
{
    private readonly IPetQueryService _queryService;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<GetMyPetsQuery> _validator;

    public GetMyPetsQueryHandler(
        IPetQueryService queryService,
        ICurrentUser currentUser,
        IValidator<GetMyPetsQuery> validator)
    {
        _queryService = queryService;
        _currentUser = currentUser;
        _validator = validator;
    }

    public async Task<Result<PagedResult<MyPetResponse>>> Handle(
        GetMyPetsQuery request,
        CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            var first = validation.Errors[0];
            return Result.Failure<PagedResult<MyPetResponse>>(
                Error.Validation(first.ErrorCode, first.ErrorMessage));
        }

        var result = await _queryService.GetMyPetsAsync(
            _currentUser.UserId,
            request,
            cancellationToken);

        return Result.Success(result);
    }
}

