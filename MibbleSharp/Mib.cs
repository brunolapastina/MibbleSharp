// <copyright file="Mib.cs" company="None">
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
   using MibbleSharp.Value;
   using System.Collections.Generic;
   using System.Linq;

   /// <summary>
   /// An SNMP MIB module. This class contains all the information
   /// from a single MIB module, including all defined types and values.
   /// Note that a single MIB file may contain several such modules,
   /// although that is not very common. MIB files are loaded through a
   /// MibLoader.
   /// </summary>
   /// <seealso href="http://www.ietf.org/rfc/rfc3411.txt">
   /// RFC 3411 - An Architecture for Describing SNMP Management Frameworks
   /// </seealso>
   public class Mib : IMibContext
   {

      /// <summary>The MIB name.</summary>
      private string name = null;

      /// <summary>
      /// The MIB symbol name map. This maps the symbol names to their
      /// respective MIB symbol objects.
      /// </summary>
      private readonly Dictionary<string, MibSymbol> symbolNameMap = new Dictionary<string, MibSymbol>();

      /// <summary>
      /// The MIB symbol value map. This maps the symbol values to their
      /// respective MIB symbol objects. Only the value symbols with
      /// either a number or an object identifier value is present in
      /// this map.
      /// </summary>
      private readonly Dictionary<string, MibValueSymbol> symbolValueMap = new Dictionary<string, MibValueSymbol>();

      /// <summary>
      /// Initializes a new instance of the <see cref="Mib"/> class.
      /// This will NOT read the actual MIB file, but only creates 
      /// an empty container. The symbols are then added during the 
      /// first analysis pass (of two), leaving symbols in the MIB
      /// possibly containing unresolved references.
      /// A separate call to Initialize() must be made once all
      /// referenced MIB modules have also been loaded.
      /// </summary>
      /// <param name="file">The MIB file name</param>
      /// <param name="loader">The MIB loader to use for imports</param>
      /// <param name="log">The MIB log to use for errors</param>
      /// <see cref="Initialize"/>
      public Mib(string file, MibLoader loader, MibLoaderLog log)
      {
         this.File = file;
         this.Loader = loader;
         this.Log = log;
      }

      /// <summary>
      /// Gets or sets a value indicating whether the Mib is explicitly loaded.
      /// A MIB module is considered explicitly loaded if the file or resource
      /// containing the MIB definition was loaded by a direct call to
      /// the MIB loader. Implicitly loaded MIB modules are loaded as a
      /// result of import statements in explicitly loaded MIBs.
      /// </summary>
      /// <returns> true if this MIB module was explicitly loaded, or false 
      /// otherwise
      /// </returns>
      public bool Loaded { get; set; } = false;

      /// <summary>
      /// Gets or sets the MIB name. This is sometimes also referred to as
      /// the MIB module name. The setter should only be called by the Mib
      /// analysis classes
      /// </summary>
      /// <returns>The MIB name</returns>
      public string Name
      {
         get
         {
            return this.name;
         }

         set
         {
            this.name = value;
            if (this.File == null)
            {
               this.File = value;
            }
         }
      }

      /// <summary>
      /// Gets the filename
      /// </summary>
      public string File { get; private set; }

      /// <summary>
      /// Gets the MIB loader used when loading this MIB.
      /// </summary>
      public MibLoader Loader { get; }

      /// <summary>
      /// Gets the loader log used when loading this MIB.
      /// </summary>
      public MibLoaderLog Log { get; }

      /// <summary>
      /// Gets or sets the SMI version used for defining this MIB. This
      /// number can be either 1 (for SMIv1) or 2 (for SMIv2). It is set
      /// based on which macros are used in the MIB file. The setter
      /// should only be called by the MIB analysis classes.
      /// </summary>
      public int SmiVersion { get; set; } = 1;

      /// <summary>
      /// Gets or sets the Mib file's header comment
      /// </summary>
      public string HeaderComment { get; set; } = null;

      /// <summary>
      /// Gets or sets the Mib file's footer comment
      /// </summary>
      public string FooterComment { get; set; } = null;

      /// <summary>
      /// Gets all imports for the Mib
      /// </summary>
      public IList<MibImport> Imports { get; } = new List<MibImport>();

      /// <summary>
      /// Gets all symbols for the Mib
      /// </summary>
      public IList<MibSymbol> Symbols { get; } = new List<MibSymbol>();

      /// <summary>
      /// Gets all MIB:s that are dependant on this one. The search
      /// will iterate through all loaded MIB:s and return those that
      /// import this one.
      /// </summary>
      /// <see cref="MibLoader"/>
      public IList<Mib> ImportingMibs => 
         this.Loader.AllMibs
         .Where(m => m != this && m.GetImport(this.name) != null)
         .ToList();

      /// <summary>
      /// Initializes the MIB file. This will resolve all imported MIB
      /// file references. Note that this method shouldn't be called
      /// until all referenced MIB files (and their respective
      /// references) have been loaded.
      /// </summary>
      /// <exception cref="MibLoaderException">
      /// If the MIB file couldn't be analyzed correctly
      /// </exception>
      /// <see cref="Validate"/>
      public void Initialize()
      {
         int errors = this.Log.ErrorCount;

         // Resolve imported MIB files
         foreach (MibImport imp in this.Imports)
         {
            try
            {
               imp.Initialize(this.Log);
            }
            catch (MibException e)
            {
               this.Log.AddError(e.Location, e.Message);
            }
         }

         // Check for errors
         if (errors != this.Log.ErrorCount)
         {
            throw new MibLoaderException(this.Log);
         }
      }

      /// <summary>
      /// Validates the MIB file.This will resolve all type and value
      /// references in the MIB symbols, while also validating them for
      /// consistency.Note that this method shouldn't be called until
      /// all referenced MIB files (and their respective references)
      /// have been initialized.
      /// </summary>
      /// <exception cref="MibLoaderException">
      /// If the MIB file couldn't be  analyzed correctly
      /// </exception>
      public void Validate()
      {
         int errors = this.Log.ErrorCount;

         // Validate all symbols
         for (int i = 0; i < this.Symbols.Count; ++i)
         {
            try
            {
               Symbols[i].Initialize(this.Log);
            }
            catch (MibException e)
            {
               this.Log.AddError(e.Location, e.Message);
            }

            if ((Symbols[i] is MibValueSymbol value) && (value.Value is NumberValue || value.Value is ObjectIdentifierValue))
            {
               this.symbolValueMap.Add(value.Value.ToString(), value);
            }
         }

         // Check for errors
         if (errors != this.Log.ErrorCount)
         {
            throw new MibLoaderException(this.Log);
         }
      }

      /// <summary>
      /// Clears and prepares this MIB for garbage collection. This method
      /// will recursively clear all associated symbols, making sure that
      /// no data structures references symbols from this MIB. Obviously,
      /// this method shouldn't be called unless all dependant MIBs have
      /// been cleared first.
      /// </summary>
      public void Clear()
      {
         this.Imports.Clear();
         
         foreach (MibSymbol symbol in this.Symbols)
         {
            symbol.Clear();
         }

         this.Symbols.Clear();
         this.symbolNameMap.Clear();
         this.symbolValueMap.Clear();
      }

      /// <summary>
      /// Compares this MIB to another object. This method will return
      /// true if the object is a string containing the MIB name, a file
      /// containing the MIB file, or a Mib having the same name.
      /// </summary>
      /// <param name="obj">The object to compare with</param>
      /// <returns>true if the objects are equal, false otherwise</returns>
      public override bool Equals(object obj)
      {
         if (obj is string)
         {
            return this.name.Equals(obj);
         }
         else if (obj is Mib)
         {
            return obj.Equals(this.name);
         }
         else
         {
            return false;
         }
      }

      /// <summary> Returns the hash code value for the object. This method is
      /// re-implemented to fulfill the contract of returning the same
      /// hash code for objects that are considered equal.
      /// </summary>
      /// <returns>The hash code value for the object</returns>
      public override int GetHashCode()
      {
         return this.name.GetHashCode();
      }

      /// <summary>
      /// Gets a MIB import reference.
      /// </summary>
      /// <param name="name">The import name to be searched or</param>
      /// <returns>
      /// The MibImport object for the name, or null if no match is found
      /// </returns>
      public MibImport GetImport(string name)
      {
         foreach (MibImport imp in this.Imports)
         {
            if (imp.Name.Equals(name))
            {
               return imp;
            }
         }

         return null;
      }

      /// <summary>
      /// Adds a reference to an imported MIB file.
      /// </summary>
      /// <param name="impRef">The import reference to be added</param>
      public void AddImport(MibImport impRef)
      {
         this.Imports.Add(impRef);
      }

      /// <summary>
      /// Returns a symbol from this MIB.
      /// </summary>
      /// <param name="name">The symbol name to be searched for</param>
      /// <returns>The Mib symbol, or null if none is found</returns>
      public MibSymbol GetSymbol(string name)
      {
         this.symbolNameMap.TryGetValue(name, out MibSymbol ms);
         return ms;
      }

      /// <summary>
      /// Returns a value symbol from this MIB.
      /// </summary>
      /// <param name="value">The symbol value</param>
      /// <returns>The Mib Value Symbol, or null if not found</returns>
      public MibValueSymbol GetSymbolByValue(string value)
      {
         this.symbolValueMap.TryGetValue(value, out MibValueSymbol ms);
         return ms;
      }

      /// <summary>Returns a value symbol from this MIB.</summary>
      /// <param name="value">The symbol value to look up</param>
      /// <returns> the MIB value symbol, or null if not found</returns>
      public MibValueSymbol GetSymbolByValue(MibValue value)
      {
         this.symbolValueMap.TryGetValue(value.ToString(), out MibValueSymbol ms);
         return ms;
      }

      /// <summary>
      /// Returns a value symbol from this MIB. The search is performed
      /// by using the strictly numerical OID value specified. Differing
      /// from the getSymbolByValue() methods, this method may return a
      /// symbol with only a partial OID match. If an exact match for
      /// the OID is present in the MIB, this method will always return
      /// the same result as getSymbolByValue(). Otherwise, the symbol
      /// with the longest matching OID will be returned, making it
      /// possible to identify a MIB symbol from an OID containing table
      /// row indices or similar.
      /// </summary>
      /// <param name="oid">The numeric OID value</param>
      /// <returns>The MIB value symbol, or null if not found</returns>
      public MibValueSymbol GetSymbolByOid(string oid)
      {
         int pos;

         do
         {
            MibValueSymbol sym = this.GetSymbolByValue(oid);
            if (sym != null)
            {
               return sym;
            }

            pos = oid.LastIndexOf(".");
            if (pos > 0)
            {
               oid = oid.Substring(0, pos);
            }
         }
         while (pos > 0);

         return null;
      }

      /// <summary>
      /// Returns the root MIB value symbol. This value symbol is
      /// normally the module identifier (in SMIv2), but may also be
      /// just the base object identifier in the MIB.
      /// </summary>
      /// <returns>The root MIB value symbol</returns>
      public MibValueSymbol GetRootSymbol()
      {
         MibValueSymbol root = null;
         MibValueSymbol parent;

         root = this.Symbols.Where(s => s is MibValueSymbol).FirstOrDefault() as MibValueSymbol;
         /*
         foreach (MibSymbol m in this.symbolList)
         {
             root = m as MibValueSymbol;
             if (root != null)
             {
                 break;
             }
         }
         */
         while (root != null && (parent = root.Parent) != null)
         {
            if (!root.Mib.Equals(parent.Mib))
            {
               break;
            }

            root = parent;
         }

         return root;
      }

      /// <summary>
      /// Adds a symbol to this MIB.
      /// </summary>
      /// <param name="symbol">The symbol to add</param>
      public void AddSymbol(MibSymbol symbol)
      {
         this.Symbols.Add(symbol);
         this.symbolNameMap.Add(symbol.Name, symbol);
      }

      /// <summary>
      /// Searches for a named MIB symbol. This method is required to
      /// implement the MibContext interface but returns the same results
      /// as getSymbol(String).
      /// NOTE: This is an internal method that should
      /// only be called by the MIB loader.
      /// </summary>
      /// <param name="symbolName">The symbol name</param>
      /// <param name="expanded">The expanded scope flag</param>
      /// <returns>The Mib Symbol, or null if not found</returns>
      public MibSymbol FindSymbol(string symbolName, bool expanded)
      {
         return this.GetSymbol(symbolName);
      }

      /// <summary>
      /// Returns a string representation of this object.
      /// </summary>
      /// <returns>A string representation of this object</returns>
      public override string ToString()
      {
         return this.Name;
      }
   }
}
