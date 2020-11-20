using System.Collections.Generic;
using SourceGeneratorTest;

IEnumerable<Person> persons = GetPersons();
SimpleSerializer serializer = new();
serializer.Serialize(persons);

static IEnumerable<Person> GetPersons()
{
    yield return new Person(1, "Batman", 42);
    yield return new Person(2, "Catwoman", 28);
    yield return new Person(3, "Superman", 67);
}

public record Person(int Id, string Name, int Age);
