using AutoMapper;
using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Application.Interfaces;
using KamSquare.KamScore.Application.Services;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;
using KamSquare.KamScore.Domain.Exceptions;
using KamSquare.KamScore.Domain.ValueObjects;
using KamSquare.KamScore.Api.Helpers;
using KamSquare.KamScore.Infrastructure.Options;

namespace KamSquare.KamScore.Api.Endpoints;

public static class TournamentEndpoints
{
    public static RouteGroupBuilder MapTournamentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tournaments")
            .WithTags("Tournaments");

        group.MapGet("/", GetTournaments);
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
        IMapper mapper)
    {
        var allTournaments = await repository.GetAllAsync();
        var dtos = allTournaments.Select(tournament =>
        {
            var dto = mapper.Map<TournamentDto>(tournament);
            if (!TournamentAuthorizationHelper.HasAdminAccess(tournament, currentUser))
            {
                dto = HideTournamentCode(dto);
            }
            return dto;
        });

        var enrichmentTasks = dtos.Select(async dto =>
        {
            var teamCountTask = teamRepository.CountByTournamentIdAsync(dto.Id!);
            var courtCountTask = courtRepository.CountByTournamentIdAsync(dto.Id!);
            await Task.WhenAll(teamCountTask, courtCountTask);
            return dto with { TeamCount = await teamCountTask, CourtCount = await courtCountTask };
        });
        var enrichedDtos = await Task.WhenAll(enrichmentTasks);

        return Results.Ok(enrichedDtos.ToList());
    }

    private static async Task<IResult> GetTournament(
        string id,
        ITournamentRepository repository,
        ITeamRepository teamRepository,
        ICourtRepository courtRepository,
        ICurrentUserService currentUser,
        IMapper mapper)
    {
        var tournament = await repository.GetByIdAsync(id);

        if (tournament is null)
        {
            throw new NotFoundException(nameof(Tournament), id);
        }

        var dto = mapper.Map<TournamentDto>(tournament);

        if (!TournamentAuthorizationHelper.HasAdminAccess(tournament, currentUser))
        {
            dto = HideTournamentCode(dto);
        }

        var teamCount = await teamRepository.CountByTournamentIdAsync(id);
        var courtCount = await courtRepository.CountByTournamentIdAsync(id);
        dto = dto with { TeamCount = teamCount, CourtCount = courtCount };

        return Results.Ok(dto);
    }

    private static async Task<IResult> CreateTournament(
        TournamentDto request,
        ITournamentRepository repository,
        ITournamentStructureRepository structureRepository,
        TournamentCopyService copyService,
        IConfiguration configuration,
        ICurrentUserService currentUser,
        IMapper mapper)
    {
        if (!string.IsNullOrEmpty(request.SourceTournamentId))
        {
            var flags = configuration.GetSection(FeatureFlagOptions.SectionName)
                .Get<Dictionary<string, bool>>() ?? [];
            if (!flags.GetValueOrDefault("CopyTournamentStructure"))
                return Results.BadRequest("Copy tournament structure feature is not enabled.");

            var copied = await copyService.CopyAsync(
                request.SourceTournamentId, request.Name, currentUser.UserId!);
            var copiedDto = mapper.Map<TournamentDto>(copied);
            return Results.Created($"/api/tournaments/{copiedDto.Id}", copiedDto);
        }

        var discipline = Enum.Parse<Discipline>(request.Discipline, ignoreCase: true);
        var tournament = Tournament.Create(request.Name, discipline, currentUser.UserId!);

        if (request.StartTime.HasValue || request.GameLength.HasValue || request.GameConditions is not null)
        {
            var gameConditions = request.GameConditions is not null
                ? mapper.Map<GameConditions>(request.GameConditions)
                : null;
            tournament.Update(request.Name, discipline, request.StartTime, request.GameLength, gameConditions);
        }

        var created = await repository.CreateAsync(tournament);

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

        var dto = mapper.Map<TournamentDto>(created);

        return Results.Created($"/api/tournaments/{dto.Id}", dto);
    }

    private static async Task<IResult> UpdateTournament(
        string id,
        TournamentDto request,
        ITournamentRepository repository,
        ICurrentUserService currentUser,
        IMapper mapper)
    {
        var tournament = await repository.GetOwnedTournamentAsync(currentUser, id);

        var discipline = Enum.Parse<Discipline>(request.Discipline, ignoreCase: true);
        var gameConditions = request.GameConditions is not null
            ? mapper.Map<GameConditions>(request.GameConditions)
            : null;

        tournament.Update(request.Name, discipline, request.StartTime, request.GameLength, gameConditions);

        var updated = await repository.UpdateAsync(tournament);
        var dto = mapper.Map<TournamentDto>(updated);

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
}
