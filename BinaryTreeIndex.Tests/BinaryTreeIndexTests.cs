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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using sones.GraphDB;
using sones.GraphDB.Manager;
using sones.GraphDB.Request;
using sones.GraphDB.TypeSystem;
using sones.GraphFS.Element.Vertex;
using sones.Library.Commons.Security;
using sones.Library.PropertyHyperGraph;
using sones.Plugins.Index.ErrorHandling;
using sones.Plugins.Index.Helper;

namespace s1ck.GraphDB.Plugins.Index.BinaryTree.Tests
{
    [TestFixture]
    public class BinaryTreeIndexTests
    {
        #region Tests

        #region KeyCount / ValueCount Tests

        /// <summary>
        /// This test checks if the number of added keys is equal to key count.
        /// </summary>
        [TestCase]
        public void KeyCountTest()
        {
            #region data

            var idx = new BinaryTreeIndex();

            var kvp = new List<KeyValuePair<IComparable, Int64>>()
            {
                new KeyValuePair<IComparable, Int64>(1, 1),
                new KeyValuePair<IComparable, Int64>(1, 2),
                new KeyValuePair<IComparable, Int64>(2, 3),
                new KeyValuePair<IComparable, Int64>(2, 4),
                new KeyValuePair<IComparable, Int64>(2, 5),
            };

            idx.AddRange(kvp, IndexAddStrategy.MERGE);

            #endregion

            #region test

            Assert.That(idx.KeyCount(), Is.EqualTo(2));

            #endregion

        }

        /// <summary>
        /// This test checks if the number of added values is equal to value count.
        /// </summary>
        [TestCase]
        public void ValueCountTest()
        {
            #region data

            var idx = new BinaryTreeIndex();

            var kvp = new List<KeyValuePair<IComparable, Int64>>()
            {
                new KeyValuePair<IComparable, Int64>(1, 1),
                new KeyValuePair<IComparable, Int64>(1, 2),
                new KeyValuePair<IComparable, Int64>(2, 3),
                new KeyValuePair<IComparable, Int64>(2, 4),
                new KeyValuePair<IComparable, Int64>(2, 5),
            };

            idx.AddRange(kvp, IndexAddStrategy.MERGE);

            #endregion

            #region test

            Assert.That(idx.ValueCount(), Is.EqualTo(5));

            #endregion
        }

        #endregion

        #region Keys Tests

        /// <summary>
        /// This test inserts a set of keys into the index and checks if they were stored correctly.
        /// </summary>
        [TestCase]
        public virtual void Keys()
        {
            #region data

            var idx = new BinaryTreeIndex();

            var n = 10;
            var vertices = CreateKeyValuePairs(n);

            #endregion

            #region test

            idx.AddRange(vertices);

            var keys = idx.Keys();

            Assert.That(keys.LongCount(), Is.EqualTo(n));

            for (long i = 0; i < n; i++)
            {
                CollectionAssert.Contains(keys, i);
            }

            #endregion
        }

        #endregion

        #region Add Tests

        /// <summary>
        /// This test adds a vertex to the index and checks if it was correctly added.
        /// </summary>
        [TestCase]
        public virtual void Add_InsertVertex()
        {
            #region data

            var idx = new BinaryTreeIndex();

            var vertexID = 1L;
            var propertyID = 1L;
            var propertyValue = 10;
            // set propertyID for index
            idx.Init(new List<Int64>() { propertyID });

            // create a vertex
            var v = new InMemoryVertex(vertexID,
                1L,
                1L,
                null,
                null,
                null,
                "dummy",
                DateTime.Now.Ticks,
                DateTime.Now.Ticks,
                new Dictionary<long, IComparable>() { { propertyID, propertyValue } }, // structured properties
                null);

            #endregion

            #region test

            // add
            idx.Add(v);

            Assert.AreEqual(1, idx.KeyCount());
            Assert.AreEqual(1, idx.ValueCount());

            Assert.IsTrue(idx[propertyValue].Contains(vertexID));

            #endregion
        }

        /// <summary>
        /// This test adds a vertex to the index which doesn't have the indexed property.
        /// This causes no error, but the vertex won't be added to the index.
        /// </summary>
        [TestCase]
        public virtual void Add_InsertVertex_Fails()
        {
            #region data

            var idx = new BinaryTreeIndex();

            var vertexID = 1L;
            var propertyID = 1L;
            var fake_propertyID = 2L;
            var propertyValue = 10;
            // set propertyID for index
            idx.Init(new List<Int64>() { propertyID });

            // create a vertex
            var v = new InMemoryVertex(vertexID,
                1L,
                1L,
                null,
                null,
                null,
                "dummy",
                DateTime.Now.Ticks,
                DateTime.Now.Ticks,
                new Dictionary<long, IComparable>() { { fake_propertyID, propertyValue } }, // structured properties
                null);

            #endregion

            #region test

            // this won't add the vertex because it doesn't have the indexed property
            idx.Add(v);
            Assert.That(idx.KeyCount(), Is.EqualTo(0L), "vertex has been added by mistake");
            Assert.That(idx.ValueCount(), Is.EqualTo(0L), "vertex has been added by mistake");

            #endregion
        }

        /// <summary>
        /// This tests adds an already existing key and an associated value to the index.
        /// This should throw an <code>IndexKeyExistsException</code>.
        /// </summary>
        [TestCase]
        public virtual void Add_InsertKeyValue_IndexAddStrategy_Unique()
        {
            #region data

            var idx = new BinaryTreeIndex();

            var key_1 = 1;
            var val_1 = 1;

            #endregion

            #region test

            idx.Add(key_1, val_1);

            // inserting existing key throws exception when strategy is unique
            Assert.Throws(typeof(IndexKeyExistsException), () =>
            {
                idx.Add(key_1, val_1, IndexAddStrategy.UNIQUE);
            });

            #endregion
        }

        /// <summary>
        /// This test adds an already existing key and an associated value to the index.
        /// By using IndexAddStrategy.Merge two values should be associated to the key.
        /// </summary>
        [TestCase]
        public virtual void Add_InsertKeyValue_IndexAddStrategy_Merge()
        {
            #region data

            var idx = new BinaryTreeIndex();

            var key_1 = 1;

            var val_1 = 1;
            var val_2 = 2;

            #endregion

            #region test

            // insert first key value
            idx.Add(key_1, val_1, IndexAddStrategy.MERGE);

            // index must contain key and one value
            Assert.IsTrue(idx[key_1].Contains(val_1));
            Assert.AreEqual(1, idx[key_1].Count());

            // merge a second value with the currently stored one
            idx.Add(key_1, val_2, IndexAddStrategy.MERGE);

            // index now contains two values for the key
            Assert.IsTrue(idx[1].Contains(val_1));
            Assert.IsTrue(idx[1].Contains(val_2));
            Assert.AreEqual(2, idx[1].Count());

            idx.Clear();

            #endregion
        }

        /// <summary>
        /// This test adds an already existing key and an associated value to the index.
        /// By using IndexAddStrategy.Replace the new value should be associated to the key.
        /// </summary>
        [TestCase]
        public virtual void Add_InsertKeyValue_IndexAddStrategy_Replace()
        {
            #region data

            var idx = new BinaryTreeIndex();

            var key_1 = 1;
            var val_1 = 1;
            var val_2 = 2;

            #endregion

            #region test

            // insert first key value
            idx.Add(key_1, val_1, IndexAddStrategy.MERGE);

            // index must contain key and one value
            Assert.IsTrue(idx[key_1].Contains(val_1));
            Assert.AreEqual(1, idx[key_1].Count());

            // idx value count
            Assert.AreEqual(1, idx.ValueCount());

            // replace first value by the second
            idx.Add(1, val_2, IndexAddStrategy.REPLACE);

            // index now contains the new value for the key
            Assert.IsTrue(idx[key_1].Contains(val_2));
            Assert.AreEqual(1, idx[key_1].Count());

            // idx value count
            Assert.AreEqual(1, idx.ValueCount());

            idx.Clear();

            #endregion
        }

        /// <summary>
        /// This test inserts a null-key to the index and checks if an exception is thrown
        /// or if is inserted correctly. An Exception will be thrown if the index doesn't support
        /// null-keys.
        /// </summary>
        [TestCase]
        public virtual void Add_InsertNull()
        {
            #region data

            var idx = new BinaryTreeIndex();

            var val_1 = 0;

            #endregion

            #region Test

            if (idx.SupportsNullableKeys)
            {
                idx.Add(null, val_1);
                Assert.That(idx.KeyCount(), Is.EqualTo(1));
                Assert.That(idx.ValueCount(), Is.EqualTo(1));
            }
            else
            {
                Assert.Throws(typeof(NullKeysNotSupportedException), () => idx.Add(null, val_1));
            }

            #endregion
        }

        /// <summary>
        /// This test inserts a list of vertices into the index and checks if they have
        /// been added correctly.
        /// </summary>
        [TestCase]
        public virtual void AddRange_InsertVertices()
        {
            #region data

            var idx = new BinaryTreeIndex();

            var n = 10;
            var propertyID = 1L;
            var vertices = CreateVertices(n, propertyID);

            idx.Init(new List<Int64>() { propertyID });

            #endregion

            #region Test

            idx.AddRange(vertices);

            // check counts
            Assert.That(idx.KeyCount(), Is.EqualTo(n));
            Assert.That(idx.KeyCount(), Is.EqualTo(n));

            // check propertyIDs
            for (long vID = 0; vID < n; vID++)
            {
                Assert.IsTrue(idx.ContainsKey(vID));
                Assert.IsTrue(idx[vID].Contains(vID));
                Assert.That(idx[vID].LongCount(), Is.EqualTo(1L));
            }

            #endregion
        }

        /// <summary>
        /// This test insert a list of key-value-pairs into the index and checks if they
        /// have been added correctly.
        /// </summary>
        [TestCase]
        public virtual void AddRange_InsertKeyValuePairs()
        {
            #region data

            var idx = new BinaryTreeIndex();

            var n = 10;
            var vertices = CreateKeyValuePairs(n);

            #endregion

            #region test

            idx.AddRange(vertices);

            // check counts
            Assert.That(idx.KeyCount(), Is.EqualTo(n));
            Assert.That(idx.KeyCount(), Is.EqualTo(n));

            // check propertyIDs
            for (long vID = 0; vID < n; vID++)
            {
                Assert.IsTrue(idx.ContainsKey(vID));
                Assert.IsTrue(idx[vID].Contains(vID));
                Assert.That(idx[vID].LongCount(), Is.EqualTo(1L));
            }

            #endregion
        }

        #endregion

        #region Init Tests

        /// <summary>
        /// This test inits an index with a list of propertyIDs to index.
        /// Via reflection, the stored list will be checked against the defined list.
        /// </summary>
        [TestCase]
        public virtual void Init_CheckPrivateMemberEquality()
        {
            #region data

            var idx = new BinaryTreeIndex();

            #endregion

            #region

            // init the index with some propertyIDs
            var definedPropertyIDs = new List<Int64>() { 0L, 1L };
            idx.Init(definedPropertyIDs);

            // use some reflection action to get the private member of the index instance
            var indexedPropertyIDs = (List<Int64>)idx.GetType().GetField("_PropertyIDs", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(idx);

            CollectionAssert.AreEqual(definedPropertyIDs, indexedPropertyIDs, "indexed propertyIDs do not match");

            #endregion
        }

        #endregion

        #region TryGetValues Tests

        /// <summary>
        /// This test checks if the TryGetValues method returns the correct values.
        /// </summary>
        [TestCase]
        public virtual void TryGetValues_ExistingKey()
        {
            #region data

            var idx = new BinaryTreeIndex();

            var key_1 = 10;
            var key_2 = 11;

            var val_1 = 2;
            var val_2 = 3;
            var val_3 = 5;

            #endregion

            #region test

            idx.Add(key_1, val_1);
            idx.Add(key_1, val_2, IndexAddStrategy.MERGE);

            idx.Add(key_2, val_3);

            Assert.AreEqual(2, idx.KeyCount(), "The number of keys is not correct.");
            Assert.AreEqual(3, idx.ValueCount(), "The number of values is not correct.");

            // TryGetValues test
            IEnumerable<Int64> result;
            Assert.IsTrue(idx.TryGetValues(key_1, out result));
            Assert.IsTrue(result.Contains(val_1));
            Assert.IsTrue(result.Contains(val_2));
            Assert.AreEqual(2, result.Count());

            Assert.IsTrue(idx.TryGetValues(key_2, out result));
            Assert.IsTrue(result.Contains(val_3));
            Assert.AreEqual(1, result.Count());

            #endregion
        }

        /// <summary>
        /// This test uses the TryGetValue Method to load a key, which doesn't exist
        /// in the index. This should return false and the out param is null because
        /// no extra object will be created in that case.
        /// </summary>
        [TestCase]
        public virtual void TryGetValues_NonExistingKey()
        {
            #region data

            var idx = new BinaryTreeIndex();

            var key_1 = 10;
            var key_2 = 11;
            var key_3 = 12;

            var val_1 = 2;
            var val_2 = 3;
            var val_3 = 5;

            #endregion

            #region test

            idx.Add(key_1, val_1);
            idx.Add(key_1, val_2, IndexAddStrategy.MERGE);

            idx.Add(key_2, val_3);

            Assert.AreEqual(2, idx.KeyCount(), "The number of keys is not correct.");
            Assert.AreEqual(3, idx.ValueCount(), "The number of values is not correct.");

            // TryGetValues with non existing key
            IEnumerable<Int64> result;
            Assert.IsFalse(idx.TryGetValues(key_3, out result));
            Assert.IsNull(result);

            #endregion
        }

        #endregion

        #region this[] Tests

        /// <summary>
        /// This test checks the this[] call to an index using an existing key.
        /// </summary>
        [TestCase]
        public virtual void this_ExistingKey()
        {
            #region data

            var idx = new BinaryTreeIndex();

            var key_1 = 1;
            var val_1 = 1;

            #endregion

            #region test

            idx.Add(key_1, val_1);

            Assert.IsNotNull(idx[key_1]);
            CollectionAssert.Contains(idx[key_1], val_1);
            Assert.AreEqual(1L, idx[key_1].LongCount());

            #endregion
        }

        /// <summary>
        /// This test checks the this[] call to an index using an non existing key.
        /// This should throw an <code>IndexKeyNotFoundException</code>.
        /// </summary>
        [TestCase]
        public virtual void this_NonExistingKey_ThrowsException()
        {
            #region data

            var idx = new BinaryTreeIndex();
            var key_1 = 1;

            #endregion

            #region test

            Assert.Throws(typeof(IndexKeyNotFoundException), () => idx[key_1].LongCount());

            #endregion
        }

        #endregion

        #region ContainsKey Tests

        /// <summary>
        /// This test inserts a key-value-pair and checks if the key exsists.
        /// Should return true.
        /// </summary>
        [TestCase]
        public virtual void ContainsKey_True()
        {
            #region data

            var idx = new BinaryTreeIndex();

            var key_1 = 1;
            var val_1 = 1;

            #endregion

            #region

            idx.Add(key_1, val_1);

            Assert.IsTrue(idx.ContainsKey(key_1));

            #endregion
        }

        /// <summary>
        /// This test if a not-added key exsists.
        /// Should return false
        /// </summary>
        [TestCase]
        public virtual void ContainsKey_False()
        {
            #region data

            var idx = new BinaryTreeIndex();

            var key_1 = 1;

            #endregion

            #region

            Assert.IsFalse(idx.ContainsKey(key_1));

            #endregion
        }

        #endregion

        #region Remove / TryRemoveValue Tests

        /// <summary>
        /// This test adds a key-value-pair and removed the key.
        /// Should return true.
        /// </summary>
        [TestCase]
        public virtual void Remove_True()
        {
            #region data

            var idx = new BinaryTreeIndex();

            var key_1 = 1L;
            var val_1 = 1L;

            #endregion

            #region test

            idx.Add(key_1, val_1);

            Assert.AreEqual(1L, idx.KeyCount());
            Assert.AreEqual(1L, idx.ValueCount());

            // remove the key
            Assert.IsTrue(idx.Remove(key_1));
            // and check the counters
            Assert.AreEqual(0L, idx.KeyCount());
            Assert.AreEqual(0L, idx.ValueCount());

            #endregion
        }

        /// <summary>
        /// This tests adds some key-value-pairs and tries to
        /// remove non existing keys from the index. This call 
        /// should return false.
        /// </summary>
        [TestCase]
        public virtual void Remove_False()
        {
            #region data

            var idx = new BinaryTreeIndex();

            var key_1 = 1L;
            var key_2 = 2L;
            var key_3 = 3L;

            var val_1 = 1L;
            var val_2 = 2L;

            #endregion

            #region test

            idx.Add(key_1, val_1);
            idx.Add(key_2, val_2);

            Assert.AreEqual(2L, idx.KeyCount());
            Assert.AreEqual(2L, idx.ValueCount());

            // remove the not existing key 3
            Assert.IsFalse(idx.Remove(key_3));
            // and check the counters
            Assert.AreEqual(2L, idx.KeyCount());
            Assert.AreEqual(2L, idx.ValueCount());

            // remove existing key 1
            Assert.IsTrue(idx.Remove(key_1));
            // and check the counters
            Assert.AreEqual(1L, idx.KeyCount());
            Assert.AreEqual(1L, idx.ValueCount());

            // remove not existing key 1
            Assert.IsFalse(idx.Remove(key_1));
            // and check the counters
            Assert.AreEqual(1L, idx.KeyCount());
            Assert.AreEqual(1L, idx.ValueCount());

            #endregion
        }

        /// <summary>
        /// This test tries to remove a value associated with a key. 
        /// Both, key and value exist in the index, and the value is 
        /// associated with the key.
        /// This should return true.
        /// </summary>
        [TestCase]
        public virtual void TryRemoveValue_ExistingKey_ExistingValue()
        {
            #region data

            var idx = new BinaryTreeIndex();

            var key_1 = 1;
            var val_1 = 1;
            var val_2 = 2;

            #endregion

            #region test

            idx.Add(key_1, val_1);

            // counter
            Assert.That(idx.KeyCount(), Is.EqualTo(1));
            Assert.That(idx.ValueCount(), Is.EqualTo(1));

            // try to remove existing value (only one value, this means the index has to be deleted too)
            Assert.IsTrue(idx.TryRemoveValue(key_1, val_1));
            Assert.That(idx.KeyCount(), Is.EqualTo(0));
            Assert.That(idx.ValueCount(), Is.EqualTo(0));

            // add two values for one key
            idx.Add(key_1, val_1);
            idx.Add(key_1, val_2, IndexAddStrategy.MERGE);

            Assert.That(idx.KeyCount(), Is.EqualTo(1));
            Assert.That(idx.ValueCount(), Is.EqualTo(2));

            // remove one value
            Assert.That(idx.TryRemoveValue(key_1, val_1), Is.True);
            Assert.That(idx.KeyCount(), Is.EqualTo(1));
            Assert.That(idx.ValueCount(), Is.EqualTo(1));
            Assert.That(idx[key_1].Contains(val_2), Is.True);
            Assert.That(idx[key_1].LongCount(), Is.EqualTo(1));

            #endregion
        }

        /// <summary>
        /// This test tries to remove a value associated with a key. 
        /// The key is stored in the index but the value is not
        /// associated with the key.
        /// This should return false.
        [TestCase]
        public virtual void TryRemoveValue_ExistingKey_NonExistingValue()
        {
            #region data

            var idx = new BinaryTreeIndex();

            var key_1 = 1;

            var val_1 = 1;
            var val_2 = 2;

            #endregion

            #region test

            idx.Add(key_1, val_1);

            // counter
            Assert.That(idx.KeyCount(), Is.EqualTo(1));
            Assert.That(idx.ValueCount(), Is.EqualTo(1));

            // try to remove existing value (only one value, this means the index has to be deleted too)
            Assert.IsFalse(idx.TryRemoveValue(key_1, val_2));
            Assert.That(idx.KeyCount(), Is.EqualTo(1));
            Assert.That(idx.ValueCount(), Is.EqualTo(1));

            #endregion
        }

        /// <summary>
        /// This test tries to remove a value associated with a key. 
        /// Neither the key or the value are stored in the index.
        /// This should return false.
        /// </summary>
        [TestCase]
        public virtual void TryRemoveValue_NonExistingKey_NonExistingValue()
        {
            #region data

            var idx = new BinaryTreeIndex();

            var key_1 = 1;
            var key_2 = 2;

            var val_1 = 1;
            var val_2 = 2;

            #endregion

            #region test

            idx.Add(key_1, val_1);

            // counter
            Assert.That(idx.KeyCount(), Is.EqualTo(1));
            Assert.That(idx.ValueCount(), Is.EqualTo(1));

            // try to remove existing value (only one value, this means the index has to be deleted too)
            Assert.IsFalse(idx.TryRemoveValue(key_2, val_2));
            Assert.That(idx.KeyCount(), Is.EqualTo(1));
            Assert.That(idx.ValueCount(), Is.EqualTo(1));

            #endregion
        }

        /// <summary>
        /// This test removes a collection of vertices from the index.
        /// </summary>
        [TestCase]
        public virtual void RemoveRange_RemoveVertices()
        {
            #region data

            var n = 10;
            var propertyID = 1L;
            var idx = new BinaryTreeIndex();
            var vertices = CreateVertices(n, propertyID);

            idx.Init(new List<Int64>() { propertyID });

            #endregion

            #region test

            idx.AddRange(vertices);

            Assert.That(idx.KeyCount(), Is.EqualTo(n));
            Assert.That(idx.ValueCount(), Is.EqualTo(n));

            idx.RemoveRange(vertices);

            Assert.That(idx.KeyCount(), Is.EqualTo(0));
            Assert.That(idx.ValueCount(), Is.EqualTo(0));

            #endregion

        }

        /// <summary>
        /// This tests remove a collection of keys from the index.
        /// </summary>
        [TestCase]
        public virtual void RemoveRange_RemoveKeys()
        {
            #region data

            var n = 10;
            var idx = new BinaryTreeIndex();
            var vertices = CreateKeyValuePairs(n);

            #endregion

            #region test

            idx.AddRange(vertices);

            Assert.That(idx.KeyCount(), Is.EqualTo(n));
            Assert.That(idx.ValueCount(), Is.EqualTo(n));

            idx.RemoveRange(vertices.Select(kvp => kvp.Key));

            Assert.That(idx.KeyCount(), Is.EqualTo(0));
            Assert.That(idx.ValueCount(), Is.EqualTo(0));

            #endregion
        }

        #endregion

        #region Clear Tests

        /// <summary>
        /// This test inserts some data into the index and then 
        /// clears it. All data has to be removed after that call.
        /// </summary>
        [TestCase]
        public virtual void Clear_RemoveAllKeys()
        {
            #region data

            var n = 10;
            var idx = new BinaryTreeIndex();
            var vertices = CreateKeyValuePairs(n);

            #endregion

            #region test

            idx.AddRange(vertices);

            Assert.That(idx.KeyCount(), Is.EqualTo(n));
            Assert.That(idx.ValueCount(), Is.EqualTo(n));

            idx.Clear();

            Assert.That(idx.KeyCount(), Is.EqualTo(0));
            Assert.That(idx.ValueCount(), Is.EqualTo(0));

            #endregion
        }

        #endregion

        #region InitializePlugin Tests

        /// <summary>
        /// This tests checks if the propertyID of an indexed is correctly set
        /// after initializing the plugin.
        /// </summary>
        [TestCase]
        public void InitializePlugin_CheckPropertyIDs()
        {
            #region data

            // create db
            var graphDB = new SonesGraphDB();
            var transactionToken = 0L;
            var securityToken = new SecurityToken();

            // create vertex type with attribute
            var vertexType = graphDB.CreateVertexType<IVertexType>(securityToken,
                transactionToken,
                new RequestCreateVertexType(
                    new VertexTypePredefinition("dummy")
                    .AddProperty(new PropertyPredefinition("age", "Int32"))),
                    (stats, type) => type);

            // create persistent index on attribute
            var indexDef = graphDB.CreateIndex<IIndexDefinition>(securityToken,
                transactionToken,
                new RequestCreateIndex(new IndexPredefinition("myindex", "dummy").AddProperty("age").SetIndexType("binarytree")),
                (stats, index) => index);

            #endregion

            #region test

            var metaManager = GetMetaManager(graphDB);
            var sonesIdx = metaManager.IndexManager.GetIndex("myindex", securityToken, transactionToken);

            // propertyID stored at the index
            var indexPropertyIDs = (List<Int64>)new BinaryTreeIndex().GetType().GetField("_PropertyIDs", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(sonesIdx);

            // propertyID stored at the vertextype
            var propID = vertexType.GetPropertyDefinition("age").ID;

            Assert.IsTrue(indexPropertyIDs.Contains(propID), "indexed propertyID is not the same as propertyID at vertex type");

            #endregion

            #region Cleanup

            graphDB.Shutdown(securityToken);

            #endregion
        }

        #endregion

        #endregion

        #region Private Helper

        /// <summary>
        /// Creates a list of vertices. VertexID are in ascending order beginning at 0.
        /// The value of the defined property is equal to the vertexID.
        /// </summary>
        /// <param name="myNumberOfVertices">Define how many vertices shall be created.</param>
        /// <param name="myPropertyID">The property to set</param>
        /// <returns>A list of vertices</returns>
        private IList<IVertex> CreateVertices(int myNumberOfVertices, Int64 myPropertyID)
        {
            var list = new List<IVertex>(myNumberOfVertices);

            for (long i = 0; i < myNumberOfVertices; i++)
            {
                list.Add(new InMemoryVertex(i,
                    1L,
                    1L,
                    null,
                    null,
                    null,
                    "dummy",
                    DateTime.Now.Ticks,
                    DateTime.Now.Ticks,
                    new Dictionary<long, IComparable>() { { myPropertyID, i } }, // structured properties
                    null));
            }

            return list;
        }

        /// <summary>
        /// Creates a list of key-value-pairs of type long, long. Key and value are equal.
        /// </summary>
        /// <param name="myNumberOfPairs">Define how many key-value-pairs shall be created</param>
        /// <returns>A list of key-value-pairs</returns>
        private IEnumerable<KeyValuePair<IComparable, Int64>> CreateKeyValuePairs(int myNumberOfPairs)
        {
            var list = new List<KeyValuePair<IComparable, Int64>>(myNumberOfPairs);

            for (long i = 0; i < myNumberOfPairs; i++)
            {
                list.Add(new KeyValuePair<IComparable, long>(i, i));
            }

            return list;
        }

        /// <summary>
        /// Uses reflection to get the RequestManager
        /// </summary>
        /// <param name="myGraphDB">A GraphDB instance</param>
        /// <returns>RequestManager</returns>
        protected SimpleRequestManager GetRequestManager(IGraphDB myGraphDB)
        {
            //gets the private IRequestManager of the _graphDBInstance
            return (SimpleRequestManager)typeof(sones.GraphDB.SonesGraphDB).GetField("_requestManager", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(myGraphDB);
        }

        /// <summary>
        /// Uses reflection to the the MetaManager
        /// </summary>
        /// <param name="myGraphDB">A GraphDB instance</param>
        /// <returns>MetaManager</returns>
        protected IMetaManager GetMetaManager(IGraphDB myGraphDB)
        {
            //gets the private IMetaManager of the IRequestManagerInstance
            return (IMetaManager)typeof(SimpleRequestManager).GetField("_metaManager", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(GetRequestManager(myGraphDB));
        }

        #endregion

    }
}
