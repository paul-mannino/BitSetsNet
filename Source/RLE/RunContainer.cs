using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitsetsNET
{
    public class RunContainer : Container
    {
        private static int DEFAULT_INIT_SIZE = 4;
        private static bool ENABLE_GALLOPING_AND = false;

        /// <summary>
        /// Compact representation of contiguous values. For instance,
        /// 14, 15, 16, 17 can be stored in the array as 14, 3 (i.e. 
        /// 14 followed by 3 contiguous values).
        /// </summary>
        private short[] valuesLength;
        private int runCount = 0;

        public override int Cardinality
        {
            get
            {
                int sum = runCount;
                for (int k = 0; k < runCount; k++)
                {
                    sum = sum + Utility.ToIntUnsigned(getLength(k));
                }
                return sum;
            }

            set { throw new InvalidOperationException("Cannot set RLE cardinality"); }
        }

        //public RunContainer() : this(DEFAULT_INIT_SIZE) { }

        protected RunContainer(ArrayContainer arr, int runCount)
        {
            this.runCount = runCount;
            this.valuesLength = new short[2 * runCount];
            if (runCount == 0)
            {
                return;
            }

            int prevVal = -2;
            int runLen = 0;
            int runIndex = 0;

            for (int i = 0; i < arr.Cardinality; i++)
            {
                int curVal = Utility.ToIntUnsigned(arr.Content[i]);
                if (curVal == prevVal + 1)
                {
                    ++runLen;
                }
                else
                {
                    if (runIndex > 0)
                    {
                        setLength(runIndex - 1, (short)runLen);
                    }
                    setValue(runIndex, (short)curVal);
                    runLen = 0;
                    ++runIndex;
                }
                prevVal = curVal;
            }
            setLength(runIndex - 1, (short)runLen);
        }

        // TO-DO: proof-read
        protected RunContainer(BitsetContainer bc, int runCount)
        {
            this.runCount = runCount;
            this.valuesLength = new short[2 * runCount];
            if (runCount == 0)
            {
                return;
            }

            int bitsetPtr = 0;
            long bitsetValue = bc.Bitmap[0];
            int runIndex = 0;
            while (true)
            {
                while (bitsetValue == 0L && bitsetPtr < bc.Bitmap.Length -1)
                {
                    bitsetValue = bc.Bitmap[++bitsetPtr];
                }

                if (bitsetValue == 0L)
                {
                    // finished processing bitset container
                    return;
                }

                int localRunStart = Utility.NumberOfTrailingZeros(bitsetValue);
                int runStart = localRunStart + 64 * bitsetPtr;
                // all 1s
                long bitsetValueMask = bitsetValue | (bitsetValue - 1);
                
                // find next 0 bit
                int runEnd = 0;
                while (bitsetValueMask == -1L && bitsetPtr < bc.Bitmap.Length - 1)
                {
                    bitsetValueMask = bc.Bitmap[++bitsetPtr];
                }

                if (bitsetValueMask == -1L)
                {
                    runEnd = 64 + bitsetPtr * 64;
                    setValue(runIndex, (short)runStart);
                    setLength(runIndex, (short)(runEnd - runStart - 1));
                    return;
                }
                int localRunEnd = Utility.NumberOfTrailingZeros(~bitsetValueMask);
                runEnd = localRunEnd + bitsetPtr * 64;
                setValue(runIndex, (short)runStart);
                setLength(runIndex, (short)(runEnd - runStart - 1));
                runIndex++;

                bitsetValue = bitsetValueMask & (bitsetValueMask + 1);
            }
        }

        public RunContainer(int capacity)
        {
            this.valuesLength = new short[2 * capacity];
        }

        public RunContainer(int nRuns, short[] valuesLength)
        {
            this.runCount = nRuns;
            Array.Copy(valuesLength, this.valuesLength, valuesLength.Length);
        }

        public RunContainer(short[] array, int runCount)
        {
            if (array.Length < 2 * runCount)
            {
                throw new ArgumentException("Array not large enough to accomodate runs");
            }
            this.runCount = runCount;
            this.valuesLength = array;
        }

        public override Container Add(ushort x)
        {
            int index = Utility.UnsignedBinarySearch(valuesLength, 0, runCount, x);
            if (index >= 0) {
                return this;
            }
            index = -index - 2;
            if (index >= 0) {
                int offset = Utility.ToIntUnsigned(x) - Utility.ToIntUnsigned(getValue(index));
                int len = Utility.ToIntUnsigned(getLength(index));
                if (offset <= len) {
                    return this;
                }
                if (offset == len + 1) {
                    if (index + 1 < runCount) {
                        if (Utility.ToIntUnsigned(getValue(index + 1)) == Utility.ToIntUnsigned(x) + 1) {
                            setLength(index,
                                (short)(getValue(index + 1) + getLength(index + 1) - getValue(index)));
                            recoverRoomAtIndex(index + 1);
                            return this; 
                        }
                    }
                    incrementLength(index);
                    return this;
                }
                if (index + 1 < runCount) {
                    if (Utility.ToIntUnsigned(getValue(index + 1)) == Utility.ToIntUnsigned(x) + 1) {
                        setValue(index + 1, (short) x);
                        setLength(index + 1, (short) (getLength(index + 1) + 1));
                        return this;
                    }
                }
            }
            if (index == -1) {
                if (0 < runCount) {
                    if (getValue(0) == x + 1) {
                        incrementLength(0);
                        decrementValue(0);
                        return this;
                    }
                }
            }
            makeRoomAtIndex(index + 1);
            setValue(index + 1, (short) x);
            setLength(index + 1, (short) 0);
            return this;
        }

        public override Container Add(ushort rangeStart, ushort rangeEnd)
        {
            RunContainer rc = (RunContainer)Clone();
            return rc.IAdd(rangeStart, rangeEnd);
        }

        public override Container And(ArrayContainer x)
        {
            throw new NotImplementedException();
        }

        public override Container And(BitsetContainer x)
        {
            throw new NotImplementedException();
        }

        public override Container AndNot(ArrayContainer x)
        {
            throw new NotImplementedException();
        }

        public override Container AndNot(BitsetContainer x)
        {
            throw new NotImplementedException();
        }

        public override Container IAndNot(ArrayContainer x)
        {
            throw new NotImplementedException();
        }

        public override Container IAndNot(BitsetContainer x)
        {
            throw new NotImplementedException();
        }

        public override Container Clone()
        {
            throw new NotImplementedException();
        }

        public override bool Contains(ushort x)
        {
            throw new NotImplementedException();
        }

        public override void FillLeastSignificant16bits(int[] x, int i, int mask)
        {
            throw new NotImplementedException();
        }

        public override Container IAnd(ArrayContainer x)
        {
            throw new NotImplementedException();
        }

        public override Container IAnd(BitsetContainer x)
        {
            throw new NotImplementedException();
        }

        public override Container INot(int start, int end)
        {
            throw new NotImplementedException();
        }

        public override bool Intersects(ArrayContainer x)
        {
            throw new NotImplementedException();
        }

        public override bool Intersects(BitsetContainer x)
        {
            throw new NotImplementedException();
        }

        public override Container Flip(ushort x)
        {
            throw new NotImplementedException();
        }

        public override Container IAdd(ushort begin, ushort end)
        {
            throw new NotImplementedException();
        }

        public override Container IOr(ArrayContainer x)
        {
            throw new NotImplementedException();
        }

        public override Container IOr(BitsetContainer x)
        {
            throw new NotImplementedException();
        }

        public override Container Or(ArrayContainer x)
        {
            throw new NotImplementedException();
        }

        public override Container Or(BitsetContainer x)
        {
            throw new NotImplementedException();
        }

        public override Container Remove(ushort x)
        {
            throw new NotImplementedException();
        }

        public override Container Remove(ushort begin, ushort end)
        {
            throw new NotImplementedException();
        }

        public override Container RunOptimize()
        {
            throw new NotImplementedException();
        }

        public override ushort Select(int j)
        {
            throw new NotImplementedException();
        }

        public override int SizeInBytes()
        {
            throw new NotImplementedException();
        }

        public override void Serialize(BinaryWriter writer)
        {
            throw new NotImplementedException();
        }

        public override IEnumerator<ushort> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        private void setLength(int index, short v)
        {
            setLength(valuesLength, index, v);
        }

        private void setLength(short[] valuesLength, int index, short v)
        {
            valuesLength[2 * index + 1] = v;
        }

        private void setValue(int index, short v)
        {
            setValue(valuesLength, index, v);
        }

        private void setValue(short[] valuesLength, int index, short v)
        {
            valuesLength[2 * index] = v;
        }

        private short getValue(int index) {
            return getValue(valuesLength, index);
        }
        
        private short getValue(short[] valuesLength, int index) {
            return valuesLength[2 * index];
        }

        private void incrementLength(int index) {
            valuesLength[(2 * index) + 1]++;
        }

        private void incrementValue(int index) {
            valuesLength[2 * index]++;
        } 

        private void decrementValue(int index) {
            valuesLength[2 * index]--;
        }

        private short getLength(int index)
        {
            return valuesLength[2 * index + 1];
        }

    }
}
