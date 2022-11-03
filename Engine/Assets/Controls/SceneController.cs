using System.Collections.Generic;

namespace Editor.Controls
{
    internal class SceneController
    {
        public List<TreeEntry> Hierarchy = new List<TreeEntry>();

        public string[] ToStringArray()
        {
            string[] s = new string[Hierarchy.Count];

            for (int i = 0; i < Hierarchy.Count; i++)
                s[i] = GetParents(Hierarchy[i], Hierarchy[i].Name, '/');

            return s;
        }

        public TreeEntry GetParent(TreeEntry node)
        {
            if (node.IDparent != null)
                foreach (var item in Hierarchy)
                    if (item.ID == node.IDparent.Value)
                        return item;
            return null;
        }

        public TreeEntry[] GetChildren(TreeEntry node)
        {
            List<TreeEntry> list = new List<TreeEntry>();
            foreach (var item in Hierarchy)
                if (item.IDparent != null)
                    if (item.IDparent.Value == node.ID)
                        list.Add(item);
            return list.ToArray();
        }

        private string GetParents(TreeEntry current, string path, char pathSeperator)
        {
            if (current.IDparent != null)
                foreach (var item in Hierarchy)
                    if (item.ID == current.IDparent)
                        path = GetParents(
                            item,
                            item.Name + pathSeperator + path,
                            pathSeperator);

            return path;
        }
    }
}
