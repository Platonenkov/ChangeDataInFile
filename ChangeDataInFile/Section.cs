using System.Collections.Generic;

namespace ChangeDataInFile
{
    public record Section
    {
        public string FileMask { get; init; }
        public IEnumerable<UpdatingData> Updating { get; init; }
    }
}