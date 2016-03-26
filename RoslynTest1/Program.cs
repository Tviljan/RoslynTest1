using Ninject;
using Ninject.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ninject.Activation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.IO;
using Microsoft.CodeAnalysis.Emit;
using System.Reflection;

namespace RoslynTest1
{

    class Program
    {


        static void Main(string[] args)
        {
            var app = new RosApp();
            app.Init(new SimpleInputProcessor());
            app.Run();
        }
    }


    public class RosApp : IRosApp
    {
        IInputProcessor _processor { get; set; }
        public void Init(IInputProcessor generator)
        {

            _processor = generator;
        }

        public void Run()
        {
            Console.Write("To load assembly from file use assembly.class for file name and write #Load <file>");
            while (true)
            {
                var input = Console.ReadLine();
                if (input == "Q")
                    break;
                if (input.StartsWith("#Load"))
                {
                    var f = input.Split(' ');
                    if (f.Length == 2)
                    {
                        try
                        {
                            var assembly = Load(f[1]);
                            Type type = assembly.GetType(f[1]);
                            IInputProcessor obj = (IInputProcessor)Activator.CreateInstance(type);
                            _processor = obj;
                        }
                        catch (Exception)
                        {

                            throw;
                        }

                    }
                    else
                    {
                        Console.WriteLine("Wrong input");
                    }
                }
                _processor.Process(input);
            }
        }

        private Assembly Load(string file)
        {
            var fileInfo = new FileInfo(file);
            if (!fileInfo.Exists)
                throw new FileNotFoundException("File not found!", file);

            string assemblyName = fileInfo.Name;
            MetadataReference[] references = new MetadataReference[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(IInputProcessor).Assembly.Location)
            };


            SyntaxTree syntaxTree = null;

            try
            {   // Open the text file using a stream reader.
                using (var sr = fileInfo.OpenText())
                {
                    var f = sr.ReadToEnd();
                    syntaxTree = CSharpSyntaxTree.ParseText(f);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
                throw;
            }


            var compilation = CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using (var ms = new MemoryStream())
            {
                EmitResult result = compilation.Emit(ms);

                if (!result.Success)
                {
                    Console.WriteLine("Something went wrong...");
                    IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError ||
                        diagnostic.Severity == DiagnosticSeverity.Error);

                    foreach (Diagnostic diagnostic in failures)
                    {
                        Console.Error.WriteLine("{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
                    }
                }
                else
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    Assembly assembly = Assembly.Load(ms.ToArray());
                    return assembly;
                }
            }
            throw new Exception("Something went wrong");
        }
    }

    public class SimpleInputProcessor : IInputProcessor
    {
        public void Process(string input)
        {
            Console.Clear();
            Console.WriteLine(string.Format("You wrote: {0}", input));
        }
    }

}
