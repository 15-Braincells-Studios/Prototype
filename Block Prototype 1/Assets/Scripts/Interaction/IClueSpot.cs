namespace PreventTheMurder
{
    /// <summary>
    /// A place in the scene that can hold a clue. GameManager fills every spot from the active
    /// ScenarioData by <see cref="SpotId"/> (null = nothing there).
    /// </summary>
    public interface IClueSpot
    {
        string SpotId { get; }
        void AssignClue(ClueData clue);
    }
}
