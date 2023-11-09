﻿using System.Data;

namespace aac4
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("Expected 2 arguments");
                return;
            }
            string grammarFilePath = args[0];
            string targetFilePath = args[1];

            string startNonterminal;
            Dictionary<string, List<string>> FIRST;
            Dictionary<string, List<string>> FOLLOW;

            DefaultDialogService dialogService = new DefaultDialogService();

            string[] machineFromTXT = File.ReadAllLines(grammarFilePath);
            if (machineFromTXT.Length != 0)
            {
                Grammar g = new(machineFromTXT);

                startNonterminal = g.StartNonTerm;
                var grammar = g.Rules;

                FIRST = new Dictionary<string, List<string>>();
                foreach (var grammarRules in grammar)
                    FIRST = dialogService.ConstructFIRST(grammar, grammarRules.Value, grammarRules.Key, FIRST);

                FOLLOW = dialogService.ConstructFOLLOW(FIRST, startNonterminal, grammar);

                DataTable predictiveAnalysisTable = dialogService.GeneratePredictiveAnalysisTable(grammar, FIRST, FOLLOW);

                string Sentence = File.ReadAllText(targetFilePath);
                string[] textRows = Sentence.Replace("\r\n", "\n").Split('\n');
                int[] numberOfCharactersInEachRow = new int[textRows.Count()];
                for (int i = 0; i < textRows.Count(); i++)
                    numberOfCharactersInEachRow[i] = textRows[i].Count();
                dialogService.TextCorrectnessVerification(predictiveAnalysisTable, FIRST, FOLLOW,
                    startNonterminal, Sentence, numberOfCharactersInEachRow);
            }
        }
    }
}
