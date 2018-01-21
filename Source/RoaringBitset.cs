using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace BitsetsNET
{
    public class RoaringBitset : IBitset
    {

        RoaringArray _containers = new RoaringArray();

        public static RoaringBitset Create(params int[] input)
        {
            RoaringBitset rb = new RoaringBitset();
            foreach (int i in input)
            {
                rb.Add(i);
            }
            return rb;
        }

        /// <summary>
        /// Adds the specified value to the current bitmap
        /// </summary>
        /// <param name="x">Value to be added</param>
        public void Add(int x)
        {
            ushort highBits = Utility.GetHighBits(x);
            int containerIndex = this._containers.GetIndex(highBits);

            if (containerIndex >= 0)
            {
                // a container exists at this index already.
                // find the right container, get the low order bits to add to the container and add them
                this._containers.SetContainerAtIndex(containerIndex, 
                                                    this._containers.GetContainerAtIndex(containerIndex)
                                                        .Add(Utility.GetLowBits(x)));
            }
            else
            {
                // no container exists for this index
                // create a new ArrayContainer, since it will only hold one integer to start
                // get the low order bits and att to the newly created container
                // add the newly created container to the array of containers
                ArrayContainer ac = new ArrayContainer();
                this._containers.InsertNewKeyValueAt(-containerIndex - 1, highBits, ac.Add(Utility.GetLowBits(x)));
            }
        }

        /// <summary>
        /// Add to the current bitmap all integers in [rangeStart,rangeEnd).
        /// </summary>
        /// <param name="rangeStart">Inclusive beginning of range</param>
        /// <param name="rangeEnd">Exclusive ending of range</param>
        public void Add(int rangeStart, int rangeEnd)
        {
            if (rangeStart >= rangeEnd)
            {
                return; // empty range
            }

            ushort hbStart = Utility.GetHighBits(rangeStart);
            ushort lbStart = Utility.GetLowBits(rangeStart);
            ushort hbLast = Utility.GetHighBits(rangeEnd - 1);
            ushort lbLast = Utility.GetLowBits(rangeEnd - 1);

            for (ushort hb = hbStart; hb <= hbLast; ++hb)
            {

                // first container may contain partial range
                ushort containerStart = 0;
                if (hb == hbStart)
                {
                    containerStart = lbStart;
                }

                // last container may contain partial range
                ushort containerLast = (hb == hbLast) ? lbLast : ushort.MaxValue;
                int containerIndex = this._containers.GetIndex(hb);

                if (containerIndex >= 0)
                {
                    Container c = this._containers.GetContainerAtIndex(containerIndex)
                                                 .Add(containerStart, (ushort)(containerLast + 1));
                    this._containers.SetContainerAtIndex(containerIndex, c);
                }
                else
                {
                    Container ac = new ArrayContainer(100);
                    ac = ac.Add(lbStart, (ushort)(lbLast + 1));
                    this._containers.InsertNewKeyValueAt(-containerIndex - 1, hb, ac);
                }
            }
        }

        /// <summary>
        /// Remove from the current bitmap all integers in [rangeStart,rangeEnd).
        /// </summary>
        /// <param name="rangeStart">inclusive beginning of range</param>
        /// <param name="rangeEnd">exclusive ending of range</param>
        public void Remove(int rangeStart, int rangeEnd)
        {
            if (rangeStart >= rangeEnd)
            {
                return; // empty range
            }

            ushort hbStart = Utility.GetHighBits(rangeStart);
            ushort lbStart = Utility.GetLowBits(rangeStart);
            ushort hbLast = Utility.GetHighBits(rangeEnd - 1);
            ushort lbLast = Utility.GetLowBits(rangeEnd - 1);

            if (hbStart == hbLast)
            {
                int containerIndex = _containers.GetIndex(hbStart);

                if (containerIndex < 0)
                {
                    return;
                }

                Container c = _containers.GetContainerAtIndex(containerIndex)
                                         .Remove(lbStart, (ushort)(lbLast + 1));

                if (c.Cardinality > 0)
                {
                    _containers.SetContainerAtIndex(containerIndex, c);
                }
                else
                {
                    _containers.RemoveAtIndex(containerIndex);
                }
                return;
            }

            int ifirst = _containers.GetIndex(hbStart);
            int ilast = _containers.GetIndex(hbLast);

            if (ifirst >= 0)
            {
                if (lbStart != 0)
                {
                    Container c = _containers.GetContainerAtIndex(ifirst)
                                             .Remove(lbStart, ushort.MaxValue);

                    if (c.Cardinality > 0)
                    {
                        _containers.SetContainerAtIndex(ifirst, c);
                        ifirst++;
                    }
                }
            }
            else
            {
                ifirst = -ifirst - 1;
            }

            if (ilast >= 0)
            {
                if (lbLast != ushort.MaxValue)
                {
                    Container c = _containers.GetContainerAtIndex(ilast)
                                            .Remove(0, (ushort)(lbLast + 1));

                    if (c.Cardinality > 0)
                    {
                        _containers.SetContainerAtIndex(ilast, c);
                    }
                    else
                    {
                        ilast++;
                    }
                }
                else
                {
                    ilast++;
                }
            }
            else
            {
                ilast = -ilast - 1;
            }

            _containers.RemoveIndexRange(ifirst, ilast);
        }

        public static RoaringBitset And(RoaringBitset x1, RoaringBitset x2)
        {
            RoaringBitset answer = new RoaringBitset();
            int length1 = x1._containers.Size, length2 = x2._containers.Size;
            int pos1 = 0, pos2 = 0;

            while (pos1 < length1 && pos2 < length2)
            {
                ushort s1 = x1._containers.GetKeyAtIndex(pos1);
                ushort s2 = x2._containers.GetKeyAtIndex(pos2);

                if (s1 == s2)
                {
                    Container c1 = x1._containers.GetContainerAtIndex(pos1);
                    Container c2 = x2._containers.GetContainerAtIndex(pos2);
                    Container c = c1.And(c2);

                    if (c.Cardinality > 0)
                    {
                        answer._containers.Append(s1, c);
                    }

                    ++pos1;
                    ++pos2;
                }
                else if (s1 < s2) // s1 < s2
                { 
                    pos1 = x1._containers.AdvanceUntil(s2, pos1);
                }
                else // s1 > s2
                { 
                    pos2 = x2._containers.AdvanceUntil(s1, pos2);
                }
            }
            return answer;
        }

        public static RoaringBitset Or(RoaringBitset x1, RoaringBitset x2)
        {
            return (RoaringBitset)x1.Or(x2);
        }

        /// <summary>
        /// Performs an in-place intersection of two Roaring Bitsets.
        /// </summary>
        /// <param name="other">the second Roaring Bitset to intersect</param>
        public void AndWith(RoaringBitset other)
        {
            int thisLength = this._containers.Size;
            int otherLength = other._containers.Size;
            int pos1 = 0, pos2 = 0, intersectionSize = 0;

            while (pos1 < thisLength && pos2 < otherLength)
            {
                ushort s1 = this._containers.GetKeyAtIndex(pos1);
                ushort s2 = other._containers.GetKeyAtIndex(pos2);

                if (s1 == s2)
                {
                    Container c1 = this._containers.GetContainerAtIndex(pos1);
                    Container c2 = other._containers.GetContainerAtIndex(pos2);
                    Container c = c1.IAnd(c2);

                    if (c.Cardinality > 0)
                    {
                        this._containers.ReplaceKeyAndContainerAtIndex(intersectionSize++, s1, c);
                    }
                        
                    ++pos1;
                    ++pos2;
                }
                else if (s1 < s2)
                { // s1 < s2
                    pos1 = this._containers.AdvanceUntil(s2, pos1);
                }
                else
                { // s1 > s2
                    pos2 = other._containers.AdvanceUntil(s1, pos2);
                }
            }
            this._containers.Resize(intersectionSize);
        }

        /// <summary>
        /// Returns the jth value stored in this bitset.
        /// Throws an IllegalArgumentException if the input value
        /// exceeds the cardinality of this set.
        /// </summary>
        /// <param name="j">Index of the value</param>
        /// <returns>The value</returns>
        public int Select(int j)
        {
            int leftover = j;
            for (int i = 0; i < this._containers.Size; i++)
            {
                Container c = this._containers.GetContainerAtIndex(i);
                int thisCardinality = c.Cardinality;
                if (thisCardinality > leftover)
                {
                    uint keycontrib = (uint) this._containers.GetKeyAtIndex(i) << 16;
                    uint lowcontrib = (uint) c.Select(leftover);
                    return (int) (lowcontrib + keycontrib);
                }
                leftover -= thisCardinality;
            }
            throw new ArgumentOutOfRangeException("select " + j + " when the cardinality is " + this.Cardinality());
        }

        /// <summary>
        /// Creates a new bitset that is the bitwise AND of this bitset with another
        /// </summary>
        /// <param name="otherSet">Other bitset</param>
        /// <returns>A new roaring bitset</returns>
        public IBitset And(IBitset otherSet)
        {
            RoaringBitset otherRoaring = otherSet as RoaringBitset;
            if (otherRoaring == null)
            {
                throw new ArgumentOutOfRangeException("otherSet must be a RoaringBitset");
            }
            return And(this, otherRoaring);
        }

        /// <summary>
        /// Performs an in-place intersection of two Roaring Bitsets.
        /// </summary>
        /// <param name="otherSet">the second Roaring Bitset to intersect</param>
        public void AndWith(IBitset otherSet)
        {
            RoaringBitset otherRoaring = otherSet as RoaringBitset;
            if (otherRoaring == null) 
            {
                throw new ArgumentOutOfRangeException("otherSet must be a RoaringBitset");
            }
            AndWith((RoaringBitset)otherSet);
        }

        /// <summary>
        /// Create a new bitset that is a deep copy of this one.
        /// </summary>
        /// <returns>The cloned bitset</returns>
        public IBitset Clone()
        {
            RoaringBitset x = new RoaringBitset();
            x._containers = _containers.Clone();
            return x;
        }

        /// <summary>
        /// Creates a new bitset that is the bitwise OR of this bitset with another
        /// </summary>
        /// <param name="otherSet">Other bitset</param>
        /// <returns>A new IBitset</returns>
        public IBitset Or(IBitset otherSet)
        {
            if (!(otherSet is RoaringBitset))
            {
                throw new ArgumentOutOfRangeException("otherSet must be a RoaringBitSet");
            }
            
            RoaringBitset answer = new RoaringBitset();
            RoaringBitset x2 = (RoaringBitset) otherSet;

            int pos1 = 0, pos2 = 0;
            int thisSize = this._containers.Size;
            int otherSetSize = x2._containers.Size;

            if (pos1 < thisSize && pos2 < otherSetSize)
            {
                ushort s1 = this._containers.GetKeyAtIndex(pos1);
                ushort s2 = x2._containers.GetKeyAtIndex(pos2);

                while (true)
                {
                    if (s1 == s2)
                    {
                        Container newContainer = this._containers.GetContainerAtIndex(pos1)
                                                     .Or(x2._containers.GetContainerAtIndex(pos2));
                        answer._containers.Append(s1, newContainer);
                        pos1++;
                        pos2++;
                        if ((pos1 == thisSize) || (pos2 == otherSetSize))
                        {
                            break;
                        }
                        s1 = this._containers.GetKeyAtIndex(pos1);
                        s2 = x2._containers.GetKeyAtIndex(pos2);
                    }
                    else if (s1 < s2)
                    {
                        answer._containers.AppendCopy(this._containers, pos1);
                        pos1++;
                        if (pos1 == thisSize)
                        {
                            break;
                        }
                        s1 = this._containers.GetKeyAtIndex(pos1);
                    }
                    else // s1 > s2
                    { 
                        answer._containers.AppendCopy(x2._containers, pos2);
                        pos2++;
                        if (pos2 == otherSetSize)
                        {
                            break;
                        }
                        s2 = x2._containers.GetKeyAtIndex(pos2);
                    }
                }
            }

            if (pos1 == thisSize)
            {
                answer._containers.AppendCopy(x2._containers, pos2, otherSetSize);
            }
            else if (pos2 == otherSetSize)
            {
                answer._containers.AppendCopy(this._containers, pos1, thisSize);
            }

            return answer;
        }

        /// <summary>
        /// Computes the in-place bitwise OR of this bitset with another
        /// </summary>
        /// <param name="otherSet">Other bitset</param>
        public void OrWith(IBitset otherSet)
        {
            if (!(otherSet is RoaringBitset))
            {
                throw new ArgumentOutOfRangeException("otherSet must be a RoaringBitSet");
            }

            RoaringBitset x2 = (RoaringBitset)otherSet;

            int pos1 = 0, pos2 = 0;
            int length1 = this._containers.Size, length2 = x2._containers.Size;

            if (pos1 < length1 && pos2 < length2)
            {
                ushort s1 = this._containers.GetKeyAtIndex(pos1);
                ushort s2 = x2._containers.GetKeyAtIndex(pos2);

                while (true)
                {
                    if (s1 == s2)
                    {
                        Container newContainer = this._containers.GetContainerAtIndex(pos1)
                                                     .IOr(x2._containers.GetContainerAtIndex(pos2));
                        this._containers.SetContainerAtIndex(pos1,newContainer);
                        pos1++;
                        pos2++;
                        if ((pos1 == length1) || (pos2 == length2))
                        {
                            break;
                        }
                        s1 = this._containers.GetKeyAtIndex(pos1);
                        s2 = x2._containers.GetKeyAtIndex(pos2);
                    }
                    else if (s1 < s2)
                    {
                        pos1++;
                        if (pos1 == length1)
                        {
                            break;
                        }
                        s1 = this._containers.GetKeyAtIndex(pos1);
                    }
                    else
                    { // s1 > s2
                        this._containers.InsertNewKeyValueAt(pos1, s2, x2._containers.GetContainerAtIndex(pos2));
                        pos1++;
                        length1++;
                        pos2++;
                        if (pos2 == length2)
                        {
                            break;
                        }
                        s2 = x2._containers.GetKeyAtIndex(pos2);
                    }
                }
            }

            if (pos1 == length1)
            {
                this._containers.AppendCopy(x2._containers, pos2, length2);
            } 
        }

        /// <summary>
        /// Return whether the given index is a member of this set
        /// </summary>
        /// <param name="index">the index to test</param>
        /// <returns>True if the index is a member of this set</returns>
        public bool Get(int index)
        {
            ushort highBits = Utility.GetHighBits(index);
            int containerIndex = _containers.GetIndex(highBits);

            // a container exists at this index already.
            // find the right container, get the low order bits to add to the 
            // container and add them
            if (containerIndex >= 0)
            {
                return _containers.GetContainerAtIndex(containerIndex)
                                 .Contains(Utility.GetLowBits(index));
            }
            else
            {
                // no container exists for this index
                return false;
            }
        }
        
        /// <summary>
        /// Adds the current index to the set if value is true, otherwise 
        /// removes it if the set contains it.
        /// </summary>
        /// <param name="index">The index to set</param>
        /// <param name="value">Boolean of whether to add or remove the index</param>
        public void Set(int index, bool value)
        {
            if (value)
            {
                Add(index);
            }
            else
            { 
                ushort hb = Utility.GetHighBits(index);
                int containerIndex = _containers.GetIndex(hb);

                if (containerIndex > -1)
                {
                    Container updatedContainer = _containers.GetContainerAtIndex(containerIndex)
                                                           .Remove(Utility.GetLowBits(index));
                    _containers.SetContainerAtIndex(containerIndex, updatedContainer);
                }
            }
        }

        /// <summary>
        /// For indices in the range [start, end) add the index to the set if
        /// the value is true, otherwise remove it.
        /// </summary>
        /// <param name="start">the index to start from (inclusive)</param>
        /// <param name="end">the index to stop at (exclusive)</param>
        public void Set(int start, int end, bool value)
        {
            if (value)
            {
                Add(start, end);
            }
            else
            {
                Remove(start, end);
            }
        }

        /// <summary>
        /// The number of members of the set
        /// </summary>
        /// <returns>an integer for the number of members in the set</returns>
        public int Cardinality()
        {
            int size = 0;
            for (int i = 0; i < this._containers.Size; i++)
            {
                size += this._containers.GetContainerAtIndex(i).Cardinality;
            }
            return size;
        }

        /// <summary>
        /// If the given index is not in the set add it, otherwise remove it.
        /// </summary>
        /// <param name="index">The index to flip</param>
        public void Flip(int x)
        {
            ushort hb = Utility.GetHighBits(x);
            int i = _containers.GetIndex(hb);

            if (i >= 0)
            {
                Container c = _containers.GetContainerAtIndex(i).Flip(Utility.GetLowBits(x));
                if (c.Cardinality > 0)
                {
                    _containers.SetContainerAtIndex(i, c);
                }
                else
                {
                    _containers.RemoveAtIndex(i);
                }
            }
            else
            {
                ArrayContainer newac = new ArrayContainer();
                _containers.InsertNewKeyValueAt(-i - 1, hb, newac.Add(Utility.GetLowBits(x)));
            }
        }

        /// <summary>
        /// For indices in the range [start, end) add the index to the set if
        /// it does not exists, otherwise remove it.
        /// </summary>
        /// <param name="start">the index to start from (inclusive)</param>
        /// <param name="end">the index to stop at (exclusive)</param>
        public void Flip(int start, int end)
        {
            if (start >= end)
            {
                return; // empty range
            }

            // Separate out the ranges of higher and lower-order bits
            int hbStart = Utility.ToIntUnsigned(Utility.GetHighBits(start));
            int lbStart = Utility.ToIntUnsigned(Utility.GetLowBits(start));
            int hbLast = Utility.ToIntUnsigned(Utility.GetHighBits(end - 1));
            int lbLast = Utility.ToIntUnsigned(Utility.GetLowBits(end - 1));

            for (int hb = hbStart; hb <= hbLast; hb++)
            {
                // first container may contain partial range
                int containerStart = (hb == hbStart) ? lbStart : 0;
                // last container may contain partial range
                int containerLast = (hb == hbLast) ? lbLast : Utility.GetMaxLowBitAsInteger();
                int i = _containers.GetIndex((ushort)hb);

                if (i >= 0)
                {
                    Container c = _containers.GetContainerAtIndex(i)
                                            .INot(containerStart, containerLast + 1);
                    if (c.Cardinality > 0)
                    {
                        _containers.SetContainerAtIndex(i, c);
                    }
                    else
                    {
                        _containers.RemoveAtIndex(i);
                    }
                }
                else
                {
                    _containers.InsertNewKeyValueAt(-i - 1, (ushort)hb,
                        Container.RangeOfOnes((ushort) containerStart, (ushort) (containerLast + 1)));
                }
            }
        }

        /// <summary>
        /// Finds members of a bitset that are not in the other set (ANDNOT).
        /// This does not modify either bitset.
        /// </summary>
        /// <param name="otherSet">The set to compare against</param>
        /// <returns>A new IBitset containing the members that are in
        /// the first bitset but not in the second.</returns>
        public IBitset AndNot(RoaringBitset otherSet)
        {

            RoaringBitset answer = new RoaringBitset();
            int pos1 = 0, pos2 = 0;
            int length1 = _containers.Size, length2 = otherSet._containers.Size;

            while (pos1 < length1 && pos2 < length2)
            {
                ushort s1 = _containers.GetKeyAtIndex(pos1);
                ushort s2 = otherSet._containers.GetKeyAtIndex(pos2);
                if (s1 == s2)
                {
                    Container c1 = _containers.GetContainerAtIndex(pos1);
                    Container c2 = otherSet._containers.GetContainerAtIndex(pos2);
                    Container c = c1.AndNot(c2);
                    if (c.Cardinality > 0)
                    {
                        answer._containers.Append(s1, c);
                    }
                    ++pos1;
                    ++pos2;
                }
                else if (Utility.CompareUnsigned(s1, s2) < 0)
                { // s1 < s2
                    int nextPos1 = _containers.AdvanceUntil(s2, pos1);
                    answer._containers.AppendCopy(_containers, pos1, nextPos1);
                    pos1 = nextPos1;
                }
                else
                { // s1 > s2
                    pos2 = otherSet._containers.AdvanceUntil(s1, pos2);
                }
            }
            if (pos2 == length2)
            {
                answer._containers.AppendCopy(_containers, pos1, length1);
            }
            return answer;
        }

        /// <summary>
        /// Finds members of this bitset that are not in the other set (ANDNOT).
        /// Modifies current bitset in place.
        /// </summary>
        /// <param name="otherSet">The set to compare against</param>
        public void IAndNot(RoaringBitset otherSet)
        {
            int pos1 = 0, pos2 = 0, intersectionSize = 0;
            int thisSize = _containers.Size;
            int otherSetSize = otherSet._containers.Size;

            while (pos1 < thisSize && pos2 < otherSetSize)
            {
                ushort s1 = _containers.GetKeyAtIndex(pos1);
                ushort s2 = otherSet._containers.GetKeyAtIndex(pos2);
                if (s1 == s2)
                {
                    Container c1 = _containers.GetContainerAtIndex(pos1);
                    Container c2 = otherSet._containers.GetContainerAtIndex(pos2);
                    Container c = c1.IAndNot(c2);
                    if (c.Cardinality > 0)
                    {
                        _containers.ReplaceKeyAndContainerAtIndex(intersectionSize++, s1, c);
                    }
                    ++pos1;
                    ++pos2;
                }
                else if (Utility.CompareUnsigned(s1, s2) < 0)
                { // s1 < s2
                    if (pos1 != intersectionSize)
                    {
                        Container c1 = _containers.GetContainerAtIndex(pos1);
                        _containers.ReplaceKeyAndContainerAtIndex(intersectionSize, s1, c1);
                    }
                    ++intersectionSize;
                    ++pos1;
                }
                else
                { // s1 > s2
                    pos2 = otherSet._containers.AdvanceUntil(s1, pos2);
                }
            }
            if (pos1 < thisSize)
            {
                _containers.CopyRange(pos1, thisSize, intersectionSize);
                intersectionSize += thisSize - pos1;
            }
            _containers.Resize(intersectionSize);
        }

        /// <summary>
        /// Finds members of this bitset that are not in the other set (ANDNOT).
        /// This does not modify either bitset.
        /// </summary>
        /// <param name="otherSet">The set to compare against</param>
        /// <returns>A new IBitset containing the members that are in
        /// this bitset but not in the other.</returns>
        public IBitset Difference(IBitset otherSet)
        {
            if (otherSet is RoaringBitset)
            {
                return this.AndNot((RoaringBitset) otherSet);
            }
            throw new ArgumentOutOfRangeException("Other set must be a roaring bitset");      
        }

        /// <summary>
        /// Finds members of this bitset that are not in the other set (ANDNOT).
        /// Places the results in the current bitset (modifies in place).
        /// </summary>
        /// <param name="otherSet">The set to compare against</param>
        /// <returns>A new IBitset containing the members that are in
        /// this bitset but not in the other.</returns>
        public void DifferenceWith(IBitset otherSet)
        {
            if (otherSet is RoaringBitset)
            {
                this.IAndNot((RoaringBitset)otherSet);
            }
            else
            {
                throw new ArgumentOutOfRangeException("Other set must be a roaring bitset");
            }
            
        }

        public override bool Equals(Object o)
        {
            if (o is RoaringBitset)
            {
                RoaringBitset srb = (RoaringBitset)o;
                return srb._containers.Equals(this._containers);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return _containers.GetHashCode();
        }


        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {

        }

        public BitArray ToBitArray()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Use a run-length encoding where it is more space efficient
        /// </summary>
        /// <returns>Whether a change is applied.</returns>
        public bool RunOptimize()
        {
            bool answer = false;
            for (int i = 0; i < this._containers.Size; i++)
            {
                Container c = this._containers.GetContainerAtIndex(i).RunOptimize();
                if (c is RunContainer)
                {
                    answer = true;
                }
                this._containers.SetContainerAtIndex(i, c);
            }
            return answer;
        }

        /// <summary>
        /// Computes an approximation of the memory usage of this container.
        /// </summary>
        /// <returns>Estimated memory usage in bytes.</returns>
        public long SizeInBytes()
        {
            long size = 8;
            for (int i = 0; i < _containers.Size; i++)
            {
                Container c = _containers.GetContainerAtIndex(i);
                size += 2 + c.SizeInBytes();
            }
            return size;
        }

        /// <summary>
        /// Write a binary serialization of this roaring bitset.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        public void Serialize(Stream stream)
        {
            //We don't care about the encoding, but we have to specify something to be able to set the stream as leave open.
            using (BinaryWriter writer = new BinaryWriter(stream, Encoding.Default, true))
            {
                _containers.Serialize(writer);
            }
        }

        /// <summary>
        /// Read a binary serialization of a roaring bitset, as written by the Serialize method.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <returns>The bitset deserialized from the stream.</returns>
        public static RoaringBitset Deserialize(Stream stream)
        {
            RoaringBitset bitset = new RoaringBitset();

            //We don't care about the encoding, but we have to specify something to be able to set the stream as leave open.
            using (BinaryReader reader = new BinaryReader(stream, Encoding.Default, true))
            {
                bitset._containers = RoaringArray.Deserialize(reader);
            }

            return bitset;
        }

        /// <summary>
        /// Get an enumerator of the set indices of this bitset.
        /// </summary>
        /// <returns>A enumerator giving the set (i.e. for which the bit is '1' or true) indices for this bitset.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<int> GetEnumerator()
        {
            return _containers.GetEnumerator();
        }

    }
}
