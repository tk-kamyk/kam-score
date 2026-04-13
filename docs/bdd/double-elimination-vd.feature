Feature: Double Elimination (VD) Phase Format

  # The exact bracket composition (14 games / 7 rounds / seeding / pairings) is
  # exercised by DoubleEliminationVdGeneratorTests. These scenarios only cover
  # user-visible behaviour.

  Background:
    Given a tournament with a phase in DoubleEliminationVd format

  Scenario: Owner generates a VD bracket for an 8-team group
    Given a group with exactly 8 teams
    When the owner generates games for the phase
    Then a VD bracket is created and scheduled

  Scenario: VD format rejects groups that do not have 8 teams
    Given a group with a team count other than 8
    When the owner tries to generate games
    Then the request is rejected

  Scenario: Standings after full completion reflect the VD position map
    Given all VD games for a group have been played
    When standings are requested
    Then 1st/2nd/7th/8th are unique positions while 3rd-4th and 5th-6th are shared
