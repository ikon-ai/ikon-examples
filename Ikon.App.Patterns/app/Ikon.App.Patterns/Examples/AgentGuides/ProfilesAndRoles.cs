namespace Ikon.App.Patterns.Examples;

// The profiles-and-roles guide sections.

#region example:profiles-attributes-type
public sealed class GameAttributes : IProfileAttributes
{
    public int HighScore { get; set; }
    public string FavouriteTrack { get; set; } = "";
}
#endregion
