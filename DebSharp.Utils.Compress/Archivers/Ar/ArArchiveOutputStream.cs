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
using System.Threading.Tasks;

namespace DebSharp.Utils.Compress.Archivers.Ar
{
    /**
    * Implements the "ar" archive format as an output stream.
    * 
    * @NotThreadSafe
    */
    public class ArArchiveOutputStream : ArchiveOutputStream
    {
        /** Fail if a long file name is required in the archive. */
        public static readonly int LONGFILE_ERROR = 0;

        /** BSD ar extensions are used to store long file names in the archive. */
        public static readonly int LONGFILE_BSD = 1;

        private readonly Stream @out;
        private long entryOffset = 0;
        private ArArchiveEntry prevEntry;
        private bool haveUnclosedEntry = false;
        private int longFileMode = LONGFILE_ERROR;

        /** indicates if this archive is finished */
        private bool finished = false;

        public ArArchiveOutputStream( Stream pOut ) {
            this.@out = pOut;
        }

        /**
         * Set the long file mode.
         * This can be LONGFILE_ERROR(0) or LONGFILE_BSD(1).
         * This specifies the treatment of long file names (names &gt;= 16).
         * Default is LONGFILE_ERROR.
         * @param longFileMode the mode to use
         * @since 1.3
         */
        public void SetLongFileMode(int longFileMode) 
        {
            this.longFileMode = longFileMode;
        }

        private long WriteArchiveHeader() 
        {
            byte [] header = Encoding.ASCII.GetBytes(ArArchiveEntry.HEADER);
            @out.Write(header, 0, header.Length);
            return header.Length;
        }

        public override void CloseArchiveEntry() 
        {
            if(finished) 
            {
                throw new IOException("Stream has already been finished");
            }
            if (prevEntry == null || !haveUnclosedEntry)
            {
                throw new IOException("No current entry to close");
            }
            if (entryOffset % 2 != 0) 
            {
                var pad = Encoding.ASCII.GetBytes("\n");
                @out.Write(pad, 0, pad.Length); // Pad byte
            }
            haveUnclosedEntry = false;
        }

        public void PutArchiveEntry(IArchiveEntry pEntry ) 
        {
            if(finished) {
                throw new IOException("Stream has already been finished");
            }

            ArArchiveEntry pArEntry = (ArArchiveEntry)pEntry;
            if (prevEntry == null) 
            {
                WriteArchiveHeader();
            } else 
            {
                if (prevEntry.GetLength() != entryOffset) 
                {
                    throw new IOException("length does not match entry (" + prevEntry.GetLength() + " != " + entryOffset);
                }

                if (haveUnclosedEntry) 
                {
                    CloseArchiveEntry();
                }
            }

            prevEntry = pArEntry;

            WriteEntryHeader(pArEntry);

            entryOffset = 0;
            haveUnclosedEntry = true;
        }

        private long Fill( long pOffset, long pNewOffset, char pFill )
        { 
            long diff = pNewOffset - pOffset;

            if (diff > 0) 
            {
                for (int i = 0; i < diff; i++) 
                {
                    write(pFill);
                }
            }

            return pNewOffset;
        }

        private long Write( string data ) 
        {
            byte[] bytes = data.getBytes("ascii");
            Write(bytes, 0, bytes.Length);
            return bytes.Length;
        }

        private long WriteEntryHeader( ArArchiveEntry pEntry ) 
        {
            long offset = 0;
            bool mustAppendName = false;

            string n = pEntry.GetName();
            if (LONGFILE_ERROR == longFileMode && n.Length > 16) 
            {
                throw new IOException("filename too long, > 16 chars: "+n);
            }
            if (LONGFILE_BSD == longFileMode && 
                (n.Length > 16 || n.IndexOf(" ") > -1)) 
            {
                mustAppendName = true;
                offset += Write(ArArchiveInputStream.BSD_LONGNAME_PREFIX
                                + string.ValueOf(n.Length));
            } else {
                offset += Write(n);
            }

            offset = Fill(offset, 16, ' ');
            string m = "" + pEntry.GetLastModifiedDate();
            if (m.Length > 12) 
            {
                throw new IOException("modified too long");
            }
            offset += Write(m);

            offset = Fill(offset, 28, ' ');
            String u = "" + pEntry.GetUserId();
            if (u.Length > 6) 
            {
                throw new IOException("userid too long");
            }
            offset += Write(u);

            offset = Fill(offset, 34, ' ');
            String g = "" + pEntry.GetGroupId();
            if (g.Length > 6) 
            {
                throw new IOException("groupid too long");
            }
            offset += Write(g);

            offset = Fill(offset, 40, ' ');
            string fm = "" + int.ToString(pEntry.GetMode(), 8);
            if (fm.Length > 8) 
            {
                throw new IOException("filemode too long");
            }
            offset += Write(fm);

            offset = Fill(offset, 48, ' ');
            string s =
                string.ValueOf(pEntry.GetLength()
                               + (mustAppendName ? n.Length : 0));
            if (s.Length > 10) 
            {
                throw new IOException("size too long");
            }
            offset += Write(s);

            offset = Fill(offset, 58, ' ');

            offset += Write(ArArchiveEntry.TRAILER);

            if (mustAppendName) {
                offset += Write(n);
            }

            return offset;
        }

        public override void Write(byte[] b, int off, int len) 
        {
            @out.Write(b, off, len);
            count(len);
            entryOffset += len;
        }

        /**
         * Calls finish if necessary, and then closes the OutputStream
         */
        public override void Close() 
        {
            if(!finished) {
                Finish();
            }
            @out.Close();
            prevEntry = null;
        }


        public override IArchiveEntry CreateArchiveEntry(string inputFile, string entryName)
        {
            if(finished) 
            {
                throw new IOException("Stream has already been finished");
            }
            return new ArArchiveEntry(inputFile, entryName);
        }

        public override void Finish() 
        {
            if(haveUnclosedEntry) {
                throw new IOException("This archive contains unclosed entries.");
            } else if(finished) {
                throw new IOException("This archive has already been finished");
            }
            finished = true;
        }
    }
}
