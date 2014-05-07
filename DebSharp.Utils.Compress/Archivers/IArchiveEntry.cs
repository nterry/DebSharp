using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DebSharp.Utils.Compress
{
    public interface IArchiveEntry
    {
        string GetName();

        long GetLength();

        //static readonly long SIZE_UNKNOWN = -1;

        bool IsDirectory();

        DateTime GetLastModifiedDate();
    }
}
