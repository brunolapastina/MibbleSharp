// <copyright file="Token.cs" company="None">
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
   using System.Text;

   /// <summary>
   /// A token node. This class represents a token (i.e. a set of adjacent
   /// characters) in a parse tree. The tokens are created by a tokenizer,
   /// that groups characters together into tokens according to a set of
   /// token patterns.
   /// </summary>
   public class Token : Node
   {
      /// <summary>
      /// The token pattern used for this token.
      /// </summary>
      private readonly TokenPattern pattern;

      /// <summary>
      /// The characters that constitute this token. This is normally
      /// referred to as the token image.
      /// </summary>
      private readonly string image;

      /// <summary>
      /// The line number of the first character in the token image.
      /// </summary>
      private readonly int startLine;

      /// <summary>
      /// The column number of the first character in the token image.
      /// </summary>
      private readonly int startColumn;

      /// <summary>
      /// The line number of the last character in the token image.
      /// </summary>
      private readonly int endLine;

      /// <summary>
      /// The column number of the last character in the token image.
      /// </summary>
      private readonly int endColumn;

      /// <summary>
      /// The previous token in the list of tokens.
      /// </summary>
      private Token previous = null;

      /// <summary>
      /// The next token in the list of tokens.
      /// </summary>
      private Token next = null;

      /// <summary>
      /// Initializes a new instance of the <see cref="Token"/> class.
      /// </summary>
      /// <param name="pattern">The token pattern</param>
      /// <param name="image">The token image, i.e. the characters</param>
      /// <param name="line">The line number of the first character</param>
      /// <param name="col">The column number of the first character</param>
      public Token(TokenPattern pattern, string image, int line, int col)
      {
         this.pattern = pattern;
         this.image = image;
         this.startLine = line;
         this.startColumn = col;
         this.endLine = line;
         this.endColumn = col + image.Length - 1;

         for (int pos = 0; image.IndexOf('\n', pos) >= 0;)
         {
            pos = image.IndexOf('\n', pos) + 1;
            this.endLine++;
            this.endColumn = image.Length - pos;
         }
      }

      /// <summary>
      /// The node type id property (read-only). This value is set as
      /// a unique identifier for each type of node, in order to
      /// simplify later identification.
      /// </summary>
      public override int Id
      {
         get
         {
            return this.pattern.Id;
         }
      }

      /// <summary>
      /// The node name property (read-only).
      /// </summary>
      public override string Name
      {
         get
         {
            return this.pattern.Name;
         }
      }

      /// <summary>
      /// The line number property of the first character in this
      /// node (read-only). If the node has child elements, this
      /// value will be fetched from the first child.
      /// </summary>
      public override int StartLine
      {
         get
         {
            return this.startLine;
         }
      }

      /// <summary>
      /// The column number property of the first character in this
      /// node (read-only). If the node has child elements, this
      /// value will be fetched from the first child.
      /// </summary>
      public override int StartColumn
      {
         get
         {
            return this.startColumn;
         }
      }

      /// <summary>
      /// The line number property of the last character in this node
      /// (read-only). If the node has child elements, this value
      /// will be fetched from the last child.
      /// </summary>
      public override int EndLine
      {
         get
         {
            return this.endLine;
         }
      }

      /// <summary>
      /// The column number property of the last character in this
      /// node (read-only). If the node has child elements, this
      /// value will be fetched from the last child.
      /// </summary>
      public override int EndColumn
      {
         get
         {
            return this.endColumn;
         }
      }

      /// <summary>
      /// Gets the token image (read-only). The token image
      /// consists of the input characters matched to form this
      /// token.
      /// </summary>
      public string Image
      {
         get
         {
            return this.image;
         }
      }

      /// <summary>
      /// Gets or sets the previous token. If the token list feature is
      /// used in the tokenizer, all tokens found will be chained
      /// together in a double-linked list. The previous token may be
      /// a token that was ignored during the parsing, due to it's
      /// ignore flag being set. If there is no previous token or if
      /// the token list feature wasn't used in the tokenizer (the
      /// default), the previous token will always be null.
      /// </summary>
      /// <see cref="Next"/>
      /// <see cref="Tokenizer.UseTokenList"/>
      public Token Previous
      {
         get
         {
            return this.previous;
         }

         set
         {
            if (this.previous != null)
            {
               this.previous.next = null;
            }

            this.previous = value;

            if (this.previous != null)
            {
               this.previous.next = this;
            }
         }
      }

      /// <summary>
      /// Gets or sets the next token. If the token list feature is used
      /// in the tokenizer, all tokens found will be chained together
      /// in a double-linked list. The next token may be a token that
      /// was ignored during the parsing, due to it's ignore flag
      /// being set. If there is no next token or if the token list
      /// feature wasn't used in the tokenizer (the default), the
      /// next token will always be null.
      /// </summary>
      /// <see cref="Previous"/>
      /// <see cref="Tokenizer.UseTokenList"/>
      public Token Next
      {
         get
         {
            return this.next;
         }

         set
         {
            if (this.next != null)
            {
               this.next.previous = null;
            }

            this.next = value;

            if (this.next != null)
            {
               this.next.previous = this;
            }
         }
      }

      /// <summary>
      /// Gets the token pattern property (read-only).
      /// </summary>
      internal TokenPattern Pattern
      {
         get
         {
            return this.pattern;
         }
      }

      /// <summary>
      /// Returns a string representation of this token.
      /// </summary>
      /// <returns>
      /// A string representation of this token
      /// </returns>
      public override string ToString()
      {
         StringBuilder buffer = new StringBuilder();
         int newline = this.image.IndexOf('\n');

         buffer.Append(this.pattern.Name);
         buffer.Append("(");
         buffer.Append(this.pattern.Id);
         buffer.Append("): \"");

         if (newline >= 0)
         {
            if (newline > 0 && this.image[newline - 1] == '\r')
            {
               newline--;
            }

            buffer.Append(this.image.Substring(0, newline));
            buffer.Append("(...)");
         }
         else
         {
            buffer.Append(this.image);
         }

         buffer.Append("\", line: ");
         buffer.Append(this.startLine);
         buffer.Append(", col: ");
         buffer.Append(this.startColumn);

         return buffer.ToString();
      }

      /// <summary>
      /// Returns a short string representation of this token. The
      /// string will only contain the token image and possibly the
      /// token pattern name.
      /// </summary>
      /// <returns>
      /// A short string representation of this token
      /// </returns>
      public string ToShortString()
      {
         StringBuilder buffer = new StringBuilder();
         int newline = this.image.IndexOf('\n');

         buffer.Append('"');
         if (newline >= 0)
         {
            if (newline > 0 && this.image[newline - 1] == '\r')
            {
               newline--;
            }

            buffer.Append(this.image.Substring(0, newline));
            buffer.Append("(...)");
         }
         else
         {
            buffer.Append(this.image);
         }

         buffer.Append('"');

         if (this.pattern.Type == TokenPattern.PatternType.RegExp)
         {
            buffer.Append(" <");
            buffer.Append(this.pattern.Name);
            buffer.Append(">");
         }

         return buffer.ToString();
      }
   }
}