using System;
using System.Collections.Generic;
using GeneratedAnswer;

namespace SourceGeneratorTest
{
    class Program
    {
        private static readonly SimpleSerializer s_simpleSerializer = new();
        //---------------------------------------------------------------------
        static void Main()
        {
            RunMySourceGenerator();
            Console.WriteLine(new string('-', 80));
            RunSimpleSerialization();
            Console.WriteLine(new string('-', 80));
            RunSimpleSerialization1();
        }
        //---------------------------------------------------------------------
        private static void RunMySourceGenerator()
        {
            Answers answers = new();

            int ans0 = answers.GetAnswer();
            Console.WriteLine(ans0);
        }
        //---------------------------------------------------------------------
        private static void RunSimpleSerialization()
        {
            IEnumerable<Person> persons = GetPersons();

            SimpleSerializer serializer = new();
            serializer.SerializeViaReflection(persons);
            Console.WriteLine();
            serializer.SerializeTyped(persons);
            Console.WriteLine();
            serializer.Serialize(persons);          // via Source Generator
            Console.WriteLine();
            s_simpleSerializer.Serialize(persons);

            static IEnumerable<Person> GetPersons()
            {
                yield return new Person { Name = "Anton", Age = 42 };
                yield return new Person { Name = "Berta", Age = 39 };
            }
        }
        //---------------------------------------------------------------------
        private static void RunSimpleSerialization1()
        {
            List<Data> values = new()
            {
                new Data(0.1),
                new Data(0.2),
                new Data(1.3)
            };

            s_simpleSerializer.Serialize(values);
        }
    }
}
