 /*
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 */



using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DebSharp.Utils.Compress.Archivers.Ar
{
    /**
     * Implements the "ar" archive format as an input stream.
     * 
     * @NotThreadSafe
     * 
     */
    public class ArArchiveInputStream : ArchiveInputStream 
    {

        private Stream input;
        private long offset = 0;
        private bool closed;

        /*
         * If getNextEnxtry has been called, the entry metadata is stored in
         * currentEntry.
         */
        private ArArchiveEntry currentEntry = null;

        // Storage area for extra long names (GNU ar)
        private byte[] namebuffer = null;

        /*
         * The offset where the current entry started. -1 if no entry has been
         * called
         */
        private long entryOffset = -1;

        // cached buffers - must only be used locally in the class (COMPRESS-172 - reduce garbage collection)
        private readonly byte[] NAME_BUF = new byte[16];
        private readonly byte[] LAST_MODIFIED_BUF = new byte[12];
        private readonly byte[] ID_BUF = new byte[6];
        private readonly byte[] FILE_MODE_BUF = new byte[8];
        private readonly byte[] LENGTH_BUF = new byte[10];

        /**
         * Constructs an Ar input stream with the referenced stream
         * 
         * @param pInput
         *            the ar input stream
         */
        public ArArchiveInputStream(Stream pInput) 
        {
            input = pInput;
            closed = false;
        }

        /**
         * Returns the next AR entry in this stream.
         * 
         * @return the next AR entry.
         * @throws IOException
         *             if the entry could not be read
         */
        public ArArchiveEntry GetNextArEntry()
        {
            if (currentEntry != null) 
            {
                long entryEnd = entryOffset + currentEntry.GetLength();
                //IOUtils.skip(this, entryEnd - offset);
                Seek(entryEnd - offset, SeekOrigin.Current);
                currentEntry = null;
            }

            if (offset == 0) 
            {
                byte[] expected =  Encoding.ASCII.GetBytes(ArArchiveEntry.HEADER);
                byte[] realized = new byte[expected.Length];
                //int read = IOUtils.readFully(this, realized);
                int read = Read(realized, 0, expected.Length);
                if (read != expected.Length) {
                    throw new IOException("failed to read header. Occured at byte: " + GetBytesRead());
                }
                for (int i = 0; i < expected.Length; i++) 
                {
                    if (expected[i] != realized[i]) 
                    {
                        throw new IOException("invalid header " + System.Text.Encoding.ASCII.GetString(realized));
                    }
                }
            }

            if (offset % 2 != 0 && Read() < 0) 
            {
                // hit eof
                return null;
            }

            if (input.available() == 0) {
                return null;
            }

            Read(NAME_BUF, 0, NAME_BUF.Length);
            Read(LAST_MODIFIED_BUF, 0, LAST_MODIFIED_BUF.Length);
            Read(ID_BUF, 0, ID_BUF.Length);
            int userId = AsInt(ID_BUF, true);
            Read(ID_BUF, 0, ID_BUF.Length);
            Read(FILE_MODE_BUF, 0, FILE_MODE_BUF.Length);
            Read(LENGTH_BUF, 0, LENGTH_BUF.Length);

            {
                byte[] expected = Encoding.ASCII.GetBytes(ArArchiveEntry.TRAILER);
                byte[] realized = new byte[expected.Length];
                int read = Read(realized, 0, realized.Length);
                if (read != expected.Length) 
                {
                    throw new IOException("failed to read entry trailer. Occured at byte: " + GetBytesRead());
                }
                for (int i = 0; i < expected.Length; i++) 
                {
                    if (expected[i] != realized[i]) 
                    {
                        throw new IOException("invalid entry trailer. not read the content? Occured at byte: " + GetBytesRead());
                    }
                }
            }

            entryOffset = offset;

            //  GNU ar uses a '/' to mark the end of the filename; this allows for the use of spaces without the use of an extended filename.

            // entry name is stored as ASCII string
            String temp = System.Text.Encoding.ASCII.GetString(NAME_BUF).Trim();
            if (IsGNUStringTable(temp)) { 
                // GNU extended filenames entry
                currentEntry = ReadGNUStringTable(LENGTH_BUF);
                return GetNextArEntry();
            }

            long len = AsLong(LENGTH_BUF);
            if (temp.EndsWith("/")) 
            { 
                // GNU terminator
                temp = temp.Substring(0, temp.Length - 1);
            } 
            else if (IsGNULongName(temp)) 
            {
                int off = int.Parse(temp.Substring(1));// get the offset
                temp = GetExtendedName(off); // convert to the long name
            } 
            else if (IsBSDLongName(temp)) 
            {
                temp = GetBSDLongName(temp);
                // entry length contained the length of the file name in
                // addition to the real length of the entry.
                // assume file name was ASCII, there is no "standard" otherwise
                int nameLen = temp.Length;
                len -= nameLen;
                entryOffset += nameLen;
            }

            currentEntry = new ArArchiveEntry(temp, len, userId,
                                              AsInt(ID_BUF, true),
                                              AsInt(FILE_MODE_BUF, 8),
                                              AsLong(LAST_MODIFIED_BUF));
            return currentEntry;
        }

        /**
         * Get an extended name from the GNU extended name buffer.
         * 
         * @param offset pointer to entry within the buffer
         * @return the extended file name; without trailing "/" if present.
         * @throws IOException if name not found or buffer not set up
         */
        private String GetExtendedName(int offset)
        {
            if (namebuffer == null) 
            {
                throw new IOException("Cannot process GNU long filename as no // record was found");
            }
            for(int i=offset; i < namebuffer.Length; i++)
            {
                //if (namebuffer[i]=='\012')
                if (namebuffer[i]=='\f')
                {
                    if (namebuffer[i-1]=='/') 
                    {
                        i--; // drop trailing /
                    }
                    return System.Text.Encoding.ASCII.GetString(namebuffer, offset, i-offset);
                }
            }
            throw new IOException("Failed to read entry: "+offset);
        }
        private long AsLong(byte[] input) 
        {
            return long.Parse(System.Text.Encoding.ASCII.GetString(input).Trim());
        }

        private int asInt(byte[] input) 
        {
            return AsInt(input, 10, false);
        }

        private int AsInt(byte[] input, bool treatBlankAsZero) 
        {
            return AsInt(input, 10, treatBlankAsZero);
        }

        private int AsInt(byte[] input, int @base) 
        {
            return AsInt(input, @base, false);
        }

        private int AsInt(byte[] input, int @base, bool treatBlankAsZero) 
        {
            string @string = System.Text.Encoding.ASCII.GetString(input).Trim();
            if (@string.Length == 0 && treatBlankAsZero) 
            {
                return 0;
            }
            //return int.Parse(@string, @base);
            return int.Parse(@string);
        }

        /*
         * (non-Javadoc)
         * 
         * @see
         * org.apache.commons.compress.archivers.ArchiveInputStream#getNextEntry()
         */
        public override IArchiveEntry GetNextEntry() 
        {
            return GetNextArEntry();
        }

        /*
         * (non-Javadoc)
         * 
         * @see java.io.InputStream#close()
         */
        
        public override void Close() 
        {
            if (!closed) {
                closed = true;
                input.Close();
            }
            currentEntry = null;
        }

        /*
         * (non-Javadoc)
         * 
         * @see java.io.InputStream#read(byte[], int, int)
         */
        public override int Read(byte[] b, int off, int len)
        {
            int toRead = len;
            if (currentEntry != null) 
            {
                long entryEnd = entryOffset + currentEntry.GetLength();
                if (len > 0 && entryEnd > offset) 
                    toRead = (int) Math.Min(len, entryEnd - offset);
                else 
                    return -1;
            }
            int ret = this.input.Read(b, off, toRead);
            Count(ret);
            offset += ret > 0 ? ret : 0;
            return ret;
        }

        /**
         * Checks if the signature matches ASCII "!&lt;arch&gt;" followed by a single LF
         * control character
         * 
         * @param signature
         *            the bytes to check
         * @param length
         *            the number of bytes to check
         * @return true, if this stream is an Ar archive stream, false otherwise
         */
        public static bool Matches(byte[] signature, int length) 
        {
            // 3c21 7261 6863 0a3e

            if (length < 8) 
                return false;

            if (signature[0] != 0x21)
                return false;

            if (signature[1] != 0x3c) 
                return false;

            if (signature[2] != 0x61)
                return false;

            if (signature[3] != 0x72)
                return false;

            if (signature[4] != 0x63)
                return false;

            if (signature[5] != 0x68)
                return false;

            if (signature[6] != 0x3e)
                return false;

            if (signature[7] != 0x0a)
                return false;

            return true;
        }

        static string BSD_LONGNAME_PREFIX = "#1/";
        private static int BSD_LONGNAME_PREFIX_LEN =
            BSD_LONGNAME_PREFIX.Length;
        private static string BSD_LONGNAME_PATTERN =
            "^" + BSD_LONGNAME_PREFIX + "\\d+";

        /**
         * Does the name look like it is a long name (or a name containing
         * spaces) as encoded by BSD ar?
         *
         * <p>From the FreeBSD ar(5) man page:</p>
         * <pre>
         * BSD   In the BSD variant, names that are shorter than 16
         *       characters and without embedded spaces are stored
         *       directly in this field.  If a name has an embedded
         *       space, or if it is longer than 16 characters, then
         *       the string "#1/" followed by the decimal represen-
         *       tation of the length of the file name is placed in
         *       this field. The actual file name is stored immedi-
         *       ately after the archive header.  The content of the
         *       archive member follows the file name.  The ar_size
         *       field of the header (see below) will then hold the
         *       sum of the size of the file name and the size of
         *       the member.
         * </pre>
         *
         * @since 1.3
         */
        private static bool IsBSDLongName(string name) 
        {
            return name != null && Regex.IsMatch(name, BSD_LONGNAME_PATTERN);
        }

        /**
         * Reads the real name from the current stream assuming the very
         * first bytes to be read are the real file name.
         *
         * @see #isBSDLongName
         *
         * @since 1.3
         */
        private string GetBSDLongName(String bsdLongName) 
        {
            int nameLen =
                int.Parse(bsdLongName.Substring(BSD_LONGNAME_PREFIX_LEN));
            byte[] name = new byte[nameLen];
            int read = input.Read(name, 0, name.Length);
            Count(read);
            if (read != nameLen) 
            {
                throw new EndOfStreamException();
            }
            return System.Text.Encoding.ASCII.GetString(name);
        }

        private static String GNU_STRING_TABLE_NAME = "//";

        /**
         * Is this the name of the "Archive String Table" as used by
         * SVR4/GNU to store long file names?
         *
         * <p>GNU ar stores multiple extended filenames in the data section
         * of a file with the name "//", this record is referred to by
         * future headers.</p>
         *
         * <p>A header references an extended filename by storing a "/"
         * followed by a decimal offset to the start of the filename in
         * the extended filename data section.</p>
         * 
         * <p>The format of the "//" file itself is simply a list of the
         * long filenames, each separated by one or more LF
         * characters. Note that the decimal offsets are number of
         * characters, not line or string number within the "//" file.</p>
         */
        private static bool IsGNUStringTable(String name) 
        {
            return GNU_STRING_TABLE_NAME.Equals(name);
        }

        /**
         * Reads the GNU archive String Table.
         *
         * @see #isGNUStringTable
         */
        private ArArchiveEntry ReadGNUStringTable(byte[] length) 
        {
            int bufflen = asInt(length); // Assume length will fit in an int
            namebuffer = new byte[bufflen];
            int read = Read(namebuffer, 0, bufflen);
            if (read != bufflen)
                throw new IOException("Failed to read complete // record: expected="
                                      + bufflen + " read=" + read);
            return new ArArchiveEntry(GNU_STRING_TABLE_NAME, bufflen);
        }

        private static string GNU_LONGNAME_PATTERN = "^/\\d+";

        /**
         * Does the name look like it is a long name (or a name containing
         * spaces) as encoded by SVR4/GNU ar?
         *
         * @see #isGNUStringTable
         */
        private bool IsGNULongName(String name) 
        {
            return name != null && Regex.IsMatch(name, GNU_LONGNAME_PATTERN);
        }
    }
}
