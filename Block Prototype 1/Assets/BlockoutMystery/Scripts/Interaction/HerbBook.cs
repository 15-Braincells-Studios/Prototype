namespace BlockoutMystery
{
    /// <summary>The herb book on the chest of drawers. Opens the book view (pages in <see cref="CaseFile.BookPages"/>).</summary>
    public class HerbBook : Interactable
    {
        public override string Prompt => "Read the book";
        public override void Interact() => UIRoot.Instance.Book.Open();
    }
}
