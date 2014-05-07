using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DebSharp.Utils.Compress.Utils;

namespace DebSharp.Utils.Compress.Archivers.Ar
{
    public class ArArchiveEntry : IArchiveEntry
    {
        /** The header for each entry */
        public static readonly string HEADER = "!<arch>\n";

        /** The trailer for each entry */
        public static readonly string TRAILER = "`\012";

        /**
         * SVR4/GNU adds a trailing / to names; BSD does not.
         * They also vary in how names longer than 16 characters are represented.
         * (Not yet fully supported by this implementation)
         */
        public string Name { get; private set; }
        public int UserId { get; private set; }
        public int GroupId { get; private set; }
        public int Mode { get; private set; }
        public long LastModified { get; private set; } // = (octal) 0100644
        public long Length { get; private set; }
        public static int DEFAULT_MODE 
        { 
            get { return 33188; }  
        }

        /**
         * Create a new instance using a couple of default values.
         *
         * <p>Sets userId and groupId to 0, the octal file mode to 644 and
         * the last modified time to the current time.</p>
         *
         * @param name name of the entry
         * @param length length of the entry in bytes
         */
        public ArArchiveEntry(String name, long length) 
            : this(name, length, 0, 0, DEFAULT_MODE, DateTime.Now.Ticks / 10000)  { }

        /**
         * Create a new instance.
         *
         * @param name name of the entry
         * @param length length of the entry in bytes
         * @param userId numeric user id
         * @param groupId numeric group id
         * @param mode file mode
         * @param lastModified last modified time in seconds since the epoch
         */
        public ArArchiveEntry(string name, long length, int userId, int groupId,
                              int mode, long lastModified) {
            Name = name;
            Length = length;
            UserId = userId;
            GroupId = groupId;
            Mode = mode;
            LastModified = lastModified;
        }

        /**
         * Create a new instance using the attributes of the given file
         */
        public ArArchiveEntry(string inputFile, String entryName) 
            : this(entryName, File.Exists(inputFile) ? new FileInfo(inputFile).Length : 0, 0, 0,
            DEFAULT_MODE, File.GetLastWriteTime(inputFile).Ticks / 1000) { }

        public string GetName()
        {
            return Name;
        }

        public long GetLength()
        {
            return Length;
        }

        /**
         * Last modified time in seconds since the epoch.
         */
        public DateTime GetLastModifiedDate() 
        {
            var doubleLastModified = (double)LastModified;
            return doubleLastModified.ConvertFromUnixTimestamp();  
        }

        public bool IsDirectory() 
        {
            return false;
        }

        public int HashCode() 
        {
            int prime = 31;
            int result = 1;
            result = prime * result + (Name == null ? 0 : Name.GetHashCode());
            return result;
        }

        public override bool Equals(Object obj) 
        {
            if (this == obj) {
                return true;
            }
            if (obj == null || this.GetType() != obj.GetType()) {
                return false;
            }
            ArArchiveEntry other = (ArArchiveEntry) obj;
            if (Name == null) {
                if (other.Name != null) {
                    return false;
                }
            } else if (!Name.Equals(other.Name)) {
                return false;
            }
            return true;
        }
    }
}
