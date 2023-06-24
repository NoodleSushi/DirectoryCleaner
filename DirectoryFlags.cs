using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirectoryCleaner
{
    [Flags]
    public enum DirectoryFlags
    {
        None = 0,
        Forced = 1,
        Recursive = 2,
        DeleteFolder = 4,
        DeleteUnzipped = 8,
        DeleteEmpty = 16,
    }
}
