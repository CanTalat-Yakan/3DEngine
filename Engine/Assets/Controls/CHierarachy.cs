using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
//using Engine.Utilities;

namespace Editor.Controls
{
    class TreeEntry
    {
        public Guid ID;
        public Guid? IDparent;
        public string Name;
        //public CObject Object;
        public TreeViewNode Node;
    }
    class CScene
    {
        public List<TreeEntry> m_Hierarchy = new List<TreeEntry>();

        public string[] ToStringArray()
        {
            string[] s = new string[m_Hierarchy.Count];

            for (int i = 0; i < m_Hierarchy.Count; i++)
                s[i] = GetParents(m_Hierarchy[i], m_Hierarchy[i].Name, '/');

            return s;
        }

        public TreeEntry GetParent(TreeEntry _node)
        {
            if (_node.IDparent != null)
                foreach (var item in m_Hierarchy)
                    if (item.ID == _node.IDparent.Value)
                        return item;
            return null;
        }
        public TreeEntry[] GetChildren(TreeEntry _node)
        {
            List<TreeEntry> list = new List<TreeEntry>();
            foreach (var item in m_Hierarchy)
                if (item.IDparent != null)
                    if (item.IDparent.Value == _node.ID)
                        list.Add(item);
            return list.ToArray();
        }
        string GetParents(TreeEntry _current, string _path, char _pathSeperator)
        {
            if (_current.IDparent != null)
                foreach (var item in m_Hierarchy)
                    if (item.ID == _current.IDparent)
                        _path = GetParents(
                            item,
                            item.Name + _pathSeperator + _path,
                            _pathSeperator);

            return _path;
        }
    }
    class CHierarchy
    {
        CTreeView m_control = new CTreeView();

        internal TreeView m_Tree;
        internal CScene m_Scene;

        public CHierarchy(TreeView _tree, CScene _scene)
        {
            m_Tree = _tree;
            m_Scene = _scene;

            Initialize();
        }

        void Initialize()
        {
            m_control.PopulateTreeView(m_Tree, m_Scene.ToStringArray(), '/');
        }
    }
}
