// <copyright file="ReaderBuffer.cs" company="None">
//    <para>
//    This program is free software: you can redistribute it and/or
//    modify it under the terms of the BSD license.</para>
//    <para>
//    This work is distributed in the hope that it will be useful, but
//    WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.</para>
//    <para>
//    See the LICENSE.txt file for more details.</para>
//    Original code as generated by Grammatica 1.6 Copyright (c) 
//    2003-2015 Per Cederberg. All rights reserved.
//    Updates Copyright (c) 2016 Jeremy Gibbons. All rights reserved
// </copyright>

namespace PerCederberg.Grammatica.Runtime
{
   using System;
   using System.IO;

   /// <summary>
   /// A character buffer that automatically reads from an input source
   /// stream when needed. This class keeps track of the current position
   /// in the buffer and its line and column number in the original input
   /// source. It allows unlimited look-ahead of characters in the input,
   /// reading and buffering the required data internally. As the
   /// position is advanced, the buffer content prior to the current
   /// position is subject to removal to make space for reading new
   /// content. A few characters before the current position are always
   /// kept to enable boundary condition checks.
   /// </summary>    
   public class ReaderBuffer
   {
      /// <summary>
      /// The stream reading block size. All reads from the underlying
      /// character stream will be made in multiples of this block size.
      /// Also the character buffer size will always be a multiple of
      /// this factor.
      /// </summary>      
      public const int BlockSize = 1024;

      /// <summary>
      /// The character buffer.
      /// </summary>
      private char[] buffer = new char[BlockSize * 4];

      /// <summary>
      /// The current character buffer position.
      /// </summary>
      private int pos = 0;

      /// <summary>
      /// The number of characters in the buffer.
      /// </summary>
      private int length = 0;

      /// <summary>
      /// The input source character reader.
      /// </summary>
      private TextReader input = null;

      /// <summary>
      /// The line number of the next character to read. This value will
      /// be incremented when reading past line breaks.
      /// </summary>
      private int line = 1;

      /// <summary>
      /// The column number of the next character to read. This value
      /// will be updated for every character read.
      /// </summary>
      private int column = 1;

      /// <summary>
      /// Initializes a new instance of the <see cref="ReaderBuffer"/> class,
      /// i.e. a new tokenizer character buffer.
      /// </summary>
      /// <param name="input">The input source character reader</param>         
      public ReaderBuffer(TextReader input)
      {
         this.input = input;
      }

      /// <summary>
      /// Gets the current buffer position (read-only).
      /// </summary>
      public int Position
      {
         get
         {
            return this.pos;
         }
      }

      /// <summary>
      /// Gets the current line number (read-only). This number
      /// is the line number of the next character to read.
      /// </summary>
      public int LineNumber
      {
         get
         {
            return this.line;
         }
      }

      /// <summary>
      /// Gets the current column number (read-only). This number
      /// is the column number of the next character to read.
      /// </summary>
      public int ColumnNumber
      {
         get
         {
            return this.column;
         }
      }

      /// <summary>
      /// Gets the current character buffer length (read-only).
      /// Note that the length may increase (and decrease) as more
      /// characters are read from the input source or removed to
      /// free up space.
      /// </summary>
      public int Length
      {
         get
         {
            return this.length;
         }
      }

      /// <summary>
      /// Discards all resources used by this buffer. This will also
      /// close the source input stream. Disposing a previously disposed
      /// buffer has no effect.
      /// </summary>
      public void Reset()
      {
         this.buffer = null;
         this.pos = 0;
         this.length = 0;
         if (this.input != null)
         {
            try
            {
               this.input.Close();
            }
            catch (Exception)
            {
               // Do nothing
            }

            this.input = null;
         }
      }

      /// <summary>
      /// Returns a substring already in the buffer. Note that this
      /// method may behave in unexpected ways when performing
      /// operations that modifies the buffer content.
      /// </summary>
      /// <param name="index">The start index, inclusive</param>
      /// <param name="length">The substring length</param>
      /// <returns>The specified substring</returns>
      /// <exception cref="IndexOutOfRangeException">
      /// If one of the indices is negative or larger than length
      /// </exception>  
      public string Substring(int index, int length)
      {
         return new string(this.buffer, index, length);
      }

      /// <summary>
      /// Returns the current content of the buffer as a string. Note
      /// that content before the current position will also be
      /// returned.
      /// </summary>
      /// <returns>The current buffer contents</returns>         
      public override string ToString()
      {
         return new string(this.buffer, 0, this.length);
      }

      /// <summary><para>
      /// Returns a character relative to the current position. This
      /// method may read from the input source and may also trim the
      /// buffer content prior to the current position. The result of
      /// calling this method may therefore be that the buffer length
      /// and content have been modified.
      /// </para><para>
      /// The character offset must be positive, but is allowed to span
      /// the entire size of the input source stream. Note that the
      /// internal buffer must hold all the intermediate characters,
      /// which may be wasteful if the offset is too large.
      /// </para></summary>
      /// <param name="offset">The character offset</param>
      /// <returns>
      /// The character found, as an integer between 0 and 65535,
      /// or -1 if the end of the stream has been reached
      /// </returns>
      /// <exception cref="IOException">
      /// If an I/O error occurred
      /// </exception>
      public int Peek(int offset)
      {
         int index = this.pos + offset;

         // Avoid most calls to EnsureBuffered(), since we are in a
         // performance hotspot here. This check is not exhaustive,
         // but only present here to speed things up.
         if (index >= this.length)
         {
            this.EnsureBuffered(offset + 1);
            index = this.pos + offset;
         }

         return (index >= this.length) ? -1 : this.buffer[index];
      }

      /// <summary>
      /// Reads the specified number of characters from the current
      /// position. This will also move the current position forward.
      /// This method will not attempt to move beyond the end of the
      /// input source stream. When reaching the end of file, the
      /// returned string might be shorter than requested. Any
      /// remaining characters will always be returned before returning
      /// null.
      /// </summary>
      /// <param name="offset">The character offset</param>
      /// <returns>
      /// A string containing the characters that were read,
      /// or null if no characters remain in the buffer
      /// </returns>
      /// <exception cref="IOException">If an I/O error occurred</exception>
      public string Read(int offset)
      {
         int count;
         string result;

         this.EnsureBuffered(offset + 1);
         if (this.pos >= this.length)
         {
            return null;
         }
         else
         {
            count = this.length - this.pos;
            if (count > offset)
            {
               count = offset;
            }

            this.UpdateLineColumnNumbers(count);
            result = new string(this.buffer, this.pos, count);
            this.pos += count;

            if (this.input == null && this.pos >= this.length)
            {
               this.Reset();
            }

            return result;
         }
      }

      /// <summary>
      /// Updates the line and column numbers counters. This method
      /// requires all the characters to be processed (i.e. returned
      /// as read) to be present in the buffer, starting at the
      /// current buffer position.
      /// </summary>
      /// <param name="offset">The number of characters to process</param>
      private void UpdateLineColumnNumbers(int offset)
      {
         for (int i = 0; i < offset; i++)
         {
            if (this.buffer[this.pos + i] == '\n')
            {
               this.line++;
               this.column = 1;
            }
            else
            {
               this.column++;
            }
         }
      }

      /// <summary>
      /// Ensures that the specified offset is read into the buffer.
      /// This method will read characters from the input stream and
      /// appends them to the buffer if needed. This method is safe to
      /// call even after end of file has been reached. This method also
      /// handles removal of characters at the beginning of the buffer
      /// once the current position is high enough. It will also enlarge
      /// the buffer as needed.
      /// </summary>
      /// <param name="offset">The read offset</param>
      /// <exception cref="IOException">
      /// If an error was encountered while reading
      /// the input stream
      /// </exception>
      private void EnsureBuffered(int offset)
      {
         int size;
         int readSize;

         // Check for end of stream or already read characters
         if (this.input == null || this.pos + offset < this.length)
         {
            return;
         }

         // Remove (almost all) old characters from buffer
         if (this.pos > ReaderBuffer.BlockSize)
         {
            this.length -= this.pos - 16;
            Array.Copy(this.buffer, this.pos - 16, this.buffer, 0, this.length);
            this.pos = 16;
         }

         // Calculate number of characters to read
         size = this.pos + offset - this.length + 1;
         if (size % ReaderBuffer.BlockSize != 0)
         {
            size = (1 + (size / ReaderBuffer.BlockSize)) * ReaderBuffer.BlockSize;
         }

         this.EnsureCapacity(this.length + size);

         // Read characters
         try
         {
            while (this.input != null && size > 0)
            {
               readSize = this.input.Read(this.buffer, this.length, size);
               if (readSize > 0)
               {
                  this.length += readSize;
                  size -= readSize;
               }
               else
               {
                  this.input.Close();
                  this.input = null;
               }
            }
         }
         catch (IOException)
         {
            this.input = null;
            throw;
         }
      }

      /// <summary>
      /// Ensures that the buffer has at least the specified capacity.
      /// </summary>
      /// <param name="size">The minimum buffer size</param>         
      private void EnsureCapacity(int size)
      {
         if (this.buffer.Length >= size)
         {
            return;
         }

         if (size % ReaderBuffer.BlockSize != 0)
         {
            size = (1 + (size / ReaderBuffer.BlockSize)) * ReaderBuffer.BlockSize;
         }

         Array.Resize(ref this.buffer, size);
      }
   }
}
