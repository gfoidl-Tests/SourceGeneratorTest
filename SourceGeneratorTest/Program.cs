using System;
using System.Collections.Generic;
using GeneratedAnswer;

namespace SourceGeneratorTest
{
    class Program
    {
        static void Main(string[] args)
        {
            RunMySourceGenerator();
            Console.WriteLine();
            RunSimpleSerialization();
        }

        private static void RunMySourceGenerator()
        {
            Answers answers = new();

            int ans0 = answers.GetAnswer();
            Console.WriteLine(ans0);
        }

        private static void RunSimpleSerialization()
        {
            IEnumerable<Person> persons = GetPersons();

            SimpleSerializer serializer = new();
            serializer.SerializeViaReflection(persons);
            Console.WriteLine();
            serializer.SerializeTyped(persons);
            Console.WriteLine();
            serializer.Serialize(persons);          // via Source Generator

            static IEnumerable<Person> GetPersons()
            {
                yield return new Person("Anton", 42);
                yield return new Person("Berta", 39);
            }
        }
    }
}
