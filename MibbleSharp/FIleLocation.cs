﻿// <copyright file="FileLocation.cs" company="None">
//    <para>
//    This work is free software; you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published
//    by the Free Software Foundation; either version 2 of the License,
//    or (at your option) any later version.</para>
//    <para>
//    This work is distributed in the hope that it will be useful, but
//    WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
//    General Public License for more details.</para>
//    <para>
//    You should have received a copy of the GNU General Public License
//    along with this program; if not, write to the Free Software
//    Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307
//    USA</para>
//    Original Java code Copyright (c) 2004-2016 Per Cederberg. All
//    rights reserved.
//    C# conversion Copyright (c) 2016 Jeremy Gibbons. All rights reserved
// </copyright>

namespace MibbleSharp
{
   using System;

   /// <summary>
   /// A file location. This class contains a reference to an exact
   /// location (line, column) inside a text file.
   /// </summary>
   [Serializable]
   public class FileLocation
   {
      /// <summary>
      /// Gets the File Location's filename
      /// </summary>
      public string File { get; }

      /// <summary>
      /// Gets the File Location's line number
      /// </summary>
      public int LineNumber { get; }

      /// <summary>
      /// Gets the File Location's column Number
      /// </summary>
      public int ColumnNumber { get; }

      /// <summary>
      /// Initializes a new instance of the <see cref="FileLocation"/> class
      /// without specifying any particular position.
      /// </summary>
      /// <param name="file">The filename for this File Location</param>
      public FileLocation(string file) : this(file, -1, -1)
      {
      }

      /// <summary>
      /// Initializes a new instance of the <see cref="FileLocation"/> class
      /// with a precise location in the file.
      /// </summary>
      /// <remarks>
      /// Location validity is not checked at this time,
      /// except for negative line and column values
      /// </remarks>
      /// <param name="file">Filename for location. Existence is not checked</param>
      /// <param name="line">Line number of location. Negative values are reset to -1</param>
      /// <param name="column">Column number of location. Negative values are reset to -1</param>
      public FileLocation(string file, int line, int column)
      {
         this.File = file;
         this.LineNumber = line < -1 ? -1 : line;
         this.ColumnNumber = column < -1 ? -1 : column;
      }

      /// <summary>
      /// Reads the specified line from the file. If the file couldn't
      /// be opened or read correctly, null will be returned. The line
      /// will NOT contain the terminating '\n' character. This method
      /// takes special care to only count the linefeed (LF, 0x0A)
      /// character as a valid newline.
      /// </summary>
      /// <returns>the line read, or null if not found</returns>
      public string ReadLine()
      {
         string str = null;
         int count = 1;
         int ch;

         if (this.File == null || this.LineNumber < 0)
         {
            return null;
         }

         using (System.IO.StreamReader sr = System.IO.File.OpenText(this.File))
         {
            // Only count line-feed characters in files with invalid line
            // termination sequences. The default readLine() method doesn't
            // quite do the right thing in those cases... (bug #16252)
            while (count < this.LineNumber && (ch = sr.Read()) >= 0)
            {
               if (ch == '\n')
               {
                  count++;
               }
            }

            str = sr.ReadLine();
         }

         return str;
      }

      /// <summary>
      /// Get string representation of the FileLocation
      /// </summary>
      /// <returns>A string representation of the FileLocation object</returns>
      public override string ToString()
      {
         return "File: " + this.File + ", Line: " + this.LineNumber + ", Column: " + this.ColumnNumber;
      }
   }
}
