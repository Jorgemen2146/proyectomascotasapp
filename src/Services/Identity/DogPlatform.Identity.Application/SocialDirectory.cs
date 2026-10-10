using DogPlatform.Identity.Domain.Repositories;
using MediatR;
namespace DogPlatform.Identity.Application.Features.Profile.SocialDirectory;
public sealed record SocialUserCard(Guid UserId,string DisplayName,string? ProfilePhotoUrl);
public sealed record GetSocialUserCardQuery(Guid UserId):IRequest<SocialUserCard?>;
public sealed record GetSocialUserCardsQuery(IReadOnlyCollection<Guid> UserIds):IRequest<IReadOnlyCollection<SocialUserCard>>;
public sealed record SearchSocialUsersQuery(string Query,int Limit):IRequest<IReadOnlyCollection<SocialUserCard>>;
internal sealed class GetSocialUserCardHandler(IUserRepository users):IRequestHandler<GetSocialUserCardQuery,SocialUserCard?>
{ public async Task<SocialUserCard?> Handle(GetSocialUserCardQuery q,CancellationToken ct){var u=await users.GetByIdAsync(q.UserId,ct);return u is not {IsActive:true}?null:new(u.Id,u.FullName.Display,string.IsNullOrWhiteSpace(u.ProfilePhotoUrl)?null:$"/api/v1/auth/users/{u.Id}/photo/content");} }
internal sealed class GetSocialUserCardsHandler(IUserRepository users):IRequestHandler<GetSocialUserCardsQuery,IReadOnlyCollection<SocialUserCard>>
{
 public async Task<IReadOnlyCollection<SocialUserCard>> Handle(GetSocialUserCardsQuery q,CancellationToken ct)
 {
  var ids=q.UserIds.Where(id=>id!=Guid.Empty).Distinct().Take(100).ToArray();
  return (await users.GetByIdsAsync(ids,ct)).Select(u=>new SocialUserCard(u.Id,u.FullName.Display,string.IsNullOrWhiteSpace(u.ProfilePhotoUrl)?null:$"/api/v1/auth/users/{u.Id}/photo/content")).ToArray();
 }
}
internal sealed class SearchSocialUsersHandler(IUserRepository users):IRequestHandler<SearchSocialUsersQuery,IReadOnlyCollection<SocialUserCard>>
{ public async Task<IReadOnlyCollection<SocialUserCard>> Handle(SearchSocialUsersQuery q,CancellationToken ct)=>string.IsNullOrWhiteSpace(q.Query)||q.Limit is <1 or >50?[]:(await users.SearchActiveAsync(q.Query,q.Limit,ct)).Select(u=>new SocialUserCard(u.Id,u.FullName.Display,string.IsNullOrWhiteSpace(u.ProfilePhotoUrl)?null:$"/api/v1/auth/users/{u.Id}/photo/content")).ToArray(); }
