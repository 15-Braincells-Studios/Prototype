namespace PreventTheMurder
{
    /// <summary>Anything the player can use with E. Return null or empty from GetPrompt to be ignored for now.</summary>
    public interface IInteractable
    {
        string GetPrompt();
        void Interact(PlayerInteractor player);
    }
}
