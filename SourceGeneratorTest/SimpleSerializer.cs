using System;
using System.Collections.Generic;
using System.Reflection;

namespace SourceGeneratorTest
{
    public partial class SimpleSerializer
    {
        public void SerializeViaReflection<T>(IEnumerable<T> values) where T : class
        {
            PropertyInfo[] properties = typeof(T).GetProperties();

            foreach (PropertyInfo pi in properties)
            {
                Console.Write($"{pi.Name}\t");
            }
            Console.WriteLine();

            foreach (T item in values)
            {
                foreach (PropertyInfo pi in properties)
                {
                    object? value = pi.GetValue(item);
                    Console.Write($"{value}\t");
                }
                Console.WriteLine();
            }
        }

        public void SerializeTyped(IEnumerable<Person> values)
        {
            Console.WriteLine("Name\tAge");

            foreach (Person item in values)
            {
                Console.WriteLine($"{item.Name}\t{item.Age}");
            }
        }
    }
}
