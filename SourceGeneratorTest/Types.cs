using System;

namespace SourceGeneratorTest
{
    public record Person(string Name, int Age);
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
