using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Editor.Controls
{
    internal class PathDisplay
    {
        public string DisplayText { get; set; }
        public string Path { get; set; }
        
        public PathDisplay(string d, string p)
        {
            DisplayText = d;
            Path = p;
        }
    
        public override string ToString()
        {
            return DisplayText;
        }
    }

    internal class TreeViewController
    {
        public string[] GetRelativePaths(string[] sArr, string relativeTo)
        {
            string[] Arr = new string[sArr.Length];

            for (int i = 0; i < sArr.Length; i++)
                Arr[i] = Path.GetRelativePath(relativeTo, sArr[i]);

            return Arr;
        }

        public void PopulateTreeView(TreeView treeView, string[] paths, char pathSeparator)
        {
            TreeViewNode lastNode = null;
            string subPathAgg;
            long count = 0;

            foreach (string path in paths)
            {
                subPathAgg = string.Empty;
                foreach (string subPath in path.Split(pathSeparator))
                {
                    subPathAgg += subPath + pathSeparator;
                    var displayModel = new PathDisplay(subPath, subPathAgg);
                    TreeViewNode[] nodes = GetSameNodes(treeView.RootNodes, subPathAgg).ToArray();
                    if (nodes.Length == 0)
                    {
                        if (lastNode == null)
                        {
                            lastNode = new TreeViewNode() { Content = displayModel, IsExpanded = true };
                            treeView.RootNodes.Add(lastNode);
                        }
                        else
                        {
                            var node = new TreeViewNode() { Content = displayModel };
                            lastNode.Children.Add(node);
                            lastNode = node;
                        }
                        count++;
                    }
                    else
                    {
                        lastNode = nodes[0];
                    }
                }
                lastNode = null;
            }
        }

        private IEnumerable<TreeViewNode> GetSameNodes(IList<TreeViewNode> _nodes, string _path)
        {
            foreach (var node in _nodes)
            {
                var content = node.Content as PathDisplay;

                if (content?.Path == _path)
                    yield return node;
                else
                {
                    if (node.Children.Count > 0)
                        foreach (var item in GetSameNodes(node.Children, _path))
                            yield return item;
                }
            }
        }

    }
}
