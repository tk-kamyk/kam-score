using System.Globalization;
using FluentValidation;
using FluentValidation.Results;
using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Application.Interfaces;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Exceptions;
using KamSquare.KamScore.Domain.Services;

namespace KamSquare.KamScore.Application.Services;

/// <summary>
/// Coordinates volunteer and shift operations that need multiple repositories
/// (structure, games, volunteers) and/or the <see cref="VolunteerShiftCalculator"/>
/// output. Endpoints delegate here to keep their handlers focused on HTTP
/// concerns and authorization.
/// </summary>
public class VolunteerService
{
    private readonly IVolunteerRepository _volunteerRepository;
    private readonly ITournamentStructureRepository _structureRepository;
    private readonly IGameRepository _gameRepository;
    private readonly ITeamRepository _teamRepository;

    public VolunteerService(
        IVolunteerRepository volunteerRepository,
        ITournamentStructureRepository structureRepository,
        IGameRepository gameRepository,
        ITeamRepository teamRepository)
    {
        _volunteerRepository = volunteerRepository;
        _structureRepository = structureRepository;
        _gameRepository = gameRepository;
        _teamRepository = teamRepository;
    }

    // --- CRUD ---

    public async Task<Volunteer> CreateAsync(string tournamentId, string name, string? contact, string? teamId)
    {
        if (await _volunteerRepository.ExistsByNameAsync(tournamentId, name))
            throw new ValidationException(
                [new ValidationFailure("Name", $"A volunteer with name '{name}' already exists in this tournament.")]);

        await EnsureTeamExistsAsync(tournamentId, teamId);

        var volunteer = Volunteer.Create(name, tournamentId, contact, teamId);
        return await _volunteerRepository.CreateAsync(volunteer);
    }

    public async Task<Volunteer> UpdateAsync(string tournamentId, string volunteerId, string name, string? contact, string? teamId)
    {
        var volunteer = await _volunteerRepository.GetByIdAsync(volunteerId, tournamentId)
            ?? throw new NotFoundException(nameof(Volunteer), volunteerId);

        if (await _volunteerRepository.ExistsByNameAsync(tournamentId, name, volunteerId))
            throw new ValidationException(
                [new ValidationFailure("Name", $"A volunteer with name '{name}' already exists in this tournament.")]);

        await EnsureTeamExistsAsync(tournamentId, teamId);

        volunteer.Update(name, contact, teamId);
        return await _volunteerRepository.UpdateAsync(volunteer);
    }

    public async Task DeleteAsync(string tournamentId, string volunteerId)
    {
        var volunteer = await _volunteerRepository.GetByIdAsync(volunteerId, tournamentId)
            ?? throw new NotFoundException(nameof(Volunteer), volunteerId);

        await _volunteerRepository.DeleteAsync(volunteer.Id, tournamentId);
    }

    private async Task EnsureTeamExistsAsync(string tournamentId, string? teamId)
    {
        if (teamId is null)
            return;

        var team = await _teamRepository.GetByIdAsync(teamId, tournamentId);
        if (team is null)
            throw new ValidationException(
                [new ValidationFailure("TeamId", $"Team '{teamId}' does not exist in this tournament.")]);
    }

    // --- Shifts ---

    public async Task<List<ShiftGroup>> GetShiftGroupsAsync(Tournament tournament)
    {
        var structure = await _structureRepository.GetByTournamentIdAsync(tournament.Id);
        var phases = structure?.Phases.OrderBy(p => p.Order).ToList() ?? [];

        if (phases.Count == 0)
            return VolunteerShiftCalculator.CalculateShiftGroups([], tournament.GameLength);

        var allGames = (await _gameRepository.GetByTournamentIdAsync(tournament.Id)).ToList();

        var phaseInfos = phases
            .Select(p =>
            {
                var phaseGames = allGames.Where(g => g.PhaseId == p.Id).ToList();
                var maxRounds = phaseGames.Count > 0 ? phaseGames.Max(g => g.Round) : 1;
                return (p.Name, p.StartTime, maxRounds);
            })
            .ToList();

        return VolunteerShiftCalculator.CalculateShiftGroups(phaseInfos, tournament.GameLength);
    }

    public async Task<List<ShiftGroupResponseDto>> GetShiftsAsync(Tournament tournament)
    {
        var shiftGroups = await GetShiftGroupsAsync(tournament);
        var volunteers = (await _volunteerRepository.GetByTournamentIdAsync(tournament.Id)).ToList();
        var allGames = (await _gameRepository.GetByTournamentIdAsync(tournament.Id)).ToList();

        return shiftGroups.Select(group => new ShiftGroupResponseDto(
            group.Name,
            group.IsSpecial,
            group.Shifts.Select(shiftTime =>
            {
                var assignedVols = volunteers
                    .Where(v => v.Assignments.Any(a => a.ShiftGroup == group.Name && a.ShiftTime == shiftTime))
                    .Select(v => new ShiftVolunteerDto(
                        v.Id,
                        v.Name,
                        group.IsSpecial || shiftTime is null || !IsTeamBusyAtTime(v.TeamId, allGames, shiftTime.Value)))
                    .ToList();

                return new ShiftSlotDto(
                    shiftTime?.ToString("HH:mm", CultureInfo.InvariantCulture),
                    assignedVols);
            }).ToList()
        )).ToList();
    }

    public async Task<List<VolunteerAvailabilityDto>> GetAvailableVolunteersAsync(
        Tournament tournament, string shiftGroup, string shiftTime)
    {
        var parsedTime = ParseShiftTime(shiftTime);

        var volunteers = (await _volunteerRepository.GetByTournamentIdAsync(tournament.Id)).ToList();
        var allGames = (await _gameRepository.GetByTournamentIdAsync(tournament.Id)).ToList();
        var gameLengthMinutes = tournament.GameLength ?? 0;

        var previousTime = gameLengthMinutes > 0 ? parsedTime.AddMinutes(-gameLengthMinutes) : (TimeOnly?)null;
        var nextTime = gameLengthMinutes > 0 ? parsedTime.AddMinutes(gameLengthMinutes) : (TimeOnly?)null;

        return volunteers.Select(v =>
        {
            var available = !IsTeamBusyAtTime(v.TeamId, allGames, parsedTime);
            var playsBefore = previousTime.HasValue && IsTeamBusyAtTime(v.TeamId, allGames, previousTime.Value);
            var playsAfter = nextTime.HasValue && IsTeamBusyAtTime(v.TeamId, allGames, nextTime.Value);
            var assigned = v.Assignments.Any(a => a.ShiftGroup == shiftGroup && a.ShiftTime == parsedTime);

            return new VolunteerAvailabilityDto(
                v.Id, v.Name, v.Assignments.Count, available, playsBefore, playsAfter, assigned);
        })
        .OrderByDescending(v => v.Available)
        .ThenBy(v => v.ShiftCount)
        .ThenBy(v => v.Name)
        .ToList();
    }

    public async Task<List<VolunteerAvailabilityDto>> GetAvailableVolunteersForSpecialAsync(
        string tournamentId, string shiftGroup)
    {
        var volunteers = (await _volunteerRepository.GetByTournamentIdAsync(tournamentId)).ToList();

        return volunteers.Select(v =>
        {
            var assigned = v.Assignments.Any(a => a.ShiftGroup == shiftGroup && a.ShiftTime is null);
            return new VolunteerAvailabilityDto(
                v.Id, v.Name, v.Assignments.Count, true, false, false, assigned);
        })
        .OrderBy(v => v.ShiftCount)
        .ThenBy(v => v.Name)
        .ToList();
    }

    public async Task AssignToShiftAsync(Tournament tournament, string shiftGroup, string shiftTime, string volunteerId)
    {
        var parsedTime = ParseShiftTime(shiftTime);

        var shiftGroups = await GetShiftGroupsAsync(tournament);
        var group = shiftGroups.FirstOrDefault(g => g.Name == shiftGroup);
        if (group is null || !group.Shifts.Contains(parsedTime))
            throw new ValidationException(
                [new ValidationFailure("shiftTime", $"'{shiftTime}' is not a valid shift time for '{shiftGroup}'.")]);

        var volunteer = await _volunteerRepository.GetByIdAsync(volunteerId, tournament.Id)
            ?? throw new NotFoundException(nameof(Volunteer), volunteerId);

        volunteer.AssignShift(shiftGroup, parsedTime);
        await _volunteerRepository.UpdateAsync(volunteer);
    }

    public async Task AssignToSpecialShiftAsync(string tournamentId, string shiftGroup, string volunteerId)
    {
        if (shiftGroup is not ("Set-up" or "Cleanup"))
            throw new ValidationException(
                [new ValidationFailure("shiftGroup", $"'{shiftGroup}' is not a valid special shift group.")]);

        var volunteer = await _volunteerRepository.GetByIdAsync(volunteerId, tournamentId)
            ?? throw new NotFoundException(nameof(Volunteer), volunteerId);

        volunteer.AssignShift(shiftGroup, null);
        await _volunteerRepository.UpdateAsync(volunteer);
    }

    public async Task UnassignFromShiftAsync(string tournamentId, string shiftGroup, string shiftTime, string volunteerId)
    {
        var parsedTime = ParseShiftTime(shiftTime);

        var volunteer = await _volunteerRepository.GetByIdAsync(volunteerId, tournamentId)
            ?? throw new NotFoundException(nameof(Volunteer), volunteerId);

        volunteer.UnassignShift(shiftGroup, parsedTime);
        await _volunteerRepository.UpdateAsync(volunteer);
    }

    public async Task UnassignFromSpecialShiftAsync(string tournamentId, string shiftGroup, string volunteerId)
    {
        var volunteer = await _volunteerRepository.GetByIdAsync(volunteerId, tournamentId)
            ?? throw new NotFoundException(nameof(Volunteer), volunteerId);

        volunteer.UnassignShift(shiftGroup, null);
        await _volunteerRepository.UpdateAsync(volunteer);
    }

    private static TimeOnly ParseShiftTime(string shiftTime)
    {
        if (!TimeOnly.TryParse(shiftTime, out var parsed))
            throw new ValidationException(
                [new ValidationFailure("shiftTime", $"Invalid time format: '{shiftTime}'.")]);
        return parsed;
    }

    private static bool IsTeamBusyAtTime(string? teamId, List<Game> allGames, TimeOnly shiftTime)
    {
        if (teamId is null) return false;
        return allGames.Any(g =>
            g.StartTime.HasValue
            && TimeOnly.FromDateTime(g.StartTime.Value) == shiftTime
            && (g.HomeTeamId == teamId || g.AwayTeamId == teamId || g.RefereeTeamId == teamId));
    }
}
