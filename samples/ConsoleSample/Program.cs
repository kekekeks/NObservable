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
                Console.WriteLine($"Prop1: {o.Prop1}");
                if(o.Prop1 == 3)
                    Console.WriteLine($"Prop1: {o.Prop1} Prop2: {o.Prop2}");
                else
                    Console.WriteLine($"Prop1: {o.Prop1}");    
            });
            Console.WriteLine("Setting Prop1 = 2");
            o.Prop1 = 2;
            Console.WriteLine("Setting Prop2 = 2");
            o.Prop2 = 2;
            Console.WriteLine("Setting Prop1 = 3");
            o.Prop1 = 3;
            Console.WriteLine("Setting Prop2 = 3");
            o.Prop2 = 3;

        }
    }
}