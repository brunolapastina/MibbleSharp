// <copyright file="LookAheadSet.cs" company="None">
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
   using System.Collections.Generic;
   using System.Text;

   /// <summary>
   /// A token look-ahead set. This class contains a set of token id
   /// sequences. All sequences in the set are limited in length, so
   /// that no single sequence is longer than a maximum value. This
   /// class also filters out duplicates. Each token sequence also
   /// contains a repeat flag, allowing the look-ahead set to contain
   /// information about possible infinite repetitions of certain
   /// sequences. That information is important when conflicts arise
   /// between two look-ahead sets, as such a conflict cannot be
   /// resolved if the conflicting sequences can be repeated (would
   /// cause infinite loop).
   /// </summary>
   internal class LookAheadSet
   {
      /// <summary>
      /// The set of token look-ahead sequences. Each sequence in
      /// turn is represented by an ArrayList with Integers for the
      /// token id:s.
      /// </summary>    
      private readonly IList<Sequence> elements = new List<Sequence>();

      /// <summary>
      /// The maximum length of any look-ahead sequence.
      /// </summary>
      private readonly int maxLength;

      /// <summary>
      /// Initializes a new instance of the <see cref="LookAheadSet"/> class, 
      /// with the specified maximum length.
      /// </summary>
      /// <param name="maxLength">The maximum token sequence length</param>         
      public LookAheadSet(int maxLength)
      {
         this.maxLength = maxLength;
      }

      /// <summary>
      /// Initializes a new instance of the <see cref="LookAheadSet"/> class to
      /// creates a duplicate look-ahead set, possibly with a
      /// different maximum length.
      /// </summary>
      /// <param name="maxLength">The maximum token sequence length</param>
      /// <param name="set">The look-ahead set to copy</param>         
      public LookAheadSet(int maxLength, LookAheadSet set)
          : this(maxLength)
      {
         this.AddAll(set);
      }

      /// <summary>
      /// Gets the size of this look-ahead set.
      /// </summary>
      public int Size
      {
         get
         {
            return this.elements.Count;
         }
      }

      /// <summary>
      /// Gets the length of the shortest token sequence in this
      /// set. This method will return zero (0) if the set is empty.
      /// </summary>
      /// @return the length of the shortest token sequence
      public int MinLength
      {
         get
         {
            int min = -1;
            foreach (var seq in this.elements)
            {
               if (min < 0 || seq.Length < min)
               {
                  min = seq.Length;
               }
            }

            return (min < 0) ? 0 : min;
         }
      }

      /// <summary>
      /// Gets the length of the longest token sequence in this
      /// set. This method will return zero (0) if the set is empty.
      /// </summary>
      public int MaxLength
      {
         get
         {
            int max = 0;

            foreach (var seq in this.elements)
            {
               if (seq.Length > max)
               {
                  max = seq.Length;
               }
            }

            return max;
         }
      }

      /// <summary>
      /// Gets a list of the initial token id:s in this look-ahead
      /// set. The list returned will not contain any duplicates.
      /// </summary>
      public int[] InitialTokens
      {
         get
         {
            List<int> list = new List<int>();
            int[] result;
            int token;

            foreach (var elt in this.elements)
            {
               token = (int)elt.GetToken(0);
               if (!list.Contains(token))
               {
                  list.Add(token);
               }
            }

            result = list.ToArray();
            return result;
         }
      }

      /// <summary>
      /// Gets a value indicating whether this look-ahead set 
      /// contains a repetitive token sequence.
      /// </summary>
      public bool IsRepetitive
      {
         get
         {
            foreach (var seq in this.elements)
            {
               if (seq.IsRepetitive)
               {
                  return true;
               }
            }

            return false;
         }
      }

      /// <summary>
      /// Checks if the next token(s) in the parser match any token
      /// sequence in this set.
      /// </summary>
      /// <param name="parser">The parser to check</param>
      /// <returns>
      /// True if the next tokens are in the set, or false otherwise
      /// </returns>
      public bool IsNext(Parser parser)
      {
         foreach (var seq in this.elements)
         {
            if (seq.IsNext(parser))
            {
               return true;
            }
         }

         return false;
      }

      /// <summary>
      /// Checks if the next token(s) in the parser match any token
      /// sequence in this set.
      /// </summary>
      /// <param name="parser">The parser to check</param>
      /// <param name="length">The maximum number of tokens to check</param>
      /// <returns>
      /// True if the next tokens are in the set, or false otherwise
      /// </returns>
      public bool IsNext(Parser parser, int length)
      {
         foreach (var seq in this.elements)
         {
            if (seq.IsNext(parser, length))
            {
               return true;
            }
         }

         return false;
      }

      /// <summary>
      /// Checks if another look-ahead set has an overlapping token
      /// sequence. An overlapping token sequence is a token sequence
      /// that is identical to another sequence, but for the length.
      /// I.e. one of the two sequences may be longer than the other.
      /// </summary>
      /// <param name="set">The look-ahead set to check</param>
      /// <returns>
      /// True if there is some token sequence that overlaps, or false otherwise
      /// </returns>
      public bool IsOverlap(LookAheadSet set)
      {
         foreach (var seq in this.elements)
         {
            if (set.IsOverlap(seq))
            {
               return true;
            }
         }

         return false;
      }

      /// <summary>
      /// Checks if some token sequence is present in both this set
      /// and a specified one.
      /// </summary>
      /// <param name="set">The look-ahead set to compare with</param>
      /// <returns>
      /// True if the look-ahead sets intersect, or false otherwise
      /// </returns>
      public bool Intersects(LookAheadSet set)
      {
         foreach (var elem in this.elements)
         {
            if (set.Contains(elem))
            {
               return true;
            }
         }

         return false;
      }

      /// <summary>
      /// Adds a new token sequence with a single token to this set.
      /// The sequence will only be added if it is not already in the
      /// set.
      /// </summary>
      /// <param name="token">The token to add</param>
      public void Add(int token)
      {
         this.Add(new Sequence(false, token));
      }

      /// <summary>
      /// Adds all the token sequences from a specified set. Only
      /// sequences not already in this set will be added.
      /// </summary>
      /// <param name="set">The set to add from</param>
      public void AddAll(LookAheadSet set)
      {
         foreach (var elt in set.elements)
         {
            this.Add(elt);
         }
      }

      /// <summary>
      /// Adds an empty token sequence to this set. The sequence will
      /// only be added if it is not already in the set.
      /// </summary>
      public void AddEmpty()
      {
         this.Add(new Sequence());
      }

      /// <summary>
      /// Removes all the token sequences from a specified set. Only
      /// sequences already in this set will be removed.
      /// </summary>
      /// <param name="set">The set to remove from</param>
      public void RemoveAll(LookAheadSet set)
      {
         foreach (var elt in set.elements)
         {
            this.Remove(elt);
         }
      }

      /// <summary>
      /// Creates a new look-ahead set that is the result of reading
      /// the specified token. The new look-ahead set will contain
      /// the rest of all the token sequences that started with the
      /// specified token.
      /// </summary>
      /// <param name="token">The token to read</param>
      /// <returns>
      /// A new look-ahead set containing the remaining tokens
      /// </returns>
      public LookAheadSet CreateNextSet(int token)
      {
         LookAheadSet result = new LookAheadSet(this.maxLength - 1);
         object value;

         foreach (var seq in this.elements)
         {
            value = seq.GetToken(0);
            if (value != null && token == (int)value)
            {
               result.Add(seq.Subsequence(1));
            }
         }

         return result;
      }

      /// <summary>
      /// Creates a new look-ahead set that is the intersection of
      /// this set with another set. The token sequences in the net
      /// set will only have the repeat flag set if it was set in
      /// both the identical token sequences.
      /// </summary>
      /// <param name="set">The set to intersect with</param>
      /// <returns>A new look-ahead set containing the intersection</returns>
      public LookAheadSet CreateIntersection(LookAheadSet set)
      {
         LookAheadSet result = new LookAheadSet(this.maxLength);
         Sequence seq2;

         foreach (var seq in this.elements)
         {
            seq2 = set.FindSequence(seq);
            if (seq2 != null && seq.IsRepetitive)
            {
               result.Add(seq2);
            }
            else if (seq2 != null)
            {
               result.Add(seq);
            }
         }

         return result;
      }

      /// <summary>
      /// Creates a new look-ahead set that is the combination of
      /// this set with another set. The combination is created by
      /// creating new token sequences that consist of appending all
      /// elements from the specified set onto all elements in this
      /// set. This is sometimes referred to as the cartesian
      /// product.
      /// </summary>
      /// <param name="set">The set to combine with</param>
      /// <returns>
      /// A new look-ahead set containing the combination
      /// </returns>
      public LookAheadSet CreateCombination(LookAheadSet set)
      {
         LookAheadSet result = new LookAheadSet(this.maxLength);

         // Handle special cases
         if (this.Size <= 0)
         {
            return set;
         }
         else if (set.Size <= 0)
         {
            return this;
         }

         // Create combinations
         foreach (var first in this.elements)
         {
            if (first.Length >= this.maxLength)
            {
               result.Add(first);
            }
            else if (first.Length <= 0)
            {
               result.AddAll(set);
            }
            else
            {
               foreach (var second in set.elements)
               {
                  result.Add(first.Concat(this.maxLength, second));
               }
            }
         }

         return result;
      }

      /// <summary>
      /// Creates a new look-ahead set with overlaps from another. All
      /// token sequences in this set that overlaps with the other set
      /// will be added to the new look-ahead set.
      /// </summary>
      /// <param name="set">The look-ahead set to check with</param>
      /// <returns>A new look-ahead set containing the overlaps</returns>
      public LookAheadSet CreateOverlaps(LookAheadSet set)
      {
         LookAheadSet result = new LookAheadSet(this.maxLength);

         foreach (var seq in this.elements)
         {
            if (set.IsOverlap(seq))
            {
               result.Add(seq);
            }
         }

         return result;
      }

      /// <summary>
      /// Creates a new look-ahead set filter. The filter will contain
      /// all sequences from this set, possibly left trimmed by each one
      /// of the sequences in the specified set.
      /// </summary>
      /// <param name="set">The look-ahead set to trim with</param>
      /// <returns>A new look-ahead set filter</returns>
      public LookAheadSet CreateFilter(LookAheadSet set)
      {
         LookAheadSet result = new LookAheadSet(this.maxLength);

         // Handle special cases
         if (this.Size <= 0 || set.Size <= 0)
         {
            return this;
         }

         // Create combinations
         foreach (var first in this.elements)
         {
            foreach (var second in set.elements)
            {
               if (first.StartsWith(second))
               {
                  result.Add(first.Subsequence(second.Length));
               }
            }
         }

         return result;
      }

      /// <summary>
      /// Creates a new identical look-ahead set, except for the
      /// repeat flag being set in each token sequence.
      /// </summary>
      /// <returns>A new repetitive look-ahead set</returns>
      public LookAheadSet CreateRepetitive()
      {
         LookAheadSet result = new LookAheadSet(this.maxLength);

         foreach (var seq in this.elements)
         {
            if (seq.IsRepetitive)
            {
               result.Add(seq);
            }
            else
            {
               result.Add(new Sequence(true, seq));
            }
         }

         return result;
      }

      /// <summary>
      /// Returns a string representation of this object.
      /// </summary>
      /// <returns>A string representation of this object</returns>
      public override string ToString()
      {
         return this.ToString(null);
      }

      /// <summary>
      /// Returns a string representation of this object.
      /// </summary>
      /// <param name="tokenizer">The tokenizer containing the tokens</param>
      /// <returns>A string representation of this object</returns>
      public string ToString(Tokenizer tokenizer)
      {
         StringBuilder buffer = new StringBuilder();

         buffer.Append("{");
         foreach (var seq in this.elements)
         {
            buffer.Append("\n  ");
            buffer.Append(seq.ToString(tokenizer));
         }

         buffer.Append("\n}");
         return buffer.ToString();
      }

      /// <summary>
      /// Checks if a token sequence is overlapping. An overlapping token
      /// sequence is a token sequence that is identical to another
      /// sequence, but for the length. I.e. one of the two sequences may
      /// be longer than the other.
      /// </summary>
      /// <param name="seq">The token sequence to check</param>
      /// <returns>
      /// True if there is some token sequence that overlaps, or false otherwise
      /// </returns>
      private bool IsOverlap(Sequence seq)
      {
         foreach (var elem in this.elements)
         {
            if (seq.StartsWith(elem) || elem.StartsWith(seq))
            {
               return true;
            }
         }

         return false;
      }

      /// <summary>
      /// Checks if the specified token sequence is present in the
      /// set.
      /// </summary>
      /// <param name="elem">The token sequence to check</param>
      /// <returns>
      /// True if the sequence is present in this set, false if not
      /// </returns>
      private bool Contains(Sequence elem)
      {
         return this.FindSequence(elem) != null;
      }

      /// <summary>
      /// Finds an identical token sequence if present in the set.
      /// </summary>
      /// <param name="elem">The token sequence to search for</param>
      /// <returns>
      /// An identical the token sequence if found, or null if not found
      /// </returns>
      private Sequence FindSequence(Sequence elem)
      {
         foreach (var elt in this.elements)
         {
            if (elt.Equals(elem))
            {
               return elt;
            }
         }

         return null;
      }

      /// <summary>
      /// Adds a token sequence to this set. The sequence will only
      /// be added if it is not already in the set. Also, if the
      /// sequence is longer than the allowed maximum, a truncated
      /// sequence will be added instead.
      /// </summary>
      /// <param name="seq">The token sequence to add</param>
      private void Add(Sequence seq)
      {
         if (seq.Length > this.maxLength)
         {
            seq = new Sequence(this.maxLength, seq);
         }

         if (!this.Contains(seq))
         {
            this.elements.Add(seq);
         }
      }

      /// <summary>
      /// Removes a token sequence from this set.
      /// </summary>
      /// <param name="seq">The token sequence to be removed</param>        
      private void Remove(Sequence seq)
      {
         this.elements.Remove(seq);
      }

      /// <summary>
      /// A token sequence. This class contains a list of token ids.
      /// It is immutable after creation, meaning that no changes
      /// will be made to an instance after creation.
      /// </summary>
      private class Sequence
      {
         /// <summary>
         /// The repeat flag. If this flag is set, the token
         /// sequence or some part of it may be repeated infinitely.
         /// </summary>
         private bool repeat = false;

         /// <summary>
         /// The list of token ids in this sequence.
         /// </summary> 
         private readonly List<int> tokens = null;

         /// <summary>
         /// Initializes a new instance of the <see cref="Sequence"/> class, 
         /// the repeat flag will be set to false.
         /// </summary> 
         public Sequence()
         {
            this.repeat = false;
            this.tokens = new List<int>();
         }

         /// <summary>
         /// Initializes a new instance of the <see cref="Sequence"/> class,
         /// with a single token.
         /// </summary>
         /// <param name="repeat">The repeat flag value</param>
         /// <param name="token">The token to add</param>
#pragma warning disable IDE0060 // Remove unused parameter
         public Sequence(bool repeat, int token)
#pragma warning restore IDE0060 // Remove unused parameter
         {
            this.repeat = false;
            this.tokens = new List<int>
            {
               token
            };
         }

         /// <summary>
         /// Initializes a new instance of the <see cref="Sequence"/> class,
         /// that is a duplicate of another sequence. Only a limited number 
         /// of tokens will be copied however. The repeat flag from the original
         /// will be kept intact.
         /// </summary>
         /// <param name="length">The maximum number of tokens to copy</param>
         /// <param name="seq">The sequence to copy</param>             
         public Sequence(int length, Sequence seq)
         {
            this.repeat = seq.repeat;
            this.tokens = new List<int>(length);
            this.tokens.AddRange(seq.tokens);
         }

         /// <summary>
         /// Initializes a new instance of the <see cref="Sequence"/> class,
         /// that is a duplicate of another sequence. The new value of the repeat flag will
         /// be used however.
         /// </summary>
         /// <param name="repeat">The new repeat flag value</param>
         /// <param name="seq">The sequence to copy</param>             
         public Sequence(bool repeat, Sequence seq)
         {
            this.repeat = repeat;
            this.tokens = seq.tokens;
         }

         /// <summary>
         /// Gets a value indicating whether this token sequence 
         /// is repetitive. A repetitive token sequence is one with 
         /// the repeat flag set.
         /// </summary>
         public bool IsRepetitive
         {
            get
            {
               return this.repeat;
            }
         }

         /// <summary>
         /// Gets the length of the token sequence.
         /// </summary>
         public int Length
         {
            get
            {
               return this.tokens.Count;
            }
         }

         /// <summary>
         /// Returns a token at a specified position in the sequence.
         /// </summary>
         /// <param name="pos">The sequence position</param>
         /// <returns>The token id if found, null if not</returns>
         public object GetToken(int pos)
         {
            if (pos >= 0 && pos < this.tokens.Count)
            {
               return this.tokens[pos];
            }
            else
            {
               return null;
            }
         }

         /// <summary>
         /// Checks if this sequence is equal to another object.
         /// Only token sequences with the same tokens in the same
         /// order will be considered equal. The repeat flag will be
         /// disregarded.
         /// </summary>
         /// <param name="obj">The object to compare with</param>
         /// <returns>
         /// True if the objects are equal, or false otherwise
         /// </returns>
         public override bool Equals(object obj)
         {
            if (obj is Sequence sequence)
            {
               return this.Equals(sequence);
            }
            else
            {
               return false;
            }
         }

         /// <summary>
         /// Checks if this sequence is equal to another sequence.
         /// Only sequences with the same tokens in the same order
         /// will be considered equal. The repeat flag will be
         /// disregarded.
         /// </summary>
         /// <param name="seq">The sequence to compare with</param>
         /// <returns>
         /// True if the sequences are equal, or false otherwise
         /// </returns>
         public bool Equals(Sequence seq)
         {
            if (this.tokens.Count != seq.tokens.Count)
            {
               return false;
            }

            for (int i = 0; i < this.tokens.Count; i++)
            {
               if (!this.tokens[i].Equals(seq.tokens[i]))
               {
                  return false;
               }
            }

            return true;
         }

         /// <summary>
         /// Returns a hash code for this object.
         /// </summary>
         /// <returns>A hash code for this object</returns>             
         public override int GetHashCode()
         {
            return this.tokens.Count.GetHashCode();
         }

         /// <summary>
         /// Checks if this token sequence starts with the tokens from
         /// another sequence. If the other sequence is longer than this
         /// sequence, this method will always return false.
         /// </summary>
         /// <param name="seq">The token sequence to check</param>
         /// <returns>
         /// True if this sequence starts with the other, or false otherwise
         /// </returns>
         public bool StartsWith(Sequence seq)
         {
            if (this.Length < seq.Length)
            {
               return false;
            }

            for (int i = 0; i < seq.tokens.Count; i++)
            {
               if (!this.tokens[i].Equals(seq.tokens[i]))
               {
                  return false;
               }
            }

            return true;
         }

         /// <summary>
         /// Checks if the next token(s) in the parser matches this
         /// token sequence.
         /// </summary>
         /// <param name="parser">The parser to check</param>
         /// <returns>
         /// True if the next tokens are in the sequence, or false otherwise
         /// </returns>
         public bool IsNext(Parser parser)
         {
            Token token;
            int id;

            for (int i = 0; i < this.tokens.Count; i++)
            {
               id = this.tokens[i];
               token = parser.PeekToken(i);
               if (token == null || token.Id != id)
               {
                  return false;
               }
            }

            return true;
         }

         /// <summary>
         /// Checks if the next token(s) in the parser matches this
         /// </summary>
         /// <param name="parser">The parser to check</param>
         /// <param name="length">The maximum number of tokens to check</param>
         /// <returns>
         /// True if the next tokens are in the sequence, or false otherwise
         /// </returns>
         public bool IsNext(Parser parser, int length)
         {
            Token token;
            int id;

            if (length > this.tokens.Count)
            {
               length = this.tokens.Count;
            }

            for (int i = 0; i < length; i++)
            {
               id = this.tokens[i];
               token = parser.PeekToken(i);
               if (token == null || token.Id != id)
               {
                  return false;
               }
            }

            return true;
         }

         /// <summary>
         /// Returns a string representation of this object.
         /// </summary>
         /// <returns>A string representation of this object</returns>
         public override string ToString()
         {
            return this.ToString(null);
         }

         /// <summary>
         /// Returns a string representation of this object.
         /// </summary>
         /// <param name="tokenizer">The tokenizer containing the tokens</param>
         /// <returns>A string representation of this object</returns>             
         public string ToString(Tokenizer tokenizer)
         {
            StringBuilder buffer = new StringBuilder();
            string str;
            int id;

            if (tokenizer == null)
            {
               buffer.Append(this.tokens.ToString());
            }
            else
            {
               buffer.Append("[");
               for (int i = 0; i < this.tokens.Count; i++)
               {
                  id = this.tokens[i];
                  str = tokenizer.GetPatternDescription(id);
                  if (i > 0)
                  {
                     buffer.Append(" ");
                  }

                  buffer.Append(str);
               }

               buffer.Append("]");
            }

            if (this.repeat)
            {
               buffer.Append(" *");
            }

            return buffer.ToString();
         }

         /// <summary>
         /// Creates a new token sequence that is the concatenation
         /// of this sequence and another. A maximum length for the
         /// new sequence is also specified.
         /// </summary>
         /// <param name="length">The maximum length of the result</param>
         /// <param name="seq">The other sequence</param>
         /// <returns>The concatenated token sequence</returns>             
         public Sequence Concat(int length, Sequence seq)
         {
            Sequence res = new Sequence(length, this);

            if (seq.repeat)
            {
               res.repeat = true;
            }

            length -= this.Length;

            if (length > seq.Length)
            {
               res.tokens.AddRange(seq.tokens);
            }
            else
            {
               for (int i = 0; i < length; i++)
               {
                  res.tokens.Add(seq.tokens[i]);
               }
            }

            return res;
         }

         /// <summary>
         /// Creates a new token sequence that is a subsequence of
         /// this one.
         /// </summary>
         /// <param name="start">The subsequence start position</param>
         /// <returns>The new token subsequence</returns>             
         public Sequence Subsequence(int start)
         {
            Sequence res = new Sequence(this.Length, this);

            while (start > 0 && res.tokens.Count > 0)
            {
               res.tokens.RemoveAt(0);
               start--;
            }

            return res;
         }
      }
   }
}