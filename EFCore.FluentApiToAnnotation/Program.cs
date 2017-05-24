using System;

namespace EFCore.FluentApiToAnnotation
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Converter Started");
            Console.WriteLine("Generating...");

            string inputPath = args.Length > 0 ? args[0] : null;
            string outputPath = args.Length > 1 ? args[1] : null;

            var converter = new Converter();
            converter.LoadFiles();
            converter.Run();
            converter.CsGenerator.CreateFiles();

            Console.WriteLine("Finished.");
            Console.WriteLine("Pres any key to close...");
        }
    }
}
