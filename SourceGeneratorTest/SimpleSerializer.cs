using System;
using System.Collections.Generic;
using System.Reflection;

namespace SourceGeneratorTest
{
    public partial class SimpleSerializer
    {
        public void SerializeViaReflection<T>(IEnumerable<T> items) where T : class
        {
            PropertyInfo[] properties = typeof(T).GetProperties();

            foreach (PropertyInfo pi in properties)
            {
                Console.Write($"{pi.Name}\t");
            }
            Console.WriteLine();

            foreach (T item in items)
            {
                foreach (PropertyInfo pi in properties)
                {
                    object? value = pi.GetValue(item);
                    Console.Write($"{value}\t");
                }
                Console.WriteLine();
            }
        }
        //---------------------------------------------------------------------
        public void SerializeTyped(IEnumerable<Person> items)
        {
            Console.WriteLine("Name\tAge");

            foreach (Person item in items)
            {
                Console.WriteLine($"{item.Name}\t{item.Age}");
            }
        }
    }
}
