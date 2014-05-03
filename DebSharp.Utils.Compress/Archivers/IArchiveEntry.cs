using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DebSharp.Utils.Compress
{
    interface IArchiveEntry
    {
        public string GetName();

        public long GetSize();

        public static readonly long SIZE_UNKNOWN = -1;

        public bool IsDirectory();

        public DateTime GetLastModifiedDate();
    }
}
