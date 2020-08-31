// <copyright file="AlternativeElement.cs" company="None">
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
    using System.IO;

    /// <summary>
    /// A regular expression alternative element. This element matches
    /// the longest alternative element.
    /// </summary>
    internal class AlternativeElement : Element
    {
        /// <summary>
        /// The first alternative element
        /// </summary>
        private readonly Element elem1;

        /// <summary>
        /// The second alternative element.
        /// </summary>
        private readonly Element elem2;

        /// <summary>
        /// Initializes a new instance of the <see cref="AlternativeElement"/> class.
        /// </summary>
        /// <param name="first">The first alternative</param>
        /// <param name="second">The second alternative</param>
        public AlternativeElement(Element first, Element second)
        {
            this.elem1 = first;
            this.elem2 = second;
        }

        /// <summary>
        /// Creates a copy of this element. The copy will be an
        /// instance of the same class matching the same strings.
        /// Copies of elements are necessary to allow elements to cache
        /// intermediate results while matching strings without
        /// interfering with other threads.
        /// </summary>
        /// <returns>A copy of this element</returns>
        public override object Clone()
        {
            return new AlternativeElement(this.elem1, this.elem2);
        }

        /// <summary>
        /// Returns the length of a matching string starting at the
        /// specified position. The number of matches to skip can also
        /// be specified, but numbers higher than zero (0) cause a
        /// failed match for any element that doesn't attempt to
        /// combine other elements.
        /// </summary>
        /// <param name="m">the matcher being used</param>
        /// <param name="buffer">the input character buffer to match</param>
        /// <param name="start">The starting position</param>
        /// <param name="skip">The number of matches to skip</param>
        /// <returns>
        /// the length of the longest matching string, or
        /// -1 if no match was found
        /// </returns>
        /// <exception cref="IOException">If an I/O error occurred</exception>
        public override int Match(
            Matcher m,
            ReaderBuffer buffer,
            int start,
            int skip)
        {
            int length = 0;
            int skip1 = 0;
            int skip2 = 0;

            while (length >= 0 && skip1 + skip2 <= skip)
            {
                int length1 = this.elem1.Match(m, buffer, start, skip1);
                int length2 = this.elem2.Match(m, buffer, start, skip2);
                if (length1 >= length2)
                {
                    length = length1;
                    skip1++;
                }
                else
                {
                    length = length2;
                    skip2++;
                }
            }

            return length;
        }

        /// <summary>
        /// Prints this element to the specified output stream.
        /// </summary>
        /// <param name="output">The output stream to write to</param>
        /// <param name="indent">The current indentation</param>
        public override void PrintTo(TextWriter output, string indent)
        {
            output.WriteLine(indent + "Alternative 1");
            this.elem1.PrintTo(output, indent + "  ");

            output.WriteLine(indent + "Alternative 2");
            this.elem2.PrintTo(output, indent + "  ");
        }
    }
}
