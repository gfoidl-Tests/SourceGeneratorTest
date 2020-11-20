using System;

namespace SourceGeneratorTest
{
    // A record would do it, but then there are more properties than I want for this demo.
    public class Person
    {
        public string? Name { get; init; }
        public int Age      { get; init; }
    }
    //-------------------------------------------------------------------------
    public class Data
    {
        private static int s_id;
        //---------------------------------------------------------------------
        public int Id              { get; } = ++s_id;
        public Guid Key            { get; } = Guid.NewGuid();
        public DateTimeOffset Time { get; } = DateTimeOffset.Now;
        public double Value        { get; }
        //---------------------------------------------------------------------
        public Data(double value) => this.Value = value;
    }
}
