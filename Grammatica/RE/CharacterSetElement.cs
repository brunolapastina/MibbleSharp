// <copyright file="CharacterSetElement.cs" company="None">
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

namespace PerCederberg.Grammatica.Runtime.RE
{
   using PerCederberg.Grammatica.Runtime;
   using System.Collections.Generic;
   using System.IO;
   using System.Linq;
   using System.Text;

   /// <summary>
   /// A regular expression character set element. This element
   /// matches a single character inside(or outside) a character set.
   /// The character set is user defined and may contain ranges of
   /// characters.The set may also be inverted, meaning that only
   /// characters not inside the set will be considered to match.
   /// </summary>
   internal class CharacterSetElement : Element
   {
      /// <summary>
      /// The dot ('.') character set. This element matches a single
      /// character that is not equal to a newline character.
      /// </summary>
      public static readonly CharacterSetElement DOT =
          new CharacterSetElement(false);

      /// <summary>
      /// The digit character set. This element matches a single
      /// numeric character.
      /// </summary>
      public static readonly CharacterSetElement DIGIT =
          new CharacterSetElement(false);

      /// <summary>
      /// The non-digit character set. This element matches a single
      /// non-numeric character.
      /// </summary>
      public static readonly CharacterSetElement NONDIGIT =
          new CharacterSetElement(true);

      /// <summary>
      /// The whitespace character set. This element matches a single
      /// whitespace character.
      /// </summary>
      public static readonly CharacterSetElement WHITESPACE =
          new CharacterSetElement(false);

      /// <summary>
      /// The non-whitespace character set. This element matches a
      /// single non-whitespace character.
      /// </summary>
      public static readonly CharacterSetElement NONWHITESPACE =
          new CharacterSetElement(true);

      /// <summary>
      /// The word character set. This element matches a single word
      /// character.
      /// </summary>
      public static readonly CharacterSetElement WORD =
          new CharacterSetElement(false);

      /// <summary>
      /// The non-word character set. This element matches a single
      /// non-word character.
      /// </summary>
      public static readonly CharacterSetElement NONWORD =
          new CharacterSetElement(true);

      /// <summary>
      /// The inverted character set flag.
      /// </summary>
      private readonly bool inverted;

      /// <summary>
      /// The character set content, for individual characters.
      /// </summary>
      private readonly List<char> charContents = new List<char>();

      /// <summary>
      /// The character set content, for character ranges.
      /// </summary>
      private readonly List<Range> rangeContents = new List<Range>();

      /// <summary>
      /// The character set content, for special character sets
      /// </summary>
      private readonly List<CharacterSetElement> charSetContents = new List<CharacterSetElement>();

      /// <summary>
      /// Initializes a new instance of the <see cref="CharacterSetElement"/> class.
      /// If the inverted character set flag is set, only characters NOT 
      /// in the set will match.
      /// </summary>
      /// <param name="inverted">The inverted character flag</param>
      public CharacterSetElement(bool inverted)
      {
         this.inverted = inverted;
      }

      /// <summary>
      /// Adds a single character to this character set.
      /// </summary>
      /// <param name="c">The character to add</param>
      public void AddCharacter(char c)
      {
         this.charContents.Add(c);
      }

      /// <summary>
      /// Adds multiple characters to this character set.
      /// </summary>
      /// <param name="str">The string containing the characters to be added</param>
      public void AddCharacters(string str)
      {
         for (int i = 0; i < str.Length; i++)
         {
            this.AddCharacter(str[i]);
         }
      }

      /// <summary>
      /// Adds multiple characters to this character set.
      /// </summary>
      /// <param name="elem">The string element with characters to be added</param>
      public void AddCharacters(StringElement elem)
      {
         this.AddCharacters(elem.String);
      }

      /// <summary>
      /// Adds a character range to this character set.
      /// </summary>
      /// <param name="min">The minimum character value</param>
      /// <param name="max">The maximum character value</param>
      public void AddRange(char min, char max)
      {
         this.rangeContents.Add(new Range(min, max));
      }

      /// <summary>
      /// Adds a character subset to this character set.
      /// </summary>
      /// <param name="elem">The character set to add</param>
      public void AddCharacterSet(CharacterSetElement elem)
      {
         this.charSetContents.Add(elem);
      }

      /// <summary>
      /// Returns this element as the character set shouldn't be
      /// modified after creation.This partially breaks the contract
      /// of clone(), but as new characters are not added to the
      /// character set after creation, this will work correctly.
      /// </summary>
      /// <returns>A copy of this object</returns>
      public override object Clone()
      {
         return this;
      }

      /// <summary>
      /// Returns the length of a matching string starting at the
      /// specified position.The number of matches to skip can also be
      /// specified, but numbers higher than zero (0) cause a failed
      /// match for any element that doesn't attempt to combine other
      /// elements.
      /// </summary>
      /// <param name="m">the matcher being used</param>
      /// <param name="buffer">the input character buffer to match</param>
      /// <param name="start">The starting position</param>
      /// <param name="skip">the number of matches to skip</param>
      /// <returns>
      /// the length of the matching string, or -1 if no match was found
      /// </returns>
      public override int Match(
          Matcher m,
          ReaderBuffer buffer,
          int start,
          int skip)
      {
         int c;

         if (skip != 0)
         {
            return -1;
         }

         c = buffer.Peek(start);
         if (c < 0)
         {
            m.SetReadEndOfString();
            return -1;
         }

         if (m.IsCaseInsensitive)
         {
            c = (int)char.ToLower((char)c);
         }

         return this.InSet((char)c) ? 1 : -1;
      }

      /// <summary>
      /// Prints this element to the specified output stream.
      /// </summary>
      /// <param name="output">The output stream to be used</param>
      /// <param name="indent">The current indentation</param>
      public override void PrintTo(TextWriter output, string indent)
      {
         output.WriteLine(indent + this.ToString());
      }

      /// <summary>
      /// Returns a string description of this character set.
      /// </summary>
      /// <returns>A string description of this character set.</returns>
      public override string ToString()
      {
         StringBuilder buffer;

         // Handle predefined character sets
         if (this == CharacterSetElement.DOT)
         {
            return ".";
         }
         else if (this == CharacterSetElement.DIGIT)
         {
            return "\\d";
         }
         else if (this == CharacterSetElement.NONDIGIT)
         {
            return "\\D";
         }
         else if (this == CharacterSetElement.WHITESPACE)
         {
            return "\\s";
         }
         else if (this == CharacterSetElement.NONWHITESPACE)
         {
            return "\\S";
         }
         else if (this == CharacterSetElement.WORD)
         {
            return "\\w";
         }
         else if (this == CharacterSetElement.NONWORD)
         {
            return "\\W";
         }

         // Handle user-defined character sets
         buffer = new StringBuilder();
         if (this.inverted)
         {
            buffer.Append("^[");
         }
         else
         {
            buffer.Append("[");
         }

         foreach (var c in this.charContents)
         {
            buffer.Append(c);
         }

         foreach (var r in this.rangeContents)
         {
            buffer.Append(r);
         }

         foreach (var cs in this.charSetContents)
         {
            buffer.Append(cs);
         }

         buffer.Append("]");

         return buffer.ToString();
      }

      /// <summary>
      /// Checks if the specified character is present in the 'dot'
      /// set.This method does not consider the inverted flag.
      /// </summary>
      /// <param name="c">The character to be checked</param>
      /// <returns>True if the character is in the set, false if not</returns>
      private static bool InDotSet(char c)
      {
         switch (c)
         {
            case '\n':
            case '\r':
            case '\u0085':
            case '\u2028':
            case '\u2029':
               return false;
            default:
               return true;
         }
      }

      /// <summary>
      /// Checks if the specified character is a digit. This method
      /// does not consider the inverted flag.
      /// </summary>
      /// <param name="c">The character to be checked</param>
      /// <returns>True if the character is in the set, false if not</returns>
      private static bool InDigitSet(char c)
      {
         return c >= '0' && c <= '9';
      }

      /// <summary>
      /// Checks if the specified character is a whitespace
      /// character.This method does not consider the inverted flag.
      /// </summary>
      /// <param name="c">The character to be checked</param>
      /// <returns>True if the character is in the set, false if not</returns>
      private static bool InWhitespaceSet(char c)
      {
         switch (c)
         {
            case ' ':
            case '\t':
            case '\n':
            case '\f':
            case '\r':
            case (char)11:
               return true;
            default:
               return false;
         }
      }

      /// <summary>
      /// Checks if the specified character is a word character. This
      /// method does not consider the inverted flag.
      /// </summary>
      /// <param name="c">The character to be checked</param>
      /// <returns>True if the character is in the set, false if not</returns>
      private static bool InWordSet(char c)
      {
         return (c <= 'a' && c <= 'z')
             || (c <= 'A' && c <= 'Z')
             || (c <= '0' && c <= '9')
             || c == '_'; // TODO: check this is the complete set
      }

      /// <summary>
      /// Checks if the specified character matches this character
      /// set.This method takes the inverted flag into account.
      /// </summary>
      /// <param name="c">The character to check</param>
      /// <returns>True if the character is in the set, false if not</returns>
      private bool InSet(char c)
      {
         if (this == CharacterSetElement.DOT)
         {
            return CharacterSetElement.InDotSet(c);
         }
         else if (this == CharacterSetElement.DIGIT
             || this == CharacterSetElement.NONDIGIT)
         {
            return CharacterSetElement.InDigitSet(c) != this.inverted;
         }
         else if (this == CharacterSetElement.WHITESPACE
             || this == CharacterSetElement.NONWHITESPACE)
         {
            return CharacterSetElement.InWhitespaceSet(c) != this.inverted;
         }
         else if (this == CharacterSetElement.WORD
             || this == CharacterSetElement.NONWORD)
         {
            return CharacterSetElement.InWordSet(c) != this.inverted;
         }
         else
         {
            return this.InUserSet(c) != this.inverted;
         }
      }

      /// <summary>
      /// Checks if the specified character is present in the user-
      /// defined set.This method does not consider the inverted
      /// flag.
      /// </summary>
      /// <param name="value">The character to be checked</param>
      /// <returns>True if the character is in the set, false if not</returns>
      private bool InUserSet(char value)
      {
         if (this.charContents.Where(c => c == value).Any())
         {
            return true;
         }

         if (this.rangeContents.Where(r => r.Inside(value)).Any())
         {
            return true;
         }

         if (this.charSetContents.Where(cs => cs.InSet(value)).Any())
         {
            return true;
         }

         return false;
      }

      /// <summary>
      /// A character range class.
      /// </summary>
      private class Range
      {
         /// <summary>
         /// The minimum character value.
         /// </summary>
         private readonly char min;

         /// <summary>
         /// The maximum character value.
         /// </summary>
         private readonly char max;

         /// <summary>
         /// Initializes a new instance of the <see cref="Range"/> class.
         /// </summary>
         /// <param name="min">The minimum character</param>
         /// <param name="max">The maximum character</param>
         public Range(char min, char max)
         {
            this.min = min;
            this.max = max;
         }

         /// <summary>
         /// Checks if the specified character is inside the range.
         /// </summary>
         /// <param name="c">The character to be checked</param>
         /// <returns>True if the character is in the set, false if not</returns>
         public bool Inside(char c)
         {
            return this.min <= c && c <= this.max;
         }

         /// <summary>
         /// Returns a string representation of this range.
         /// </summary>
         /// <returns>A string representation of this range.</returns>
         public override string ToString()
         {
            return this.min + "-" + this.max;
         }
      }
   }
}