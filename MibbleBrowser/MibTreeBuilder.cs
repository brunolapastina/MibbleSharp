

namespace MibbleBrowser
{
   using MibbleSharp;
   using MibbleSharp.Value;
   using System.Collections.Generic;
   using System.IO;
   using System.Windows.Forms;

   /// <summary>
   /// 
   /// </summary>
   class MibTreeBuilder
   {
      /// <summary>
      /// The treeview nodes will be added to
      /// </summary>
      private readonly TreeView treeView;

      /// <summary>
      /// 
      /// </summary>
      private readonly MibNode rootNode = new MibNode("Mibs", null);

      /// <summary>
      /// 
      /// </summary>
      private readonly Dictionary<MibValueSymbol, MibNode> nodes = new Dictionary<MibValueSymbol, MibNode>();

      /// <summary>
      /// The MibLoader to be used for this tree
      /// </summary>
      private readonly MibLoader loader = new MibLoader();

      protected internal MibTreeBuilder(TreeView treeView)
      {
         this.treeView = treeView;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="filename"></param>
      protected internal void LoadMibFile(string filename)
      {
         Mib mib;

         this.Initialize();
         /*
         if(!File.Exists(filename))
         {
             throw new FileNotFoundException();
         }*/

         if (!loader.HasDir(Directory.GetParent(filename).FullName))
         {
            // loader.RemoveAllDirs();
            loader.AddDir(Directory.GetParent(filename).FullName);
         }

         if (File.Exists(filename))
         {
            using (StreamReader sr = new StreamReader(filename))
            {
               mib = loader.Load(sr);
            }
         }
         else
         {
            mib = loader.Load(filename);
         }

         this.treeView.BeginUpdate();

         foreach (var sym in mib.Symbols)
         {
            this.AddSymbol(sym);
         }

         this.treeView.EndUpdate();
      }

      private void Initialize()
      {
         if (treeView.Nodes.Count == 0 || treeView.Nodes[0].Equals(rootNode) == false)
         {
            treeView.Nodes.Clear();
            nodes.Clear();
            treeView.Nodes.Add(rootNode);
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="sym"></param>
      private void AddSymbol(MibSymbol sym)
      {
         if (!(sym is MibValueSymbol valSym))
         {
            return;
         }

         if (!(valSym.Value is ObjectIdentifierValue oiv))
         {
            return;
         }

         this.AddToTree(oiv);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="oiv"></param>
      private MibNode AddToTree(ObjectIdentifierValue oiv)
      {
         MibNode parent;

         // Add parent node to tree (if needed)
         if (HasParent(oiv))
         {
            parent = AddToTree(oiv.Parent);
         }
         else
         {
            parent = rootNode;
         }

         // Check if node already added
         foreach (var node in parent.Nodes)
         {
            MibNode mibNode = node as MibNode;
            if (mibNode.Value.Equals(oiv))
            {
               return mibNode;
            }
         }

         // Create new node
         string name = oiv.Name + " (" + oiv.Value + ")";
         MibNode newNode = new MibNode(name, oiv);
         parent.Nodes.Add(newNode);
         nodes.Add(oiv.Symbol, newNode);
         return newNode;
      }

      private bool HasParent(ObjectIdentifierValue oiv)
      {
         ObjectIdentifierValue parent = oiv.Parent;

         return oiv.Symbol != null
             && oiv.Symbol.Mib != null
             && parent != null
             && parent.Symbol != null
             && parent.Symbol.Mib != null
             && parent.Symbol.Mib.Equals(oiv.Symbol.Mib);
      }
   }
}
