using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DebSharp.Utils.Compress
{
    public class ArchiveException : Exception
    {
        public ArchiveException(string message) : base(message) { }
        public ArchiveException(string message, Exception innerException) : base(message, innerException) { }
    }
}
