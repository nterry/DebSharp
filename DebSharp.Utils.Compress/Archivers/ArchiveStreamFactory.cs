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

using DebSharp.Utils.Compress.Archivers.Ar;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DebSharp.Utils.Compress
{
    public class ArchiveStreamFactory
    {
        /**
        * Constant used to identify the AR archive format.
        * @since 1.1
        */
        public static readonly String AR = "ar";

        /**
        * Create an archive input stream from an archiver name and an input stream.
        * 
        * @param archiverName the archive name, i.e. "ar", "arj", "zip", "tar", "jar", "dump" or "cpio"
        * @param in the input stream
        * @return the archive input stream
        * @throws ArchiveException if the archiver name is not known
        * @throws StreamingNotSupportedException if the format cannot be
        * read from a stream
        * @throws IllegalArgumentException if the archiver name or stream is null
        */
        public ArchiveInputStream createArchiveInputStream(string archiverName, Stream @in)
        {
            if (archiverName == null) {
                throw new ArgumentNullException("Archivername must not be null.");
            }

            if (@in == null) {
                throw new ArgumentNullException("InputStream must not be null.");
            }

            if (AR.Equals(archiverName, StringComparison.InvariantCultureIgnoreCase))
            {
                return new ArArchiveInputStream(@in);
            }
            
            throw new ArchiveException(string.Format("Archiver: {0} not found.", archiverName));
        }

        /**
         * Create an archive output stream from an archiver name and an input stream.
         * 
         * @param archiverName the archive name, i.e. "ar", "zip", "tar", "jar" or "cpio"
         * @param out the output stream
         * @return the archive output stream
         * @throws ArchiveException if the archiver name is not known
         * @throws StreamingNotSupportedException if the format cannot be
         * written to a stream
         * @throws IllegalArgumentException if the archiver name or stream is null
         */
        public ArchiveOutputStream createArchiveOutputStream(string archiverName, Stream @out) 
        {
            if (archiverName == null) {
                throw new ArgumentNullException("Archivername must not be null.");
            }
            if (@out == null) {
                throw new ArgumentNullException("OutputStream must not be null.");
            }

            if (AR.Equals(archiverName, StringComparison.InvariantCultureIgnoreCase))
            {
                return new ArArchiveOutputStream(@out);
            }
            throw new ArchiveException(string.Format("Archiver: {0} not found.", archiverName));
        }
    }
}
