namespace ChangeDataInFile
{
    public class UpdatingData
    {
        public string Old { get; init; }
        public string New { get; init; }
        public void Deconstruct(out string OldValue, out string NewValue) => (OldValue, NewValue) = (Old, New);
    }
}