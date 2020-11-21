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
    //-------------------------------------------------------------------------
    public partial class Calc
    {
        [DoubleConstant(Math.PI)]
        private static partial double Pi();

        [DoubleConstant(Math.E)]
        private static partial double E();

        [DoubleConstant("1.2345")]
        private static partial double StringConst();

        public static void Print()
        {
            Console.WriteLine($"pi:        {Pi()}");
            Console.WriteLine($"e:         {E()}");
            Console.WriteLine($"1/sqrt(2): {StringConst()}");
        }

        // This code will be generated
        //private const long PiLong = 4614256656552045848;
        //private static partial double Pi() => BitConverter.Int64BitsToDouble(PiLong);
    }
}
