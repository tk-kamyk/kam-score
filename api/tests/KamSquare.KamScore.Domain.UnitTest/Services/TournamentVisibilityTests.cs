using FluentAssertions;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;
using KamSquare.KamScore.Domain.Services;

namespace KamSquare.KamScore.Domain.UnitTest.Services;

public class TournamentVisibilityTests
{
    private static Tournament Typed(string name, string ownerId, TournamentType type)
    {
        var tournament = Tournament.Create(name, Discipline.Volleyball, ownerId, type);
        return tournament;
    }

    private static readonly Tournament AlicePublic = Typed("Alice Public", "alice", TournamentType.Public);
    private static readonly Tournament AlicePrivate = Typed("Alice Private", "alice", TournamentType.Private);
    private static readonly Tournament AliceTemplate = Typed("Alice Template", "alice", TournamentType.Template);
    private static readonly Tournament BobPublic = Typed("Bob Public", "bob", TournamentType.Public);
    private static readonly Tournament BobPrivate = Typed("Bob Private", "bob", TournamentType.Private);
    private static readonly Tournament BobTemplate = Typed("Bob Template", "bob", TournamentType.Template);

    private static Tournament[] All() =>
        [AlicePublic, AlicePrivate, AliceTemplate, BobPublic, BobPrivate, BobTemplate];

    // --- VisibleInList ---

    [Fact]
    public void VisibleInList_Anonymous_ReturnsOnlyPublic()
    {
        var result = TournamentVisibility.VisibleInList(All(), viewerUserId: null, isAuthenticated: false, isAdmin: false);

        result.Select(t => t.Name).Should().BeEquivalentTo("Alice Public", "Bob Public");
    }

    [Fact]
    public void VisibleInList_AuthenticatedOwner_ReturnsPublicPlusOwn_NotOthersNonPublic()
    {
        var result = TournamentVisibility.VisibleInList(All(), viewerUserId: "alice", isAuthenticated: true, isAdmin: false);

        result.Select(t => t.Name).Should().BeEquivalentTo(
            "Alice Public", "Alice Private", "Alice Template", "Bob Public");
    }

    [Fact]
    public void VisibleInList_Admin_ReturnsAll()
    {
        var result = TournamentVisibility.VisibleInList(All(), viewerUserId: "admin", isAuthenticated: true, isAdmin: true);

        result.Should().HaveCount(6);
    }

    // --- CopySources ---

    [Fact]
    public void CopySources_Owner_IncludesAllTemplatesAndOwnPrivate_ExcludesOthersPrivate()
    {
        var result = TournamentVisibility.CopySources(All(), viewerUserId: "alice", isAdmin: false);

        result.Select(t => t.Name).Should().BeEquivalentTo(
            "Alice Public", "Bob Public", "Alice Template", "Bob Template", "Alice Private");
    }

    [Fact]
    public void CopySources_Admin_ReturnsAll()
    {
        var result = TournamentVisibility.CopySources(All(), viewerUserId: "admin", isAdmin: true);

        result.Should().HaveCount(6);
    }
}
