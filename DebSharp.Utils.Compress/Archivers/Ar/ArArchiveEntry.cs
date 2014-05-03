using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        string Name { get; private set; }
        int UserId { get; private set; }
        int GroupId { get; private set; }
        int Mode { get; private set; }
        long LastModified { get; private set; } // = (octal) 0100644
        long Length { get; private set; }
        static int DEFAULT_MODE 
        { 
            get { return DEFAULT_MODE  = 33188; } 
            private set; 
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
        public ArArchiveEntry(File inputFile, String entryName) 
            : this(entryName, inputFile.isFile() ? inputFile.length() : 0, 0, 0,
            DEFAULT_MODE, inputFile.lastModified() / 1000) { }

        /**
         * Last modified time in seconds since the epoch.
         */
        public DateTime GetLastModifiedDate() 
        {
            //TODO: Not sure this is actual time since epoch
            return new DateTime(1000 * LastModified);
        }

        public bool IsDirectory() 
        {
            return false;
        }

        public int HashCode() 
        {
            int prime = 31;
            int result = 1;
            result = prime * result + (Name == null ? 0 : Name.hashCode());
            return result;
        }

        public bool Equals(Object obj) 
        {
            if (this == obj) {
                return true;
            }
            if (obj == null || getClass() != obj.getClass()) {
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
