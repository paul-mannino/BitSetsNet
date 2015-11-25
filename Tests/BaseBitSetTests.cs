﻿using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BitsetsNET.Tests
{
    /// <summary>
    /// Summary description for IBitSet
    /// </summary>
    [TestClass]
    public abstract class BaseBitSetTests
    {

        const int TEST_SET_LENGTH = 10;
        const int TEST_ITERATIONS = 10;
        protected abstract IBitset CreateSetFromIndicies(int[] indices, int length);

        [TestMethod()]
        public virtual void AndTest()
        {
            int[] first = SetGenerator.GetRandomArray(TEST_SET_LENGTH);
            int[] second = SetGenerator.GetRandomArray(TEST_SET_LENGTH);
            int[] result = first.Intersect(second).ToArray();
            IBitset expected = CreateSetFromIndicies(result, TEST_SET_LENGTH);
            IBitset actual = CreateSetFromIndicies(first, TEST_SET_LENGTH).And(CreateSetFromIndicies(second, TEST_SET_LENGTH));
            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        public virtual void AndWithTest()
        {
            int[] first = SetGenerator.GetRandomArray(TEST_SET_LENGTH);
            int[] second = SetGenerator.GetRandomArray(TEST_SET_LENGTH);
            int[] result = first.Intersect(second).ToArray();
            IBitset testSet = CreateSetFromIndicies(first, TEST_SET_LENGTH);
            testSet.AndWith(CreateSetFromIndicies(second, TEST_SET_LENGTH));

            Assert.AreEqual(CreateSetFromIndicies(result, TEST_SET_LENGTH), testSet);
  
        }

        [TestMethod()]
        public virtual void CloneTest()
        {
            int[] set = SetGenerator.GetRandomArray(TEST_SET_LENGTH);
            IBitset testSet = CreateSetFromIndicies(set, TEST_SET_LENGTH);
            var clone = testSet.Clone();
            Assert.AreEqual(clone, testSet);
        }

        [TestMethod()]
        public virtual void GetTest()
        {
            int[] set = SetGenerator.GetRandomArray(TEST_SET_LENGTH);
            IBitset testSet = CreateSetFromIndicies(set, TEST_SET_LENGTH);
            bool expected = set.Contains(2);
            bool result = testSet.Get(2);
            Assert.AreEqual(expected, result);
        }

        [TestMethod()]
        public virtual void LengthTest()
        {
            int[] set = SetGenerator.GetRandomArray(TEST_SET_LENGTH);
            IBitset testSet = CreateSetFromIndicies(set, TEST_SET_LENGTH);
            Assert.AreEqual(TEST_SET_LENGTH, testSet.Length());
        }

        [TestMethod()]
        public virtual void OrTest()
        {
            int[] first = SetGenerator.GetRandomArray(TEST_SET_LENGTH);
            int[] second = SetGenerator.GetRandomArray(TEST_SET_LENGTH);
            int[] result = first.Union(second).ToArray();
            IBitset expected = CreateSetFromIndicies(result, TEST_SET_LENGTH);
            IBitset actual = CreateSetFromIndicies(first, TEST_SET_LENGTH).Or(CreateSetFromIndicies(second, TEST_SET_LENGTH));

            Assert.AreEqual(expected, actual);

        }

        [TestMethod()]
        public virtual void OrWithTest()
        {
            int[] first = SetGenerator.GetRandomArray(TEST_SET_LENGTH);
            int[] second = SetGenerator.GetRandomArray(TEST_SET_LENGTH);
            int[] result = first.Union(second).ToArray();
            IBitset testSet = CreateSetFromIndicies(first, TEST_SET_LENGTH);
            testSet.OrWith(CreateSetFromIndicies(second, TEST_SET_LENGTH));

            Assert.AreEqual(CreateSetFromIndicies(result, TEST_SET_LENGTH), testSet);
        }

        [TestMethod()]
        public virtual void SetTest()
        {

        }

        [TestMethod()]
        public virtual void SetAllTest()
        {

        }

    }
}
