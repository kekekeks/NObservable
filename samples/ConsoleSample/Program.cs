using System;
using NObservable;

namespace ConsoleSample
{
    class Program
    {
        [Observable]
        class Foo
        {
            public int Prop1 { get; set; }
            public int Prop2 { get; set; }
        }
        
        static void Main(string[] args)
        {
            var o = new Foo{Prop1 = 1, Prop2 = 1};
            Console.WriteLine("Initial run");
            Observe.Autorun(() => {
                if(o.Prop1 == 3)
                    Console.WriteLine($"Prop1: {o.Prop1} Prop2: {o.Prop2}");
                else
                    Console.WriteLine($"Prop1: {o.Prop1}");    
            });
            Console.WriteLine("Setting Prop1 = 2, expecting update");
            o.Prop1 = 2;
            Console.WriteLine("Setting Prop2 = 2, it wasn't read last time, not expecting update");
            o.Prop2 = 2;
            Console.WriteLine("Setting Prop1 = 3, expecting update");
            o.Prop1 = 3;
            Console.WriteLine("Setting Prop2 = 3, it was read last time, expecting update");
            o.Prop2 = 3;

        }
    }
}