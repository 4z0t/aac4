using System.Data;
using System.Diagnostics;

namespace aac4
{
    class Program
    {
        static void Main(string[] args)
        {
            string grammarFilePath;
            string targetFilePath;
            if (args.Length != 2)
            {
                //Console.Error.WriteLine("Expected 2 arguments");
                //return;
                grammarFilePath = "D:\\Git\\aac4/aac4/Cgrammar.txt";
                targetFilePath = "D:\\Git\\aac4/aac4/example2.txt";
            }
            else
            {
                grammarFilePath = args[0];
                targetFilePath = args[1];
            }

            string[] machineFromTXT = File.ReadAllLines(grammarFilePath);
            if (machineFromTXT.Length != 0)
            {
                Grammar g = new(machineFromTXT);

                string startNonterminal = g.StartNonTerm;
                var grammar = g.Rules;

                Debug.WriteLine("------------File red------------");

                g.ConstructFIRST();

                Debug.WriteLine("------------FIRST------------");

                g.ConstructFOLLOW();

                Debug.WriteLine("------------FOLLOW------------");

                DataTable predictiveAnalysisTable = g.MakePredictiveAnalysisTable();

                Debug.WriteLine("------------PAT------------");

                string Sentence = File.ReadAllText(targetFilePath);

                ErrorCorrector errorCorrector = new(predictiveAnalysisTable);
                var res = errorCorrector.CheckText(startNonterminal, Sentence);
                if (res.errors != null)
                {
                    if (res.result != null)
                    {
                        Utility.PrintTableOrView(res.result, "Result Table");
                    }
                    Utility.PrintTableOrView(res.errors, "Table Of Errors");
                    Console.WriteLine("Error(s) found");
                }
                else if (res.result != null)
                {
                    Utility.PrintTableOrView(res.result, "Result Table");
                    Console.WriteLine("Errors not found");
                }


            }
        }
    }
}
