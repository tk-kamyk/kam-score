namespace KamSquare.KamScore.Domain.Services;

// A station is an opaque colour index 0..Count-1 (null = none). The backend stays
// colour-agnostic — only the COUNT lives here, validated against. The actual colours are
// owned by the frontend palette (spa/src/volunteer/stations.ts), whose length MUST match
// Count. See docs/design/volunteer.md.
public static class StationPalette
{
    public const int Count = 8;
}
