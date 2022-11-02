using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using Engine.Utilities;

namespace Editor.Controls
{
    class TreeEntry
    {
        public Guid ID;
        public Guid? IDparent;
        public string Name;

        public Entity Entity;
        public TreeViewNode Node;
    }
    class SceneController
    {
        public List<TreeEntry> m_Hierarchy = new List<TreeEntry>();

        public string[] ToStringArray()
        {
            string[] s = new string[m_Hierarchy.Count];

            for (int i = 0; i < m_Hierarchy.Count; i++)
                s[i] = GetParents(m_Hierarchy[i], m_Hierarchy[i].Name, '/');

            return s;
        }

        public TreeEntry GetParent(TreeEntry node)
        {
            if (node.IDparent != null)
                foreach (var item in m_Hierarchy)
                    if (item.ID == node.IDparent.Value)
                        return item;
            return null;
        }

        public TreeEntry[] GetChildren(TreeEntry node)
        {
            List<TreeEntry> list = new List<TreeEntry>();
            foreach (var item in m_Hierarchy)
                if (item.IDparent != null)
                    if (item.IDparent.Value == node.ID)
                        list.Add(item);
            return list.ToArray();
        }

        string GetParents(TreeEntry current, string path, char pathSeperator)
        {
            if (current.IDparent != null)
                foreach (var item in m_Hierarchy)
                    if (item.ID == current.IDparent)
                        path = GetParents(
                            item,
                            item.Name + pathSeperator + path,
                            pathSeperator);

            return path;
        }
    }
    class HierarachyController
    {
        TreeViewController m_control = new TreeViewController();

        internal TreeView m_Tree;
        internal SceneController m_Scene;

        public HierarachyController(TreeView _tree, SceneController _scene)
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
