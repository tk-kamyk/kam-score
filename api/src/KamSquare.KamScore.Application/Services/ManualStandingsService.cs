using FluentValidation;
using FluentValidation.Results;
using KamSquare.KamScore.Application.Interfaces;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;
using KamSquare.KamScore.Domain.Exceptions;
using KamSquare.KamScore.Domain.Services.Formats;
using KamSquare.KamScore.Domain.ValueObjects;

namespace KamSquare.KamScore.Application.Services;

/// <summary>
/// Owns manual standings entry for Custom-format phases: validates phase format +
/// status, writes the ordering to the group, and persists the structure. Returns
/// the recomputed standings so the caller can respond with fresh data.
/// </summary>
public class ManualStandingsService
{
    private readonly ITournamentStructureRepository _structureRepository;

    public ManualStandingsService(ITournamentStructureRepository structureRepository)
    {
        _structureRepository = structureRepository;
    }

    public async Task<List<Standing>> UpdateAsync(
        string tournamentId,
        string phaseId,
        string groupId,
        IReadOnlyList<string> orderedTeamIds,
        TournamentStructure structure)
    {
        var phase = structure.GetPhase(phaseId);

        if (phase.Format != PhaseFormat.Custom)
            throw new ValidationException(
                [new ValidationFailure("Format", "Manual standings are only supported for Custom-format phases.")]);

        if (phase.Status != PhaseStatus.InProgress)
            throw new PhaseStateException(phase.Name, "save manual standings",
                $"phase must be InProgress, but is {phase.Status}");

        var group = phase.Groups.FirstOrDefault(g => g.Id == groupId)
            ?? throw new NotFoundException(nameof(Group), groupId);

        try
        {
            group.SetManualStandingOrder(orderedTeamIds);
        }
        catch (ArgumentException ex)
        {
            throw new ValidationException(
                [new ValidationFailure("OrderedTeamIds", ex.Message)]);
        }

        await _structureRepository.UpdateAsync(structure);

        return CustomStandingsRanker.Calculate(group);
    }
}
