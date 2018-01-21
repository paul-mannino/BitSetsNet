using System;
using System.Diagnostics;

namespace BitsetsNET.Examples
{
    public class Basic
    {
        static void Main(string[] args)
        {
            RoaringBitset rb = RoaringBitset.Create(1, 4, 5, 6);
            RoaringBitset rb2 = new RoaringBitset();
            rb2.Add(5000, 5255);

            RoaringBitset rbOr = RoaringBitset.Or(rb, rb2);
            rb.OrWith(rb2);

            Debug.Assert(rbOr.Equals(rb), "Expected inplace OrWith to be equal to standard Or");

            long cardinality = rb.Cardinality();
            Debug.Assert(cardinality == 259, "Cardinality should be 259");

            Console.WriteLine("Printing values...");
            foreach (int i in rb)
            {
                Console.WriteLine(i);
            }
        }
    }
}
