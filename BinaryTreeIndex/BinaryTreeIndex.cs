/*
* sones GraphDB - Community Edition - http://www.sones.com
* Copyright (C) 2007-2011 sones GmbH
*
* This file is part of sones GraphDB Community Edition.
*
* sones GraphDB is free software: you can redistribute it and/or modify
* it under the terms of the GNU Affero General Public License as published by
* the Free Software Foundation, version 3 of the License.
* 
* sones GraphDB is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU Affero General Public License for more details.
*
* You should have received a copy of the GNU Affero General Public License
* along with sones GraphDB. If not, see <http://www.gnu.org/licenses/>.
* 
*/

using System;
using System.Linq;
using System.Collections.Generic;
using sones.Library.VersionedPluginManager;
using sones.Plugins.Index.Abstract;
using sones.Plugins.Index.ErrorHandling;
using sones.Plugins.Index.Helper;

namespace s1ck.GraphDB.Plugins.Index.BinaryTree
{
    /// <summary>
    /// This class represents a sample index for the sones GraphDB.
    /// 
    /// It is part of the tutorial on "Custom Indices" which can be
    /// found here:
    /// 
    /// http://developers.sones.de/wiki/doku.php?id=documentation:plugins:database:indices
    /// 
    /// The implementation is a binary search tree as it is described here:
    ///
    /// http://en.wikipedia.org/wiki/Binary_tree
    /// 
    /// The index can be used as 1:1 or 1:n mapping between keys
    /// and associated values. It is not designed for production use,
    /// there is no concerning about parallel access or persistence.
    /// 
    /// Feel free to use this index as a starting point for own
    /// implementations.
    /// 
    /// Author: Martin "s1ck" Junghanns (martin.junghanns@sones.com)
    /// </summary>
    public class BinaryTreeIndex : ASonesIndex, IPluginable
    {
        #region Inner class BinaryTreeNode

        /// <summary>
        /// This inner class represents a node in the binary tree.
        /// Each node has a left and a right child node, a search key
        /// and an associated value.
        /// 
        /// Node:
        ///         Key ---> Set of values
        ///        /   \
        ///       /     \
        /// Left Node  Right Node
        /// </summary>
        class BinaryTreeNode
        {
            /// <summary>
            /// Refers to the left child node
            /// </summary>
            public BinaryTreeNode Left;
            /// <summary>
            /// Refers to the right child node
            /// </summary>
            public BinaryTreeNode Right;
            /// <summary>
            /// The indexed search key for that node
            /// </summary>
            public IComparable Key;
            /// <summary>
            /// The associated values for the search key
            /// </summary>
            public HashSet<Int64> Values;
            /// <summary>
            /// Creates a new instance of a tree node
            /// </summary>
            /// <param name="myKey">Search key</param>
            /// <param name="myValues">Associated values</param>
            public BinaryTreeNode(IComparable myKey, HashSet<Int64> myValues)
            {
                Key     = myKey;
                Values  = myValues;
                Left    = null;
                Right   = null;
            }
        }

        #endregion

        #region Private Members

        /// <summary>
        /// Reference to the root node
        /// </summary>
        private BinaryTreeNode _Root;
        /// <summary>
        /// Number of keys stored in the tree
        /// </summary>
        private Int64 _KeyCount;
        /// <summary>
        /// Number of values stored in the tree
        /// </summary>
        private Int64 _ValueCount;

        #endregion

        #region Constructors

        /// <summary>
        /// Empty constructor. This one is important for the sones PluginManager.
        /// </summary>
        public BinaryTreeIndex()
        {
            _KeyCount = 0L;
            _ValueCount = 0L;
        }

        /// <summary>
        /// Initializes the binary tree index and assigns
        /// a list of propertyIDs to the internal member.
        /// </summary>
        /// <param name="myPropertyIDs">A list of indexed propertyIDs</param>
        public BinaryTreeIndex(IList<Int64> myPropertyIDs)
            : this()
        {
            _PropertyIDs = myPropertyIDs;
        }

        #endregion

        #region IPluginable Members

        /// <summary>
        /// Name of the plugin
        /// </summary>
        public string PluginName
        {
            get { return "sones.binarytreeindex"; }
        }

        /// <summary>
        /// Short name of the plugin
        /// </summary>
        public string PluginShortName
        {
            get { return "sones.bintreeidx"; }
        }

        /// <summary>
        /// Returns plugin parameters which can be set for this plugin.
        /// </summary>
        public PluginParameters<Type> SetableParameters
        {
            get
            {
                return new PluginParameters<Type>
                {
                };
            }
        }

        /// <summary>
        /// This method is called by the plugin manager when the plugin is loaded.
        /// 
        /// It loads the indexed propertyIDs out of the parameter dictionary and
        /// initializes the index with these propertyIDs
        /// </summary>
        /// <param name="UniqueString"></param>
        /// <param name="myParameters"></param>
        /// <returns>An instance of the binary tree index</returns>
        public IPluginable InitializePlugin(string UniqueString, Dictionary<string, object> myParameters = null)
        {
            if (myParameters != null && myParameters.ContainsKey(IndexConstants.PROPERTY_IDS_OPTIONS_KEY))
            {
                return new BinaryTreeIndex((IList<Int64>)myParameters[IndexConstants.PROPERTY_IDS_OPTIONS_KEY]);
            }
            else
            {
                throw new ArgumentException("{0} were not set for initializing binary tree index",
                    IndexConstants.PROPERTY_IDS_OPTIONS_KEY);
            }
        }

        /// <summary>
        /// Releases all resources which are hold by the index.
        /// </summary>
        public void Dispose()
        {

        }

        #endregion

        #region ISonesIndex Members

        /// <summary>
        /// The name of the index as it is used by the GraphQL (camel case).
        /// </summary>
        public override string IndexName
        {
            get { return "binarytree"; }
        }

        /// <summary>
        /// Returns the number of keys stored in the index.
        /// </summary>
        /// <returns>Number of stored keys</returns>
        public override long KeyCount()
        {
            return _KeyCount;
        }

        /// <summary>
        /// Returns the number of values stored in the index.
        /// </summary>
        /// <returns>Number of stored values</returns>
        public override long ValueCount()
        {
            return _ValueCount;
        }

        /// <summary>
        /// Returns sorted keys stored in the tree.
        /// </summary>
        /// <returns>An ordered list of index keys</returns>
        public override IEnumerable<IComparable> Keys()
        {
            var result = new List<IComparable>();

            if (_Root != null)
            {
                TraverseInOrder(_Root, ref result);
            }

            return result;
        }

        /// <summary>
        /// Returns the type of the indexed keys.
        /// 
        /// Note: If the index is empty the typeof(IComparable) will be returned.
        /// </summary>
        /// <returns>The type of the indexed keys or typeof(IComparable) if the index is empty.</returns>
        public override Type GetKeyType()
        {
            return (_Root != null) ? _Root.Values.First().GetType() : typeof(IComparable);
        }

        /// <summary>
        /// Adds a search key and its associated value to the index.
        /// 
        /// If IndexAddStrategy is REPLACE, an existing values associated with the key will be replaced by the new value.
        /// If IndexAddStrategy is MERGE, the new value will be added to the existing set.
        /// If IndexAddStrategy is UNIQUE, an exception will be thrown if the key already exists.
        /// </summary>
        /// <param name="myKey">Search key</param>
        /// <param name="myVertexID">Associated value</param>
        /// <param name="myIndexAddStrategy">Define what happens, if the key already exists.</param>
        public override void Add(IComparable myKey, long myVertexID,
            IndexAddStrategy myIndexAddStrategy = IndexAddStrategy.MERGE)
        {
            if (myKey == null) // uh, tree does not support null keys
            {
                throw new NullKeysNotSupportedException("binarytree index does not support null as key");
            }

            if (_Root == null) // nothing inserted yet -> create a new root
            {
                _Root = new BinaryTreeNode(myKey, new HashSet<Int64>() { myVertexID });
                _KeyCount++;
                _ValueCount++;
            }
            else
            {
                // insert into index
                Add(_Root, myKey, myVertexID, myIndexAddStrategy);
            }
        }

        /// <summary>
        /// Writes the associated value to the out param if the key exists.
        /// </summary>
        /// <param name="myKey">Search key</param>
        /// <param name="myVertexIDs">Stores the values (if any exist)</param>
        /// <returns>True, if the key has been found.</returns>
        public override bool TryGetValues(IComparable myKey, out IEnumerable<long> myVertexIDs)
        {
            BinaryTreeNode res;
            if ((res = Find(_Root, myKey)) != null)
            {
                myVertexIDs = res.Values;
                return true;
            }
            else
            {
                myVertexIDs = null;
                return false;
            }
        }

        /// <summary>
        /// Returns the values associated with the given key or throws
        /// an Exception if the key does not exist.
        /// </summary>
        /// <param name="myKey">Search key</param>
        /// <returns>The associated values</returns>
        public override IEnumerable<long> this[IComparable myKey]
        {
            get
            {
                BinaryTreeNode res;
                if ((res = Find(_Root, myKey)) != null)
                {
                    return res.Values;
                }
                else
                {
                    throw new IndexKeyNotFoundException(
                        String.Format("index key {0} was not found in binary tree", myKey));
                }
            }
        }

        /// <summary>
        /// Checks if a given key exists in the index.
        /// </summary>
        /// <param name="myKey">Search key</param>
        /// <returns>True, if the key exists in the index.</returns>
        public override bool ContainsKey(IComparable myKey)
        {
            if (myKey == null)
            {
                throw new NullKeysNotSupportedException("binarytree index does not support null as key");
            }

            return Find(_Root, myKey) != null;
        }

        /// <summary>
        /// Removes the given key and all associated values from the index.
        /// </summary>
        /// <param name="myKey">Search key</param>
        /// <returns>True, if the key has been found and removed</returns>
        public override bool Remove(IComparable myKey)
        {
            if (myKey == null)
            {
                throw new NullKeysNotSupportedException("binarytree index does not support null as key");
            }
            bool removed = false;

            _Root = Remove(_Root, myKey, ref removed);

            return removed;
        }

        /// <summary>
        /// Removes multiple keys from the index
        /// </summary>
        /// <param name="myKeys">search keys to remove</param>
        public override void RemoveRange(IEnumerable<IComparable> myKeys)
        {
            foreach (var key in myKeys)
            {
                Remove(key);
            }
        }

        /// <summary>
        /// Checks if a given value is associated with a given key and 
        /// if yes, the key will be deleted from the index.
        /// 
        /// TODO: this method checks first if key and value exists 
        /// and then removes it. maybe this can be done in one step by
        /// duplicating the remove method.
        /// </summary>
        /// <param name="myKey">search key</param>
        /// <param name="myValue">associated value</param>
        /// <returns>True, if key and value were deleted from the index</returns>
        public override bool TryRemoveValue(IComparable myKey, long myValue)
        {
            if (myKey == null)
            {
                throw new NullKeysNotSupportedException("binarytree index does not support null as key");
            }

            BinaryTreeNode res;

            if ((res = Find(_Root, myKey)) != null && res.Values.Contains(myValue))
            {
                res.Values.Remove(myValue);
                _ValueCount--;

                return (res.Values.Count == 0) ? Remove(myKey) : true;
            }

            return false;
        }

        /// <summary>
        /// Currently nothing happens here.
        /// 
        /// TODO: maybe some rebalancing
        /// </summary>
        public override void Optimize()
        {
        }

        /// <summary>
        /// Resets the index by setting the root node to null and letting
        /// the GC do the work.
        /// </summary>
        public override void Clear()
        {
            _Root = null;
            _KeyCount = 0L;
            _ValueCount = 0L;
        }

        /// <summary>
        /// This index does not support null as key.
        /// </summary>
        public override bool SupportsNullableKeys
        {
            get { return false; }
        }

        #endregion

        #region Private Members

        /// <summary>
        /// Adds a given search key and the associated value to the index.
        /// </summary>
        /// <param name="myTreeNode">Starting tree node</param>
        /// <param name="myKey">Search key to be inserted</param>
        /// <param name="myValue">Associated value</param>
        /// <param name="myIndexAddStrategy">Define what happens if the key exists</param>
        private void Add(BinaryTreeNode myTreeNode,
            IComparable myKey,
            Int64 myValue,
            IndexAddStrategy myIndexAddStrategy)
        {
            if (myKey.CompareTo(myTreeNode.Key) == 0) // key already exists
            {
                switch (myIndexAddStrategy)
                {
                    case IndexAddStrategy.REPLACE:
                        // replace the value
                        _ValueCount -= myTreeNode.Values.Count;
                        myTreeNode.Values = new HashSet<Int64>() { myValue };
                        _ValueCount++;
                        break;
                    case IndexAddStrategy.MERGE:
                        // add the new value to the existing ones
                        if (myTreeNode.Values.Add(myValue))
                        {
                            _ValueCount++;
                        }
                        break;
                    default: // UNIQUE
                        throw new IndexKeyExistsException(
                            String.Format("key {0} already exists in binary tree", myKey));
                }
            }
            else
            {
                if (myKey.CompareTo(myTreeNode.Key) == -1) // new key is lower
                {
                    if (myTreeNode.Left == null)
                    {
                        myTreeNode.Left = new BinaryTreeNode(myKey, new HashSet<Int64>() { myValue });
                        _ValueCount++;
                        _KeyCount++;
                    }
                    else
                    {
                        Add(myTreeNode.Left, myKey, myValue, myIndexAddStrategy);
                    }
                }
                else // new key is greater
                {
                    if (myTreeNode.Right == null)
                    {
                        myTreeNode.Right = new BinaryTreeNode(myKey, new HashSet<Int64>() { myValue });
                        _ValueCount++;
                        _KeyCount++;
                    }
                    else
                    {
                        Add(myTreeNode.Right, myKey, myValue, myIndexAddStrategy);
                    }
                }
            }
        }

        /// <summary>
        /// Checks if a given key exists in the tree
        /// </summary>
        /// <param name="myTreeNode">Starting Node</param>
        /// <param name="myKey">Search key</param>
        /// <returns>The tree node if the key exists or null if it does not.</returns>
        private BinaryTreeNode Find(BinaryTreeNode myTreeNode,
            IComparable myKey)
        {
            if (myTreeNode == null) // no way
            {
                return null;
            }
            else if (myKey.CompareTo(myTreeNode.Key) == -1) // lower
            {
                return Find(myTreeNode.Left, myKey);
            }
            else if (myKey.CompareTo(myTreeNode.Key) == 1) // greater
            {
                return Find(myTreeNode.Right, myKey);
            }
            else // match
            {
                return myTreeNode;
            }

        }

        /// <summary>
        /// Removes a given key from the tree, if it exists.
        /// Returns the new (or existing) root node.
        /// </summary>
        /// <param name="myTreeNode">Starting tree node</param>
        /// <param name="myKey">Search key</param>
        /// <returns>Root node as a result of back propagation</returns>
        private BinaryTreeNode Remove(BinaryTreeNode myTreeNode,
            IComparable myKey,
            ref bool myRemoved)
        {
            if (myTreeNode == null)
            {
                myRemoved = false;
                return null;
            }
            else if (myKey.CompareTo(myTreeNode.Key) == -1) // lower
            {
                myTreeNode.Left = Remove(myTreeNode.Left, myKey, ref myRemoved);
            }
            else if (myKey.CompareTo(myTreeNode.Key) == 1) // greater
            {
                myTreeNode.Right = Remove(myTreeNode.Right, myKey, ref myRemoved);
            }
            else if (myTreeNode.Left != null && myTreeNode.Right != null)
            {
                // get minimum tree node in right subtree
                var minRightTree = FindMinNodeIn(myTreeNode.Right);
                /* 
                 * replace key and value of the current node with key and value 
                 * of the minimum node in the right subtree
                 */
                _KeyCount--; // update key count
                myTreeNode.Key = minRightTree.Key;

                _ValueCount -= myTreeNode.Values.Count; // update value count
                myTreeNode.Values = minRightTree.Values;

                // remove the minimum tree node in the right subtree
                myTreeNode.Right = RemoveMinIn(myTreeNode.Right);

                // node has been removed
                myRemoved = true;

            }
            else
            {
                /*
                 * if there is a left child, we replace the current node with that one
                 * and of not, we replace the current with the right child node
                 */
                _KeyCount--; // update key count
                _ValueCount -= myTreeNode.Values.Count; // update value count

                myTreeNode = (myTreeNode.Left != null) ? myTreeNode.Left : myTreeNode.Right;
                
                // node has been removed
                myRemoved = true;
            }

            return myTreeNode;
        }

        /// <summary>
        /// Returns the minimum BinaryTreeNode in a subtree. The minimum node
        /// is the most left node in the subtree.
        /// </summary>
        /// <param name="myTreeNode">Starting node</param>
        /// <returns>The smallest in the given subtree</returns>
        private BinaryTreeNode FindMinNodeIn(BinaryTreeNode myTreeNode)
        {
            if (myTreeNode.Left != null)
            {
                // go deeper in the left subtree
                return FindMinNodeIn(myTreeNode.Left);
            }
            else
            {
                // got the minimum
                return myTreeNode;
            }
        }

        /// <summary>
        /// Removes the minium BinaryTreeNode in a tree. The minimum node
        /// is the most left node in the subtree.
        /// </summary>
        /// <param name="myTreeNode">Starting node</param>
        /// <returns>The new left child node, if treenode has been deleted.</returns>
        private BinaryTreeNode RemoveMinIn(BinaryTreeNode myTreeNode)
        {
            if (myTreeNode != null)
            {
                if (myTreeNode.Left != null)
                {
                    myTreeNode.Left = RemoveMinIn(myTreeNode.Left);
                    return myTreeNode;
                }
                else
                {
                    return myTreeNode.Right;
                }
            }
            return null;
        }

        /// <summary>
        /// Traverses the tree in-order: Left-Root-Right
        /// 
        /// This means the keys are returned sorted.
        /// </summary>
        /// <param name="myTreeNode">Starting tree node</param>
        /// <param name="myResult">stores the keys visited during traversal</param>
        private void TraverseInOrder(BinaryTreeNode myTreeNode, ref List<IComparable> myResult)
        {
            if (myTreeNode.Left != null)
            {
                TraverseInOrder(myTreeNode.Left, ref myResult);
            }
            myResult.Add(myTreeNode.Key);

            if (myTreeNode.Right != null)
            {
                TraverseInOrder(myTreeNode.Right, ref myResult);
            }
        }

        #endregion

    }
}
