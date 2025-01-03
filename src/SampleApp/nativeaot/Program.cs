using System;
using System.Runtime.CompilerServices;

namespace nativeaot
{
    unsafe class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            Foo1(2);
        }

        static int Foo1(int x)
        {
            return Foo2(x);
        }

        static int Foo2(int x)
        {
            Foo4();
            return x;
        }

        static void Foo4()
        {
            Console.WriteLine("Hit key:");
            Console.ReadLine();

            var inner1 = new System.Runtime.InteropServices.ExternalException("Inner1", unchecked((int)0x80070057));
            ulong address1 = *(nuint*)Unsafe.AsPointer(ref inner1);
            Console.WriteLine($"inner1: {address1:X16} {inner1.Message}");

            var inner2 = new ArgumentException("Inner2");
            ulong address2 = *(nuint*)Unsafe.AsPointer(ref inner2);
            Console.WriteLine($"inner2: {address2:X16} {inner2.Message}");

            var inner3 = new ArgumentException(@"Inner3 c:\temp\foo");
            ulong address3 = *(nuint*)Unsafe.AsPointer(ref inner3);
            Console.WriteLine($"inner3: {address3:X16} {inner3.Message}");

            var agg = new AggregateException("Agg", new Exception[] { inner1, inner2, inner3 });
            ulong addressAgg = *(nuint*)Unsafe.AsPointer(ref agg);
            Console.WriteLine($"agg: {addressAgg:X16} {agg.Message}");
            throw agg;
        }
    }
}
