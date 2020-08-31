// <copyright file="Parser.cs" company="None">
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
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    /// <summary>
    /// A base parser class. This class provides the standard parser
    /// interface, as well as token handling.
    /// </summary>
    public abstract class Parser
    {
        /// <summary>
        /// The tokenizer to use.
        /// </summary> 
        private Tokenizer tokenizer;

        /// <summary>
        /// The parser initialization flag.
        /// </summary> 
        private bool initialized = false;

        /// <summary>
        /// The analyzer to use for callbacks.
        /// </summary> 
        private Analyzer analyzer;

        /// <summary>
        /// The list of production patterns.
        /// </summary> 
        private readonly List<ProductionPattern> patterns = new List<ProductionPattern>();

        /// <summary>
        /// The map with production patterns and their id:s. This map
        /// contains the production patterns indexed by their id:s.
        /// </summary> 
        private readonly Dictionary<int, ProductionPattern> patternIds = new Dictionary<int, ProductionPattern>();

        /// <summary>
        /// The list of buffered tokens. This list will contain tokens that
        /// have been read from the tokenizer, but not yet consumed.
        /// </summary> 
        private readonly List<Token> tokens = new List<Token>();

        /// <summary>
        /// The error log. All parse errors will be added to this log as
        /// the parser attempts to recover from the error. If the error
        /// count is higher than zero (0), this log will be thrown as the
        /// result from the parse() method.
        /// </summary> 
        private ParserLogException errorLog = new ParserLogException();

        /// <summary>
        /// The error recovery counter. This counter is initially set to a
        /// negative value to indicate that no error requiring recovery
        /// has been encountered. When a parse error is found, the counter
        /// is set to three (3), and is then decreased by one for each
        /// correctly read token until it reaches zero (0).
        /// </summary> 
        private int errorRecovery = -1;

        /// <summary>
        /// Initializes a new instance of the <see cref="Parser"/> class.
        /// </summary>
        /// <param name="input">The input stream to read from</param>
        /// <param name="analyzer">The analyzer callback to use</param>
        /// <exception cref="ParserCreationException">
        /// If the tokenizer couldn't be initialized correctly
        /// </exception>
        internal Parser(TextReader input, Analyzer analyzer)
        {
            this.tokenizer = new Tokenizer(input);
            this.analyzer = analyzer;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Parser"/> class.
        /// </summary>
        /// <param name="tokenizer">The tokenizer to use</param>
        /// <param name="analyzer">The analyzer callback to use</param>
        internal Parser(Tokenizer tokenizer, Analyzer analyzer)
        {
            this.tokenizer = tokenizer;
            this.analyzer = analyzer;
        }

        /// <summary>
        /// Gets or sets the tokenizer. This property contains
        /// the tokenizer in use by this parser.
        /// </summary>         
        public Tokenizer Tokenizer
        {
            get
            {
                return this.tokenizer;
            }

            set
            {
                this.tokenizer = value;
            }
        }

        /// <summary>
        /// Gets the analyzer (read-only). This property contains
        /// the analyzer in use by this parser.
        /// </summary>         
        public Analyzer Analyzer
        {
            get
            {
                return this.analyzer;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the parser is initialized.
        /// Normally this flag is set by the prepare() method, but this method 
        /// allows further modifications to it.
        /// </summary>
        internal bool Initialized
        {
            get
            {
                return this.initialized;
            }

            set
            {
                this.initialized = value;
            }
        }

        /// <summary>
        /// Adds a new production pattern to the parser. The first pattern
        /// added is assumed to be the starting point in the grammar. The
        /// patterns added may be validated to some extent.
        /// </summary>
        /// <param name="pattern">The pattern to add</param>
        /// <exception cref="ParserCreationException">
        /// If the pattern couldn't be added correctly to the parser
        /// </exception>
        public virtual void AddPattern(ProductionPattern pattern)
        {
            if (pattern.Count <= 0)
            {
                throw new ParserCreationException(
                    ParserCreationException.ErrorType.InvalidProduction,
                    pattern.Name,
                    "no production alternatives are present (must have at least one)");
            }

            if (this.patternIds.ContainsKey(pattern.Id))
            {
                string msg = "another pattern with the same id (" + pattern.Id +
                    ") has already been added";
                throw new ParserCreationException(
                    ParserCreationException.ErrorType.InvalidProduction,
                    pattern.Name,
                    msg);
            }

            this.patterns.Add(pattern);
            this.patternIds.Add(pattern.Id, pattern);
            this.Initialized = false;
        }

        /// <summary>
        /// Initializes the parser. All the added production patterns will
        /// be analyzed for ambiguities and errors. This method also
        /// initializes internal data structures used during the parsing.
        /// </summary>
        /// <exception cref="ParserCreationException">
        /// If the parser couldn't be initialized correctly
        /// </exception>
        public virtual void Prepare()
        {
            if (this.patterns.Count <= 0)
            {
                throw new ParserCreationException(
                    ParserCreationException.ErrorType.InvalidParser,
                    "no production patterns have been added");
            }

            foreach (var pat in this.patterns)
            {
                this.CheckPattern(pat);
            }

            this.Initialized = true;
        }
        
        /// <summary>
        /// Resets this parser for usage with another input stream. The
        /// associated tokenizer and analyzer will also be reset. This
        /// method will clear all the internal state and the error log in
        /// the parser. It is normally called in order to reuse a parser
        /// and tokenizer pair with multiple input streams, thereby
        /// avoiding the cost of re-analyzing the grammar structures.
        /// </summary>
        /// <param name="input">The new input stream to read</param>
        /// <see cref="Tokenizer.Reset(TextReader)"/>
        /// <see cref="Analyzer.Reset"/>
        public void Reset(TextReader input)
        {
            this.tokenizer.Reset(input);
            this.analyzer.Reset();
        }

        /// <summary>
        /// Resets this parser for usage with another input stream. The
        /// associated tokenizer will also be reset and the analyzer
        /// replaced. This method will clear all the internal state and
        /// the error log in the parser. It is normally called in order
        /// to reuse a parser and tokenizer pair with multiple input
        /// streams, thereby avoiding the cost of re-analyzing the
        /// grammar structures.
        /// </summary>
        /// <param name="input">The new input stream to read</param>
        /// <param name="analyzer">The new analyzer callback to use</param>
        /// <see cref="Tokenizer.Reset(TextReader)"/>
        public void Reset(TextReader input, Analyzer analyzer)
        {
            this.tokenizer.Reset(input);
            this.analyzer = analyzer;
        }

        /// <summary>
        /// Parses the token stream and returns a parse tree. This
        /// method will call Prepare() if not previously called. It
        /// will also call the Reset() method, to make sure that only
        /// the Tokenizer.Reset() method must be explicitly called in
        /// order to reuse a parser for multiple input streams. In case
        /// of a parse error, the parser will attempt to recover and
        /// throw all the errors found in a parser log exception in the
        /// end.
        /// </summary>
        /// <returns>The parse tree</returns>
        /// <exception cref="ParserCreationException">
        /// If the parser couldn't be initialized correctly
        /// </exception>
        /// <exception cref="ParserLogException">
        /// If the input couldn't be parsed correctly
        /// </exception>
        /// <see cref="Prepare"/>
        /// <see cref="Reset(TextReader)"/>
        /// <see cref="Tokenizer.Reset(TextReader)"/>
        public Node Parse()
        {
            Node root = null;

            // Initialize parser
            if (!this.initialized)
            {
                this.Prepare();
            }

            this.tokens.Clear();
            this.errorLog = new ParserLogException();
            this.errorRecovery = -1;

            // Parse input
            try
            {
                root = this.ParseStart();
            }
            catch (ParseException e)
            {
                this.AddError(e, true);
            }

            // Check for errors
            if (this.errorLog.Count > 0)
            {
                throw this.errorLog;
            }

            return root;
        }
        
        /// <summary>
        /// Returns a string representation of this parser. The string will
        /// contain all the production definitions and various additional
        /// information.
        /// </summary>
        /// <returns>
        /// A detailed string representation of this parser
        /// </returns>
        public override string ToString()
        {
            StringBuilder buffer = new StringBuilder();

            foreach (var pat in this.patterns)
            {
                buffer.Append(this.ToString(pat));
                buffer.Append("\n");
            }

            return buffer.ToString();
        }

        /// <summary>
        /// Adds an error to the error log. If the parser is in error
        /// recovery mode, the error will not be added to the log. If the
        /// recovery flag is set, this method will set the error recovery
        /// counter thus enter error recovery mode. Only lexical or
        /// syntactical errors require recovery, so this flag shouldn't be
        /// set otherwise.
        /// </summary>
        /// <param name="e">The error to add</param>
        /// <param name="recovery">The recover flag</param>
        internal void AddError(ParseException e, bool recovery)
        {
            if (this.errorRecovery <= 0)
            {
                this.errorLog.AddError(e);
            }

            if (recovery)
            {
                this.errorRecovery = 3;
            }
        }

        /// <summary>
        /// Returns the production pattern with the specified id.
        /// </summary>
        /// <param name="id">The production pattern id</param>
        /// <returns>
        /// The production pattern found, or null if non-existent
        /// </returns>
        internal ProductionPattern GetPattern(int id)
        {
            return this.patternIds[id];
        }

        /// <summary>
        /// Returns the production pattern for the starting production.
        /// </summary>
        /// <returns>
        /// The start production pattern, or null if no patterns have been added
        /// </returns>
        internal ProductionPattern GetStartPattern()
        {
            if (this.patterns.Count <= 0)
            {
                return null;
            }
            else
            {
                return this.patterns[0];
            }
        }

        /// <summary>
        /// Returns the ordered set of production patterns.
        /// </summary>
        /// <returns>
        /// The ordered set of production patterns
        /// </returns>
        internal ICollection GetPatterns()
        {
            return this.patterns;
        }

        /// <summary>
        /// Handles the parser entering a production. This method calls the
        /// appropriate analyzer callback if the node is not hidden. Note
        /// that this method will not call any callback if an error
        /// requiring recovery has occurred.
        /// </summary>
        /// <param name="node">The parse tree node</param>        
        internal void EnterNode(Node node)
        {
            if (!node.Hidden && this.errorRecovery < 0)
            {
                try
                {
                    this.analyzer.Enter(node);
                }
                catch (ParseException e)
                {
                    this.AddError(e, false);
                }
            }
        }

        /// <summary>
        /// Handles the parser leaving a production. This method calls the
        /// appropriate analyzer callback if the node is not hidden, and
        /// returns the result. Note that this method will not call any
        /// callback if an error requiring recovery has occurred.
        /// </summary>
        /// <param name="node">The parse tree node</param>
        /// <returns>
        /// The parse tree node, or null if no parse tree should be created
        /// </returns>
        internal Node ExitNode(Node node)
        {
            if (!node.Hidden && this.errorRecovery < 0)
            {
                try
                {
                    return this.analyzer.Exit(node);
                }
                catch (ParseException e)
                {
                    this.AddError(e, false);
                }
            }

            return node;
        }

        /// <summary>
        /// Handles the parser adding a child node to a production. This
        /// method calls the appropriate analyzer callback. Note that this
        /// method will not call any callback if an error requiring
        /// recovery has occurred.
        /// </summary>
        /// <param name="node">The parent parse tree node</param>
        /// <param name="child">The child parse tree node, or null</param>
        internal void AddNode(Production node, Node child)
        {
            if (this.errorRecovery >= 0)
            {
                // Do nothing
            }
            else if (node.Hidden)
            {
                node.AddChild(child);
            }
            else if (child != null && child.Hidden)
            {
                for (int i = 0; i < child.ChildCount; i++)
                {
                    this.AddNode(node, child[i]);
                }
            }
            else
            {
                try
                {
                    this.analyzer.Child(node, child);
                }
                catch (ParseException e)
                {
                    this.AddError(e, false);
                }
            }
        }

        /// <summary>
        /// Reads and consumes the next token in the queue. If no token
        /// was available for consumption, a parse error will be
        /// thrown.
        /// </summary>
        /// <returns>The token consumed</returns>
        /// <exception cref="ParseException">
        /// If the input stream couldn't be read or parsed correctly
        /// </exception>
        internal Token NextToken()
        {
            Token token = this.PeekToken(0);

            if (token != null)
            {
                this.tokens.RemoveAt(0);
                return token;
            }
            else
            {
                throw new ParseException(
                    ParseException.ErrorType.UnexpectedEOF,
                    null,
                    this.tokenizer.CurrentLine,
                    this.tokenizer.CurrentColumn);
            }
        }

        /// <summary>
        /// Reads and consumes the next token in the queue. If no token was
        /// available for consumption, a parse error will be thrown. A
        /// parse error will also be thrown if the token id didn't match
        /// the specified one.
        /// </summary>
        /// <param name="id">The expected token id</param>
        /// <returns>The token consumed</returns>
        /// <exception cref="ParseException">
        /// If the input stream couldn't be parsed correctly, or if the 
        /// token wasn't expected
        /// </exception>
        internal Token NextToken(int id)
        {
            Token token = this.NextToken();
            IList<string> list;

            if (token.Id == id)
            {
                if (this.errorRecovery > 0)
                {
                    this.errorRecovery--;
                }

                return token;
            }
            else
            {
                list = new List<string>(1);
                list.Add(this.tokenizer.GetPatternDescription(id));

                throw new ParseException(
                    ParseException.ErrorType.UnexpectedToken,
                    token.ToShortString(),
                    list,
                    token.StartLine,
                    token.StartColumn);
            }
        }

        /// <summary>
        /// Returns a token from the queue. This method is used to check
        /// coming tokens before they have been consumed. Any number of
        /// tokens forward can be checked.
        /// </summary>
        /// <param name="steps">The token queue number, zero (0) for first</param>
        /// <returns>
        /// The token in the queue, or null if no more tokens in the queue
        /// </returns>
        internal Token PeekToken(int steps)
        {
            Token token;

            while (steps >= this.tokens.Count)
            {
                try
                {
                    token = this.tokenizer.Next();
                    if (token == null)
                    {
                        return null;
                    }
                    else
                    {
                        this.tokens.Add(token);
                    }
                }
                catch (ParseException e)
                {
                    this.AddError(e, true);
                }
            }

            return this.tokens[steps];
        }

        /// <summary>
        /// Returns a token description for a specified token.
        /// </summary>
        /// <param name="token">The token to describe</param>
        /// <returns>The token description</returns>
        internal string GetTokenDescription(int token)
        {
            if (this.tokenizer == null)
            {
                return string.Empty;
            }
            else
            {
                return this.tokenizer.GetPatternDescription(token);
            }
        }

        /// <summary>
        /// Parses the token stream and returns a parse tree.
        /// </summary>
        /// <returns>The parse tree</returns>
        /// <exception cref="ParserLogException">
        /// If the input couldn't be parsed correctly
        /// </exception>
        protected abstract Node ParseStart();

        /// <summary>
        /// Factory method to create a new production node. This method
        /// can be overridden to provide other production implementations
        /// than the default one.
        /// </summary>
        /// <param name="pattern">The production pattern</param>
        /// <returns>The new production node</returns>
        protected virtual Production NewProduction(ProductionPattern pattern)
        {
            return this.analyzer.NewProduction(pattern);
        }
        
        /// <summary>
        /// Creates a new tokenizer for this parser. Can be overridden by
        /// a subclass to provide a custom implementation.
        /// </summary>
        /// <param name="input">The input stream to read from</param>
        /// <returns>The new tokenizer</returns>
        /// <exception cref="ParserCreationException">
        /// If the tokenizer couldn't be initialized correctly
        /// </exception>
        protected virtual Tokenizer NewTokenizer(TextReader input)
        {
            // TODO: This method should really be abstract, but it isn't in this
            //       version due to backwards compatibility requirements.
            return new Tokenizer(input);
        }

        /// <summary>
        /// Checks a production pattern for completeness. If some rule
        /// in the pattern referenced an production pattern not added
        /// to this parser, a parser creation exception will be thrown.
        /// </summary>
        /// <param name="pattern">The production pattern to check</param>
        /// <exception cref="ParserCreationException">
        /// If the pattern referenced a pattern not added to this parser
        /// </exception>
        private void CheckPattern(ProductionPattern pattern)
        {
            for (int i = 0; i < pattern.Count; i++)
            {
                this.CheckAlternative(pattern.Name, pattern[i]);
            }
        }

        /// <summary>
        /// Checks a production pattern alternative for completeness.
        /// If some element in the alternative referenced a production
        /// pattern not added to this parser, a parser creation
        /// exception will be thrown.
        /// </summary>
        /// <param name="name">The name of the pattern being checked</param>
        /// <param name="alt">The production pattern alternative</param>
        /// <exception cref="ParserCreationException">
        /// If the alternative referenced a pattern not added to this parser
        /// </exception>
        private void CheckAlternative(
            string name,
            ProductionPatternAlternative alt)
        {
            for (int i = 0; i < alt.Count; i++)
            {
                this.CheckElement(name, alt[i]);
            }
        }

        /// <summary>
        /// Checks a production pattern element for completeness. If
        /// the element references a production pattern not added to
        /// this parser, a parser creation exception will be thrown.
        /// </summary>
        /// <param name="name">The name of the pattern being checked</param>
        /// <param name="elem">The production pattern element to check</param>
        /// <exception cref="ParserCreationException">
        /// If the element referenced a pattern not added to this parser
        /// </exception>
        private void CheckElement(
            string name,
            ProductionPatternElement elem)
        {
            if (elem.IsProduction && this.GetPattern(elem.Id) == null)
            {
                string msg = "an undefined production pattern id (" + elem.Id +
                    ") is referenced";
                throw new ParserCreationException(
                    ParserCreationException.ErrorType.InvalidProduction,
                    name,
                    msg);
            }
        }

        /// <summary>
        /// Returns a string representation of a production pattern.
        /// </summary>
        /// <param name="prod">The production pattern</param>
        /// <returns>
        /// A detailed string representation of the pattern
        /// </returns>
        private string ToString(ProductionPattern prod)
        {
            StringBuilder buffer = new StringBuilder();
            StringBuilder indent = new StringBuilder();
            LookAheadSet set;
            int i;

            buffer.Append(prod.Name);
            buffer.Append(" (");
            buffer.Append(prod.Id);
            buffer.Append(") ");
            for (i = 0; i < buffer.Length; i++)
            {
                indent.Append(" ");
            }

            buffer.Append("= ");
            indent.Append("| ");

            for (i = 0; i < prod.Count; i++)
            {
                if (i > 0)
                {
                    buffer.Append(indent);
                }

                buffer.Append(this.ToString(prod[i]));
                buffer.Append("\n");
            }

            for (i = 0; i < prod.Count; i++)
            {
                set = prod[i].LookAhead;
                if (set.MaxLength > 1)
                {
                    buffer.Append("Using ");
                    buffer.Append(set.MaxLength);
                    buffer.Append(" token look-ahead for alternative ");
                    buffer.Append(i + 1);
                    buffer.Append(": ");
                    buffer.Append(set.ToString(this.tokenizer));
                    buffer.Append("\n");
                }
            }

            return buffer.ToString();
        }

        /// <summary>
        /// Returns a string representation of a production pattern
        /// alternative.
        /// </summary>
        /// <param name="alt">The production pattern alternative</param>
        /// <returns>
        /// A detailed string representation of the alternative
        /// </returns>
        private string ToString(ProductionPatternAlternative alt)
        {
            StringBuilder buffer = new StringBuilder();

            for (int i = 0; i < alt.Count; i++)
            {
                if (i > 0)
                {
                    buffer.Append(" ");
                }

                buffer.Append(this.ToString(alt[i]));
            }

            return buffer.ToString();
        }

        /// <summary>
        /// Returns a string representation of a production pattern
        /// element.
        /// </summary>
        /// <param name="elem">The production pattern element</param>
        /// <returns>
        /// A detailed string representation of the element
        /// </returns>
        private string ToString(ProductionPatternElement elem)
        {
            StringBuilder buffer = new StringBuilder();
            int min = elem.MinCount;
            int max = elem.MaxCount;

            if (min == 0 && max == 1)
            {
                buffer.Append("[");
            }

            if (elem.IsToken)
            {
                buffer.Append(this.GetTokenDescription(elem.Id));
            }
            else
            {
                buffer.Append(this.GetPattern(elem.Id).Name);
            }

            if (min == 0 && max == 1)
            {
                buffer.Append("]");
            }
            else if (min == 0 && max == int.MaxValue)
            {
                buffer.Append("*");
            }
            else if (min == 1 && max == int.MaxValue)
            {
                buffer.Append("+");
            }
            else if (min != 1 || max != 1)
            {
                buffer.Append("{");
                buffer.Append(min);
                buffer.Append(",");
                buffer.Append(max);
                buffer.Append("}");
            }

            return buffer.ToString();
        }
    }
}