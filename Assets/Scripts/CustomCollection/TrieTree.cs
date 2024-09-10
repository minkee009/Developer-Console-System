using System.Collections.Generic;
using System;

namespace SPTr.CustomCollections
{
    public class TrieNode
    {
        public char Key { get; private set; }
        public Dictionary<char, TrieNode> Children { get; private set; }
        public string Data { get; private set; }

        public TrieNode(char key)
        {
            Key = key;
            Children = new Dictionary<char, TrieNode>();
            Data = null;
        }

        public void SetData(string data) => Data = data;
    }

    public class TrieTree
    {
        private TrieNode _root;
        private Stack<TrieNode> _searchStack;

        public TrieTree()
        {
            _root = new TrieNode(' ');
            _searchStack = new Stack<TrieNode>();
        }

        public void Add(string word)
        {
            var node = _root;
            foreach (var c in word)
            {
                if (!node.Children.ContainsKey(c))
                {
                    node.Children[c] = new TrieNode(c);
                }

                node = node.Children[c];
            }

            node.SetData(word);
        }

        public bool Remove(string word)
        {
            var node = _root;
            var parent = _root;
            foreach (var c in word)
            {
                if (!node.Children.ContainsKey(c))
                {
                    Console.WriteLine("삭제하려는 요소가 없습니다.");
                    return false;
                }

                parent = node;
                node = node.Children[c];
            }

            if (node.Data == null)
            {
                Console.WriteLine("삭제하려는 요소가 없습니다.");
                return false;
            }
            else if (node.Children.Count == 0)
            {
                parent.Children.Remove(node.Key);
            }
            else
            {
                node.SetData(null);
            }

            return true;
        }

        public List<string> GetSuggestions(string prefix)
        {
            var node = _root;
            foreach (var c in prefix)
            {
                if (!node.Children.ContainsKey(c))
                {
                    return null;
                }

                node = node.Children[c];
            }

            return GetSuggestions(node);
        }

        public List<string> GetSuggestions(TrieNode node)
        {
            var suggestions = new List<string>();
            var current = node;

            _searchStack.Push(current);

            while (_searchStack.Count > 0)
            {
                current = _searchStack.Pop();
                if (current.Data != null)
                    suggestions.Add(current.Data);

                if (current.Children.Count > 0)
                    foreach (var child in current.Children.Values)
                        _searchStack.Push(child);
            }

            _searchStack.Clear();

            return suggestions;
        }

        public bool TryLoadSuggestions(ref List<string> suggestions, string prefix)
        {
            if (suggestions == null)
                return false;

            var current = _root;
            foreach (var c in prefix)
            {
                if (!current.Children.ContainsKey(c))
                {
                    return false;
                }

                current = current.Children[c];
            }

            suggestions.Clear();
            _searchStack.Push(current);

            while (_searchStack.Count > 0)
            {
                current = _searchStack.Pop();
                if (current.Data != null)
                    suggestions.Add(current.Data);

                if (current.Children.Count > 0)
                    foreach (var child in current.Children.Values)
                        _searchStack.Push(child);
            }
            _searchStack.Clear();

            return suggestions.Count > 0 ? true : false;
        }

        public bool TryLoadSuggestionNodes(ref List<TrieNode> nodes, string prefix)
        {
            var current = _root;
            foreach (var c in prefix)
            {
                if (!current.Children.ContainsKey(c))
                {
                    return false;
                }

                current = current.Children[c];
            }

            nodes.Clear();
            _searchStack.Push(current);

            while (_searchStack.Count > 0)
            {
                current = _searchStack.Pop();
                if (current.Data != null)
                    nodes.Add(current);

                if (current.Children.Count > 0)
                    foreach (var child in current.Children.Values)
                        _searchStack.Push(child);
            }
            _searchStack.Clear();

            return nodes.Count > 0 ? true : false;
        }
    }
}