// <copyright file="RegExp.cs" company="None">
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
    using System;
    using System.Collections;
    using System.Globalization;
    using System.IO;
    using System.Text;

    /// <summary>
    /// A regular expression. This class creates and holds an internal
    /// data structure representing a regular expression. It also
    /// allows creating matchers. This class is thread-safe. Multiple
    /// matchers may operate simultaneously on the same regular
    /// expression.
    /// </summary>
    public class RegExp
    {
        /// <summary>The base regular expression element.</summary>
        private readonly Element element;

        /// <summary>The regular expression pattern.</summary>
        private readonly string pattern;

        /// <summary>The character case ignore flag.</summary>
        private readonly bool ignoreCase;

        /// <summary>
        /// The current position in the pattern. This variable is used by
        /// the parsing methods.
        /// </summary>
        private int pos;

        /// <summary>
        /// Initializes a new instance of the <see cref="RegExp"/> class, 
        /// i.e. a new case-sensitive regular expression.
        /// </summary>
        /// <param name="pattern">The regular expression pattern</param>
        /// <exception cref="RegExpException">
        /// If the regular expression couldn't be parsed correctly
        /// </exception>
        public RegExp(string pattern)
            : this(pattern, false)
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="RegExp"/> class, 
        /// i.e. a new case-sensitive regular expression.
        /// </summary>
        /// <param name="pattern">The regular expression pattern</param>
        /// <param name="ignoreCase">Whether to ignore case or not</param>
        /// <exception cref="RegExpException">
        /// If the regular expression couldn't be parsed correctly
        /// </exception> 
        public RegExp(string pattern, bool ignoreCase)
        {
            this.pattern = pattern;
            this.ignoreCase = ignoreCase;
            this.pos = 0;
            this.element = this.ParseExpr();
            if (this.pos < pattern.Length)
            {
                throw new RegExpException(
                    RegExpException.ErrorType.UnexpectedCharacter,
                    this.pos,
                    pattern);
            }
        }

        /// <summary>
        /// Creates a new matcher for the specified string.
        /// </summary>
        /// <param name="str">The string to work with</param>
        /// <returns>The regular expression matcher</returns>
        public Matcher Matcher(string str)
        {
            return this.Matcher(new ReaderBuffer(new StringReader(str)));
        }

        /// <summary>
        /// Creates a new matcher for the specified look-ahead
        /// character input stream.
        /// </summary>
        /// <param name="buffer">The character input buffer</param>
        /// <returns>The regular expression matcher</returns>
        public Matcher Matcher(ReaderBuffer buffer)
        {
            return new Matcher((Element)this.element.Clone(), buffer, this.ignoreCase);
        }
        
        /// <summary>
        /// Returns a string representation of the regular expression.
        /// </summary>
        /// <returns>A string representation of the regular expression</returns>
        public override string ToString()
        {
            StringWriter str;

            str = new StringWriter();
            str.WriteLine("Regular Expression");
            str.WriteLine("  Pattern: " + this.pattern);
            str.Write("  Flags:");
            if (this.ignoreCase)
            {
                str.Write(" caseignore");
            }

            str.WriteLine();
            str.WriteLine("  Compiled:");
            this.element.PrintTo(str, "    ");
            return str.ToString();
        }

        /// <summary>
        /// Parses a regular expression. This method handles the <c>Expr</c>
        /// production in the grammar (see <c>regexp.grammar</c>).
        /// </summary>
        /// <returns>The element representing this expression</returns>
        /// <exception cref="RegExpException">
        /// If an error was encountered in the pattern string
        /// </exception>
        private Element ParseExpr()
        {
            Element first;
            Element second;

            first = this.ParseTerm();
            if (this.PeekChar(0) != '|')
            {
                return first;
            }
            else
            {
                this.ReadChar('|');
                second = this.ParseExpr();
                return new AlternativeElement(first, second);
            }
        }

        /// <summary>
        /// Parses a regular expression term. This method handles the
        /// Term production in the grammar (see <c>regexp.grammar</c>).
        /// </summary>
        /// <returns>The element representing this term</returns>
        /// <exception cref="RegExpException">
        /// if an error was encountered in the pattern string
        /// </exception>
        private Element ParseTerm()
        {
            ArrayList list = new ArrayList()
            {
               this.ParseFact()
            };

            while (true)
            {
                switch (this.PeekChar(0))
                {
                    case -1:
                    case ')':
                    case ']':
                    case '{':
                    case '}':
                    case '?':
                    case '+':
                    case '|':
                        return this.CombineElements(list);
                    default:
                        list.Add(this.ParseFact());
                        break;
                }
            }
        }

        /// <summary>
        /// Parses a regular expression factor. This method handles the
        /// Fact production in the grammar (see <c>regexp.grammar</c>).
        /// </summary>
        /// <returns>The element representing this factor</returns>
        /// <exception cref="RegExpException">
        /// if an error was encountered in the
        /// pattern string
        /// </exception>
        private Element ParseFact()
        {
            Element elem;

            elem = this.ParseAtom();
            switch (this.PeekChar(0))
            {
                case '?':
                case '*':
                case '+':
                case '{':
                    return this.ParseAtomModifier(elem);
                default:
                    return elem;
            }
        }

        /// <summary>
        /// Parses a regular expression atom. This method handles the
        /// Atom production in the grammar (see <c>regexp.grammar</c>).
        /// </summary>
        /// <returns>The element representing this atom</returns>
        /// <exception cref="RegExpException">
        /// If an error was encountered in the
        /// pattern string
        /// </exception>
        private Element ParseAtom()
        {
            Element elem;

            switch (this.PeekChar(0))
            {
                case '.':
                    this.ReadChar('.');
                    return CharacterSetElement.DOT;
                case '(':
                    this.ReadChar('(');
                    elem = this.ParseExpr();
                    this.ReadChar(')');
                    return elem;
                case '[':
                    this.ReadChar('[');
                    elem = this.ParseCharSet();
                    this.ReadChar(']');
                    return elem;
                case -1:
                case ')':
                case ']':
                case '{':
                case '}':
                case '?':
                case '*':
                case '+':
                case '|':
                    throw new RegExpException(
                        RegExpException.ErrorType.UnexpectedCharacter,
                        this.pos,
                        this.pattern);
                default:
                    return this.ParseChar();
            }
        }

        /// <summary>
        /// Parses a regular expression atom modifier. This method handles
        /// the AtomModifier production in the grammar (see <c>regexp.grammar</c>).
        /// </summary>
        /// <param name="elem">The element to modify</param>
        /// <returns>The modified element</returns>
        /// <exception cref="RegExpException">
        /// If an error was encountered in the
        /// pattern string
        /// </exception>
        private Element ParseAtomModifier(Element elem)
        {
            int min;
            int max;
            RepeatElement.RepeatType type;
            int firstPos;

            // Read min and max
            type = RepeatElement.RepeatType.GREEDY;
            switch (this.ReadChar())
            {
                case '?':
                    min = 0;
                    max = 1;
                    break;
                case '*':
                    min = 0;
                    max = -1;
                    break;
                case '+':
                    min = 1;
                    max = -1;
                    break;
                case '{':
                    firstPos = this.pos - 1;
                    min = this.ReadNumber();
                    max = min;
                    if (this.PeekChar(0) == ',')
                    {
                        this.ReadChar(',');
                        max = -1;
                        if (this.PeekChar(0) != '}')
                        {
                            max = this.ReadNumber();
                        }
                    }

                    this.ReadChar('}');
                    if (max == 0 || (max > 0 && min > max))
                    {
                        throw new RegExpException(
                            RegExpException.ErrorType.InvalidRepeatCount,
                            firstPos,
                            this.pattern);
                    }

                    break;
                default:
                    throw new RegExpException(
                        RegExpException.ErrorType.UnexpectedCharacter,
                        this.pos - 1,
                        this.pattern);
            }

            // Read operator mode
            if (this.PeekChar(0) == '?')
            {
                this.ReadChar('?');
                type = RepeatElement.RepeatType.RELUCTANT;
            }
            else if (this.PeekChar(0) == '+')
            {
                this.ReadChar('+');
                type = RepeatElement.RepeatType.POSSESSIVE;
            }

            return new RepeatElement(elem, min, max, type);
        }
        
        /// <summary>
        /// Parses a regular expression character set. This method handles
        /// the contents of the '[...]' construct in a regular expression.
        /// </summary>
        /// <returns>The element representing this character set</returns>
        /// <exception cref="RegExpException">
        /// If an error was encountered in the
        /// pattern string
        /// </exception>
        private Element ParseCharSet()
        {
            CharacterSetElement charset;
            Element elem;
            bool repeat = true;
            char start;
            char end;

            if (this.PeekChar(0) == '^')
            {
                this.ReadChar('^');
                charset = new CharacterSetElement(true);
            }
            else
            {
                charset = new CharacterSetElement(false);
            }

            while (this.PeekChar(0) > 0 && repeat)
            {
                start = (char)this.PeekChar(0);
                switch (start)
                {
                    case ']':
                        repeat = false;
                        break;
                    case '\\':
                        elem = this.ParseEscapeChar();
                        if (elem is StringElement)
                        {
                            charset.AddCharacters((StringElement)elem);
                        }
                        else
                        {
                            charset.AddCharacterSet((CharacterSetElement)elem);
                        }

                        break;
                    default:
                        this.ReadChar(start);
                        if (this.PeekChar(0) == '-'
                            && this.PeekChar(1) > 0
                            && this.PeekChar(1) != ']')
                        {
                            this.ReadChar('-');
                            end = this.ReadChar();
                            charset.AddRange(this.FixChar(start), this.FixChar(end));
                        }
                        else
                        {
                            charset.AddCharacter(this.FixChar(start));
                        }

                        break;
                }
            }

            return charset;
        }

        /// <summary>
        /// Parses a regular expression character. This method handles
        /// a single normal character in a regular expression.
        /// </summary>
        /// <returns>The element representing this character</returns>
        /// <exception cref="RegExpException">
        /// if an error was encountered in the
        /// pattern string
        /// </exception>
        private Element ParseChar()
        {
            switch (this.PeekChar(0))
            {
                case '\\':
                    return this.ParseEscapeChar();
                case '^':
                case '$':
                    throw new RegExpException(
                        RegExpException.ErrorType.UnsupportedSpecialCharacter,
                        this.pos,
                        this.pattern);
                default:
                    return new StringElement(this.FixChar(this.ReadChar()));
            }
        }
        
        /// <summary>
        /// Parses a regular expression character escape. This method
        /// handles a single character escape in a regular expression.
        /// </summary>
        /// <returns>The element representing this character escape</returns>
        /// <exception cref="RegExpException">
        /// if an error was encountered in the
        /// pattern string
        /// </exception>
        private Element ParseEscapeChar()
        {
            char c;
            string str;
            int value;

            this.ReadChar('\\');
            c = this.ReadChar();
            switch (c)
            {
                case '0':
                    c = this.ReadChar();
                    if (c < '0' || c > '3')
                    {
                        throw new RegExpException(
                            RegExpException.ErrorType.UnsupportedEscapeCharacter,
                            this.pos - 3,
                            this.pattern);
                    }

                    value = c - '0';
                    c = (char)this.PeekChar(0);
                    if (c >= '0' && c <= '7')
                    {
                        value *= 8;
                        value += this.ReadChar() - '0';
                        c = (char)this.PeekChar(0);
                        if (c >= '0' && c <= '7')
                        {
                            value *= 8;
                            value += this.ReadChar() - '0';
                        }
                    }

                    return new StringElement(this.FixChar((char)value));
                case 'x':
                    str = this.ReadChar().ToString() +
                          this.ReadChar().ToString();
                    try
                    {
                        value = int.Parse(
                            str,
                            NumberStyles.AllowHexSpecifier);
                        return new StringElement(this.FixChar((char)value));
                    }
                    catch (FormatException)
                    {
                        throw new RegExpException(
                            RegExpException.ErrorType.UnsupportedEscapeCharacter,
                            pos - str.Length - 2,
                            this.pattern);
                    }

                case 'u':
                    str = this.ReadChar().ToString() +
                          this.ReadChar().ToString() +
                          this.ReadChar().ToString() +
                          this.ReadChar().ToString();
                    try
                    {
                        value = int.Parse(
                            str,
                            NumberStyles.AllowHexSpecifier);
                        return new StringElement(this.FixChar((char)value));
                    }
                    catch (FormatException)
                    {
                        throw new RegExpException(
                            RegExpException.ErrorType.UnsupportedEscapeCharacter,
                            pos - str.Length - 2,
                            this.pattern);
                    }

                case 't':
                    return new StringElement('\t');
                case 'n':
                    return new StringElement('\n');
                case 'r':
                    return new StringElement('\r');
                case 'f':
                    return new StringElement('\f');
                case 'a':
                    return new StringElement('\u0007');
                case 'e':
                    return new StringElement('\u001B');
                case 'd':
                    return CharacterSetElement.DIGIT;
                case 'D':
                    return CharacterSetElement.NONDIGIT;
                case 's':
                    return CharacterSetElement.WHITESPACE;
                case 'S':
                    return CharacterSetElement.NONWHITESPACE;
                case 'w':
                    return CharacterSetElement.WORD;
                case 'W':
                    return CharacterSetElement.NONWORD;
                default:
                    if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
                    {
                        throw new RegExpException(
                            RegExpException.ErrorType.UnsupportedEscapeCharacter,
                            this.pos - 2,
                            this.pattern);
                    }

                    return new StringElement(this.FixChar(c));
            }
        }
        
        /// <summary>
        /// Adjusts a character for inclusion in a string or character
        /// set element. For case-insensitive regular expressions, this
        /// transforms the character to lower-case.
        /// </summary>
        /// <param name="c">The input character</param>
        /// <returns>The adjusted character</returns>
        private char FixChar(char c)
        {
            return this.ignoreCase ? char.ToLower(c) : c;
        }

        /// <summary>
        /// Reads a number from the pattern. If the next character isn't a
        /// numeric character, an exception is thrown. This method reads
        /// several consecutive numeric characters.
        /// </summary>
        /// <returns>The numeric value read</returns>
        /// <exception cref="RegExpException">
        /// if an error was encountered in the
        /// pattern string
        /// </exception>
        private int ReadNumber()
        {
            StringBuilder buf = new StringBuilder();
            int c;

            c = this.PeekChar(0);
            while (c >= '0' && c <= '9')
            {
                buf.Append(this.ReadChar());
                c = this.PeekChar(0);
            }

            if (buf.Length <= 0)
            {
                throw new RegExpException(
                    RegExpException.ErrorType.UnexpectedCharacter,
                    this.pos,
                    this.pattern);
            }

            return int.Parse(buf.ToString());
        }

        /// <summary>
        /// Reads the next character in the pattern. If no next character
        /// exists, an exception is thrown.
        /// </summary>
        /// <returns>The character read</returns>
        /// <exception cref="RegExpException">
        /// If no next character was available in the pattern string
        /// </exception>
        private char ReadChar()
        {
            int c = this.PeekChar(0);

            if (c < 0)
            {
                throw new RegExpException(
                    RegExpException.ErrorType.UnterminatedPattern,
                    this.pos,
                    this.pattern);
            }
            else
            {
                this.pos++;
                return (char)c;
            }
        }

        /// <summary>
        /// Reads the next character in the pattern. If the character
        /// wasn't the specified one, an exception is thrown.
        /// </summary>
        /// <param name="c">The character to read</param>
        /// <returns>The character read</returns>
        /// <exception cref="RegExpException">
        /// if the character read didn't match the specified 
        /// one, or if no next character was available in the
        /// pattern string
        /// </exception>
        private char ReadChar(char c)
        {
            if (c != this.ReadChar())
            {
                throw new RegExpException(
                    RegExpException.ErrorType.UnexpectedCharacter,
                    this.pos - 1,
                    this.pattern);
            }

            return c;
        }
        
        /// <summary>
        /// Returns a character that has not yet been read from the
        /// pattern. If the requested position is beyond the end of the
        /// pattern string, -1 is returned.
        /// </summary>
        /// <param name="count">The preview position, from zero (0)</param>
        /// <returns>
        /// The character found, or -1 if beyond the end of the 
        /// pattern string
        /// </returns>
        private int PeekChar(int count)
        {
            if (this.pos + count < this.pattern.Length)
            {
                return this.pattern[this.pos + count];
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// Combines a list of elements. This method takes care to always
        /// concatenate adjacent string elements into a single string
        /// element.
        /// </summary>
        /// <param name="list">The list with elements</param>
        /// <returns>The combined element</returns>
        private Element CombineElements(ArrayList list)
        {
            Element prev;
            Element elem;
            string str;
            int i;

            // Concatenate string elements
            prev = (Element)list[0];
            for (i = 1; i < list.Count; i++)
            {
                elem = (Element)list[i];
                if (prev is StringElement
                 && elem is StringElement)
                {
                    str = ((StringElement)prev).String +
                          ((StringElement)elem).String;
                    elem = new StringElement(str);
                    list.RemoveAt(i);
                    list[i - 1] = elem;
                    i--;
                }

                prev = elem;
            }

            // Combine all remaining elements
            elem = (Element)list[list.Count - 1];
            for (i = list.Count - 2; i >= 0; i--)
            {
                prev = (Element)list[i];
                elem = new CombineElement(prev, elem);
            }

            return elem;
        }
    }
}