using AutoMapper;
using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Application.Interfaces;
using KamSquare.KamScore.Application.Services;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;
using KamSquare.KamScore.Domain.Exceptions;
using KamSquare.KamScore.Domain.Services;
using KamSquare.KamScore.Domain.ValueObjects;
using KamSquare.KamScore.Api.Helpers;
using KamSquare.KamScore.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace KamSquare.KamScore.Api.Endpoints;

public static class TournamentEndpoints
{
    public static RouteGroupBuilder MapTournamentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tournaments")
            .WithTags("Tournaments");

        group.MapGet("/", GetTournaments);
        group.MapGet("/copy-sources", GetCopySources).RequireAuthorization();
        group.MapGet("/{id}", GetTournament);
        group.MapPost("/", CreateTournament).RequireAuthorization();
        group.MapPut("/{id}", UpdateTournament).RequireAuthorization();
        group.MapDelete("/{id}", DeleteTournament).RequireAuthorization();

        return group;
    }

    private static async Task<IResult> GetTournaments(
        ITournamentRepository repository,
        ITeamRepository teamRepository,
        ICourtRepository courtRepository,
        ICurrentUserService currentUser,
        IOptions<UserOptions> userOptions,
        IMapper mapper)
    {
        var displayNames = ResolveDisplayNames(userOptions);
        var allTournaments = await repository.GetAllAsync();
        var visible = TournamentVisibility.VisibleInList(
            allTournaments, currentUser.UserId, currentUser.IsAuthenticated, currentUser.IsAdmin);

        var enrichedDtos = await EnrichAllAsync(
            visible, currentUser, displayNames, teamRepository, courtRepository, mapper);

        return Results.Ok(enrichedDtos);
    }

    private static async Task<List<TournamentDto>> EnrichAllAsync(
        IEnumerable<Tournament> tournaments,
        ICurrentUserService currentUser,
        Dictionary<string, string> displayNames,
        ITeamRepository teamRepository,
        ICourtRepository courtRepository,
        IMapper mapper)
    {
        var enrichmentTasks = tournaments.Select(tournament =>
        {
            var dto = mapper.Map<TournamentDto>(tournament);
            return EnrichTournamentDtoAsync(dto, tournament, currentUser, displayNames, teamRepository, courtRepository);
        });
        var enrichedDtos = await Task.WhenAll(enrichmentTasks);

        return enrichedDtos.ToList();
    }

    private static async Task<IResult> GetCopySources(
        ITournamentRepository repository,
        ITeamRepository teamRepository,
        ICourtRepository courtRepository,
        ICurrentUserService currentUser,
        IOptions<UserOptions> userOptions,
        IMapper mapper)
    {
        var displayNames = ResolveDisplayNames(userOptions);
        var allTournaments = await repository.GetAllAsync();
        var sources = TournamentVisibility.CopySources(
            allTournaments, currentUser.UserId, currentUser.IsAdmin);

        var enrichedDtos = await EnrichAllAsync(
            sources, currentUser, displayNames, teamRepository, courtRepository, mapper);

        return Results.Ok(enrichedDtos);
    }

    private static async Task<IResult> GetTournament(
        string id,
        ITournamentRepository repository,
        ITeamRepository teamRepository,
        ICourtRepository courtRepository,
        ICurrentUserService currentUser,
        IOptions<UserOptions> userOptions,
        IMapper mapper)
    {
        var tournament = await repository.GetByIdAsync(id);

        if (tournament is null)
        {
            throw new NotFoundException(nameof(Tournament), id);
        }

        var dto = mapper.Map<TournamentDto>(tournament);
        var displayNames = ResolveDisplayNames(userOptions);
        dto = await EnrichTournamentDtoAsync(dto, tournament, currentUser, displayNames, teamRepository, courtRepository);

        return Results.Ok(dto);
    }

    private static async Task<IResult> CreateTournament(
        TournamentDto request,
        ITournamentRepository repository,
        ITournamentStructureRepository structureRepository,
        TournamentCopyService copyService,
        ITeamRepository teamRepository,
        ICourtRepository courtRepository,
        ICurrentUserService currentUser,
        IOptions<UserOptions> userOptions,
        IMapper mapper)
    {
        var type = Enum.Parse<TournamentType>(request.Type!, ignoreCase: true);

        Tournament created;
        if (!string.IsNullOrEmpty(request.SourceTournamentId))
        {
            created = await copyService.CopyAsync(
                request.SourceTournamentId, request.Name, currentUser.UserId!, type);
        }
        else
        {
            var discipline = Enum.Parse<Discipline>(request.Discipline, ignoreCase: true);
            var tournament = Tournament.Create(request.Name, discipline, currentUser.UserId!, type);

            if (request.StartTime.HasValue || request.GameLength.HasValue || request.GameConditions is not null)
            {
                var gameConditions = request.GameConditions is not null
                    ? mapper.Map<GameConditions>(request.GameConditions)
                    : null;
                tournament.Update(request.Name, discipline, request.StartTime, request.GameLength, gameConditions, type);
            }

            created = await repository.CreateAsync(tournament);

            try
            {
                var structure = TournamentStructure.Create(created.Id);
                await structureRepository.CreateAsync(structure);
            }
            catch
            {
                await repository.DeleteAsync(created.Id, created.OwnerId);
                throw;
            }
        }

        var dto = mapper.Map<TournamentDto>(created);
        var displayNames = ResolveDisplayNames(userOptions);
        dto = await EnrichTournamentDtoAsync(dto, created, currentUser, displayNames, teamRepository, courtRepository);

        return Results.Created($"/api/tournaments/{dto.Id}", dto);
    }

    private static async Task<IResult> UpdateTournament(
        string id,
        TournamentDto request,
        ITournamentRepository repository,
        ITeamRepository teamRepository,
        ICourtRepository courtRepository,
        ICurrentUserService currentUser,
        IOptions<UserOptions> userOptions,
        IMapper mapper)
    {
        var tournament = await repository.GetOwnedTournamentAsync(currentUser, id);

        var discipline = Enum.Parse<Discipline>(request.Discipline, ignoreCase: true);
        var type = Enum.Parse<TournamentType>(request.Type!, ignoreCase: true);
        var gameConditions = request.GameConditions is not null
            ? mapper.Map<GameConditions>(request.GameConditions)
            : null;

        tournament.Update(request.Name, discipline, request.StartTime, request.GameLength, gameConditions, type);

        var updated = await repository.UpdateAsync(tournament);
        var dto = mapper.Map<TournamentDto>(updated);
        var displayNames = ResolveDisplayNames(userOptions);
        dto = await EnrichTournamentDtoAsync(dto, updated, currentUser, displayNames, teamRepository, courtRepository);

        return Results.Ok(dto);
    }

    private static async Task<IResult> DeleteTournament(
        string id,
        ITournamentRepository repository,
        ITournamentStructureRepository structureRepository,
        IGameRepository gameRepository,
        ITeamRepository teamRepository,
        ICourtRepository courtRepository,
        IVolunteerRepository volunteerRepository,
        ICurrentUserService currentUser)
    {
        var tournament = await repository.GetOwnedTournamentAsync(currentUser, id);

        await Task.WhenAll(
            gameRepository.DeleteByTournamentIdAsync(id),
            teamRepository.DeleteByTournamentIdAsync(id),
            courtRepository.DeleteByTournamentIdAsync(id),
            volunteerRepository.DeleteByTournamentIdAsync(id));

        await structureRepository.DeleteByTournamentIdAsync(id);
        await repository.DeleteAsync(id, tournament.OwnerId);

        return Results.NoContent();
    }

    private static TournamentDto HideTournamentCode(TournamentDto dto)
    {
        return dto with { TournamentCode = null };
    }

    private static string ResolveOwnerDisplayName(string? ownerId, Dictionary<string, string> displayNames)
    {
        if (string.IsNullOrEmpty(ownerId))
            return "Unknown";

        return displayNames.TryGetValue(ownerId, out var displayName) ? displayName : ownerId;
    }

    private static Dictionary<string, string> ResolveDisplayNames(IOptions<UserOptions> userOptions)
    {
        return userOptions.Value.Entries.ToDictionary(u => u.Username, u => u.DisplayName);
    }

    private static async Task<TournamentDto> EnrichTournamentDtoAsync(
        TournamentDto dto,
        Tournament tournament,
        ICurrentUserService currentUser,
        Dictionary<string, string> displayNames,
        ITeamRepository teamRepository,
        ICourtRepository courtRepository)
    {
        dto = dto with { OwnerDisplayName = ResolveOwnerDisplayName(dto.OwnerId, displayNames) };

        if (!TournamentAuthorizationHelper.HasAdminAccess(tournament, currentUser))
        {
            dto = HideTournamentCode(dto);
        }

        var teamCountTask = teamRepository.CountByTournamentIdAsync(tournament.Id);
        var courtCountTask = courtRepository.CountByTournamentIdAsync(tournament.Id);
        await Task.WhenAll(teamCountTask, courtCountTask);

        return dto with { TeamCount = await teamCountTask, CourtCount = await courtCountTask };
    }
}
