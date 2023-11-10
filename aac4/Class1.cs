using Microsoft.Win32;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;

namespace aac4
{

    using GrammarDict = Dictionary<string, List<string>>;

    public class BrokenFileException : Exception
    {
        public BrokenFileException() : base("The file is broken!") { }
    }

    public class DefaultDialogService
    {
        public string BuildGrammarDictionary(string[] rules, out GrammarDict grammar)
        {
            grammar = new GrammarDict();

            string nonterminal = string.Empty;
            List<string> listOfReplacementsForNonterminal = new();
            StringBuilder subline = new();
            foreach (var line in rules)
            {
                string replacementsForNonterminal;
                if (Regex.Match(line, @"<.*?>:\s.*").Success)
                {
                    if (!nonterminal.Equals(string.Empty))
                    {
                        listOfReplacementsForNonterminal.Add(subline.ToString());
                        subline.Clear();
                        grammar.Add(nonterminal, new List<string>(listOfReplacementsForNonterminal));
                        listOfReplacementsForNonterminal.Clear();
                    }
                    nonterminal = Regex.Replace(line, @"(?<nonterminal><.*?>):\s.*", "${nonterminal}");
                    replacementsForNonterminal = Regex.Replace(line, @"<.*?>:\s(?<replacements>.*)", "${replacements}");
                }
                else if (Regex.Match(line, @"\t\|\s.*").Success && !nonterminal.Equals(string.Empty))
                {
                    if (subline.Length != 0)
                    {
                        listOfReplacementsForNonterminal.Add(subline.ToString());
                        subline.Clear();
                    }
                    replacementsForNonterminal = Regex.Replace(line, @"\t\|\s(?<replacements>.*)", "${replacements}");
                }
                else
                {
                    throw new BrokenFileException();
                }


                if (!replacementsForNonterminal.Equals(string.Empty))
                {
                    for (int i = 0; i < replacementsForNonterminal.Length; i++)
                    {
                        switch (replacementsForNonterminal[i])
                        {
                            case '<':
                                subline.Append(replacementsForNonterminal[i++]);
                                if (i >= replacementsForNonterminal.Length)
                                    throw new ArgumentException();
                                if (replacementsForNonterminal[i] != '>')
                                    do
                                    {
                                        if (i >= replacementsForNonterminal.Length)
                                            throw new BrokenFileException();
                                        subline.Append(replacementsForNonterminal[i]);
                                    } while (replacementsForNonterminal[i++] != '>');
                                else
                                    throw new BrokenFileException();
                                i--;
                                break;

                            case '\'':
                                subline.Append(replacementsForNonterminal[i++]);
                                if (i >= replacementsForNonterminal.Length)
                                    throw new BrokenFileException();
                                if (replacementsForNonterminal[i] != '\'')
                                    do
                                    {
                                        if (i >= replacementsForNonterminal.Length)
                                            throw new BrokenFileException();
                                        subline.Append(replacementsForNonterminal[i]);
                                    } while (replacementsForNonterminal[i++] != '\'');
                                else
                                    throw new BrokenFileException();
                                i--;
                                break;

                            case ' ':
                                if (i + 1 < replacementsForNonterminal.Length)
                                {
                                    if (replacementsForNonterminal[i + 1] != ' ' &&
                                        replacementsForNonterminal[i + 1] != '|')
                                    {
                                        //subline.Append("<__>");
                                        subline.Append("' '");
                                    }
                                    else if (i + 3 < replacementsForNonterminal.Length)
                                    {
                                        if (replacementsForNonterminal[++i] == '|' &&
                                            replacementsForNonterminal[++i] == ' ')
                                        {
                                            listOfReplacementsForNonterminal.Add(subline.ToString());
                                            subline.Clear();
                                        }
                                    }
                                    else
                                    {
                                        throw new BrokenFileException();
                                    }
                                }
                                else
                                    throw new BrokenFileException();
                                break;
                            default:
                                throw new BrokenFileException();
                        }
                    }
                }
                else
                {
                    listOfReplacementsForNonterminal.Add("$");
                }
            }
            listOfReplacementsForNonterminal.Add(subline.ToString());
            subline.Clear();
            grammar.Add(nonterminal, new List<string>(listOfReplacementsForNonterminal));
            //foreach (var (k, v) in grammar)
            //{
            //    Console.WriteLine("'" + k + "'");
            //    foreach (var s in v)
            //    {
            //        Console.WriteLine(s);
            //    }
            //}
            listOfReplacementsForNonterminal.Clear();

            return Regex.Replace(rules[0], @"(?<nonterminal><.*?>):\s.*", "${nonterminal}");
        }

        public static void Seek(string s, ref StringBuilder sb, char start, char end, ref int i)
        {
            sb.Append(start);
            while (s[++i] != end)
                sb.Append(s[i]);
            sb.Append(end);
        }

        public GrammarDict ConstructFIRST(GrammarDict grammar, List<string> grammarRules, string currentNonterminal, GrammarDict oldFIRST)
        {
            GrammarDict newFIRST = new(oldFIRST);

            List<string> terminals = new();
            StringBuilder currentSentence = new();
            bool isItFirstTime = true;
            bool doAllPreviousFIRSTsHaveEmptySymbol = false;
            foreach (var grammarRule in grammarRules)
            {
                for (int i = 0; i < grammarRule.Length; i++)
                {
                    switch (grammarRule[i])
                    {
                        case '<':
                            Seek(grammarRule, ref currentSentence, '<', '>', ref i);
                            break;
                        case '\'':
                            Seek(grammarRule, ref currentSentence, '\'', '\'', ref i);
                            break;
                        case '$':
                            currentSentence.Append("ε");
                            break;
                    }

                    if (grammar.ContainsKey(currentSentence.ToString()) && isItFirstTime)
                    {
                        newFIRST = this.ConstructFIRST(grammar, grammar[currentSentence.ToString()],
                            currentSentence.ToString(), newFIRST);
                        if (newFIRST[currentSentence.ToString()].Contains("ε"))
                        {
                            doAllPreviousFIRSTsHaveEmptySymbol = true;
                            newFIRST[currentSentence.ToString()].Remove("ε");
                        }
                        terminals.AddRange(newFIRST[currentSentence.ToString()].Except(terminals));
                    }
                    else if (grammar.ContainsKey(currentSentence.ToString()) && !isItFirstTime)
                    {
                        if (doAllPreviousFIRSTsHaveEmptySymbol)
                        {
                            newFIRST = this.ConstructFIRST(grammar, grammar[currentSentence.ToString()],
                                currentSentence.ToString(), newFIRST);
                            if (!newFIRST[currentSentence.ToString()].Contains("ε") &&
                                doAllPreviousFIRSTsHaveEmptySymbol)
                            {
                                doAllPreviousFIRSTsHaveEmptySymbol = false;
                            }
                            else if (newFIRST[currentSentence.ToString()].Contains("ε") &&
                                !doAllPreviousFIRSTsHaveEmptySymbol)
                            {
                                newFIRST[currentSentence.ToString()].Remove("ε");
                            }
                            terminals.AddRange(newFIRST[currentSentence.ToString()].Except(terminals));
                        }
                    }
                    else
                    {
                        if (currentSentence.ToString().Equals("ε"))
                        {
                            doAllPreviousFIRSTsHaveEmptySymbol = true;
                        }
                        else
                        {
                            if (!terminals.Contains(currentSentence.ToString()))
                                terminals.Add(currentSentence.ToString());
                            currentSentence.Clear();
                        }
                        break;
                    }
                    currentSentence.Clear();
                    isItFirstTime = false;
                }
                if (doAllPreviousFIRSTsHaveEmptySymbol)
                {
                    terminals.Add("ε");
                }
                currentSentence.Clear();
                doAllPreviousFIRSTsHaveEmptySymbol = false;
            }

            if (!newFIRST.ContainsKey(currentNonterminal))
                newFIRST.Add(currentNonterminal, terminals);
            return newFIRST;
        }

        public GrammarDict ConstructFOLLOW(GrammarDict FIRST, string startNonterminal, GrammarDict grammar)
        {
            GrammarDict newFOLLOW = new();
            if (newFOLLOW.Count != grammar.Count)
                foreach (var nonterminal in grammar.Keys)
                    newFOLLOW.Add(nonterminal, new List<string>());

            if (!newFOLLOW[startNonterminal].Contains("$"))
                newFOLLOW[startNonterminal].Add("'$'");

            // Если имеется продукция A-> aBb, то все элементы множества FIRST(b), кроме ε, помещаются в множество FOLLOW(B).
            // Если имеется продукция A-> aB или A-> aBb, где FIRST(b) содержит ε (т.е. b => ε), то все элементы из множества FOLLOW(A) 
            // помещаются в множество FOLLOW(B).
            bool hasFOLLOWchanged = true;
            while (hasFOLLOWchanged)
            {
                hasFOLLOWchanged = false;
                foreach (var grammarRules in grammar)
                {
                    foreach (string grammarRuleForeach in grammarRules.Value)
                    {
                        // Сдвиг окна aBb в паттерне A-> aBb.
                        string grammarRule = grammarRuleForeach;
                        Match match;
                        while (true)
                        {
                            match = Regex.Match(grammarRule, @"^.*?(<.*?>).+$");
                            if (!match.Success)
                            {
                                break;
                            }

                            string B = match.Groups[1].Value;

                            string beta = grammarRuleForeach.Remove(0, grammarRuleForeach.IndexOf(B) + B.Length);
                            if (FIRST.ContainsKey(beta))
                            {
                                List<string> rangeToAdd = new(FIRST[beta].Where(x => !x.Equals("ε")).
                                    Except(newFOLLOW[B]));
                                if (rangeToAdd.Count != 0)
                                {
                                    newFOLLOW[B].AddRange(new List<string>(FIRST[beta].Where(x => !x.Equals("ε")).
                                    Except(newFOLLOW[B])));
                                    hasFOLLOWchanged = true;
                                }

                                if (FIRST[beta].Contains("ε"))
                                {
                                    rangeToAdd = new List<string>(newFOLLOW[grammarRules.Key].Except(newFOLLOW[B]));
                                    if (rangeToAdd.Count != 0)
                                    {
                                        newFOLLOW[B].AddRange(rangeToAdd);
                                        hasFOLLOWchanged = true;
                                    }
                                }
                            }
                            else
                            {
                                bool doesTemporaryFirstContainEpsilon = false;

                                GrammarDict temporaryFIRST = ConstructFIRST(grammar, new List<string> { beta }, "beta", new());
                                if (temporaryFIRST["beta"].Contains("ε"))
                                    doesTemporaryFirstContainEpsilon = true;
                                temporaryFIRST["beta"] = new List<string>(temporaryFIRST["beta"].Except(newFOLLOW[B]));
                                if (temporaryFIRST["beta"].Count != 0)
                                {
                                    newFOLLOW[B].AddRange(temporaryFIRST["beta"].Except(newFOLLOW[B]));
                                    hasFOLLOWchanged = true;
                                }

                                if (doesTemporaryFirstContainEpsilon)
                                {
                                    List<string> rangeToAdd = new List<string>(newFOLLOW[grammarRules.Key].Except(newFOLLOW[B]));
                                    if (rangeToAdd.Count != 0)
                                    {
                                        newFOLLOW[B].AddRange(rangeToAdd);
                                        hasFOLLOWchanged = true;
                                    }
                                }
                            }
                            grammarRule = Regex.Replace(grammarRule, B, "");

                        }
                        match = Regex.Match(grammarRule, @"^.*(<.*?>)$");
                        if (match.Success)
                        {
                            string B = match.Groups[1].Value;
                            List<string> rangeToAdd = new(newFOLLOW[grammarRules.Key].Except(newFOLLOW[B]));
                            if (rangeToAdd.Count != 0)
                            {
                                newFOLLOW[B].AddRange(rangeToAdd);
                                hasFOLLOWchanged = true;
                            }
                        }
                    }
                }
            }

            return newFOLLOW;
        }

        public DataTable GeneratePredictiveAnalysisTable(GrammarDict grammar, GrammarDict FIRST, GrammarDict FOLLOW)
        {
            DataTable predictiveAnalysisTable = new();
            List<string> headerRow = new()
            {
                "Nonterminals"
            };

            // Generates an empty table with terminals in the header row and nonterminals in the header column.
            List<string> headerColumn = new();
            {
                StringBuilder buffer = new();
                foreach (var (term, rule) in grammar)
                {
                    headerColumn.Add(term);
                    foreach (var grammarRule in rule)
                    {
                        for (int i = 0; i < grammarRule.Length; i++)
                        {
                            if (grammarRule[i] == '\'')
                            {
                                Seek(grammarRule, ref buffer, '\'', '\'', ref i);
                                var s = buffer.ToString();
                                if (!headerRow.Contains(s))
                                    headerRow.Add(s);
                                buffer.Clear();
                            }
                            else if (grammarRule[i] == '$' && !headerRow.Contains("'$'"))
                                headerRow.Add("'$'");
                        }
                    }
                }
            }
            predictiveAnalysisTable.Columns.AddRange(headerRow.Select(r => new DataColumn(r)).ToArray());
            foreach (var nonterminal in headerColumn)
            {
                List<string> yetAnotherHeaderRow = new()
                {
                    nonterminal
                };
                yetAnotherHeaderRow.AddRange(Enumerable.Repeat("", headerRow.Count - 1));
                predictiveAnalysisTable.Rows.Add(yetAnotherHeaderRow.ToArray());
            }

            // Adds synch symbols.
            foreach (var (term, _) in grammar)
            {
                foreach (DataRow row in predictiveAnalysisTable.Rows)
                    if (row.Field<string>("Nonterminals") == term)
                    {
                        foreach (var elementFromFOLLOW in FOLLOW[term])
                        {
                            row[elementFromFOLLOW] = "Synch";
                        }
                    }
            }

            // Fills in the table.
            foreach (var (term, rule) in grammar)
            {
                foreach (string grammarRule in rule)
                {
                    GrammarDict constructedFIRSTforProduction = this.ConstructFIRST(grammar, new() { grammarRule }, "test", new());
                    foreach (var terminal in constructedFIRSTforProduction["test"])
                    {
                        if (terminal == "ε")
                            continue;
                        foreach (DataRow row in predictiveAnalysisTable.Rows)
                            if (row.Field<string>("Nonterminals") == term)
                                row[terminal] = grammarRule;
                    }
                    if (constructedFIRSTforProduction["test"].Contains("ε"))
                        foreach (DataRow row in predictiveAnalysisTable.Rows)
                            if (row.Field<string>("Nonterminals") == term)
                                foreach (var terminalFromFOLLOW in FOLLOW[term])
                                    row[terminalFromFOLLOW] = grammarRule;
                }
            }

            return predictiveAnalysisTable;
        }

        public void TextCorrectnessVerification(DataTable predictiveAnalysisTable, GrammarDict FIRST,
            GrammarDict FOLLOW, string startNonterminal, string text, int[] quantityOfSymbolsInEachLine)
        {
            List<KeyValuePair<int, string>> errorMessages = new();
            int indexOfCharacterInInitialText = 0;

            DataTable analysisResultsTable = new();
            string[] infoRow = new string[3] { "Stack", "Input", "Remark" };
            ArrayList changedStack = new();

            analysisResultsTable.Columns.AddRange(infoRow.Select(r => new DataColumn(r)).ToArray());
            Array.Clear(infoRow, 0, infoRow.Length);

            text = text.Replace("\r\n", " ") + "$";
            string initialText = text;
            Stack<string> stack = new();
            stack.Push("$");
            stack.Push(startNonterminal);
            infoRow[0] = $"${startNonterminal}";
            infoRow[1] = text;
            infoRow[2] = "start";
            changedStack.Add("$");
            List<string> terminalsFromTable = new();
            foreach (DataColumn terminal in predictiveAnalysisTable.Columns)
                if (terminal.ColumnName != "Nonterminals")
                    terminalsFromTable.Add(terminal.ColumnName);

            try
            {
                while (true)
                {
                    string currentValueInStack = stack.Pop();
                    if (infoRow[2] != "start")
                    {
                        Array.Clear(infoRow, 0, infoRow.Length);
                        infoRow[0] = string.Join("", changedStack.ToArray().Select(x => x.ToString()).ToArray())
                            + currentValueInStack;
                        infoRow[1] = text;
                    }

                    infoRow[2] = "";

                    if (currentValueInStack != "$")
                    {
                        int lastIndexOfTerminalInText = -1;
                        List<string> variantsOfTerminals = new();
                        foreach (var terminalFromTable in terminalsFromTable)
                        {
                            for (int j = 0; j < terminalFromTable.Length - 2; j++)
                            {
                                if (text[j] == terminalFromTable[j + 1])
                                {
                                    lastIndexOfTerminalInText = j;
                                }
                                else
                                {
                                    lastIndexOfTerminalInText = -1;
                                    break;
                                }
                            }
                            if (lastIndexOfTerminalInText != -1)
                            {
                                variantsOfTerminals.Add(terminalFromTable);
                            }
                        }
                        if (variantsOfTerminals.Count > 0)
                            lastIndexOfTerminalInText = variantsOfTerminals.
                                Aggregate((max, cur) => max.Length > cur.Length ? max : cur).Count() - 3;
                        if (lastIndexOfTerminalInText == -1)
                        {
                            infoRow[2] = $"Error, remove {currentValueInStack} from the top of the stack and skip <<{text[0]}>>";
                            analysisResultsTable.Rows.Add(infoRow);

                            errorMessages.Add(new(indexOfCharacterInInitialText, $"Invalid character '{text[0]}'"));
                            indexOfCharacterInInitialText += 1;

                            text = text.Remove(0, 1);
                            changedStack.RemoveAt(changedStack.Count - 1);

                            continue;
                            //throw new ArgumentException("Error in the text (3)");
                        }
                        else
                        {
                            lastIndexOfTerminalInText++;
                        }
                        if (currentValueInStack == $"\'{text[..lastIndexOfTerminalInText]}\'")
                        {
                            text = text.Remove(0, lastIndexOfTerminalInText);
                            analysisResultsTable.Rows.Add(infoRow);
                            changedStack.RemoveAt(changedStack.Count - 1);

                            indexOfCharacterInInitialText += lastIndexOfTerminalInText;
                        }
                        else
                        {
                            bool isCurrentValueInStackNonterminal = false;
                            foreach (DataRow row in predictiveAnalysisTable.Rows)
                            {
                                if (row.Field<string>("Nonterminals") == currentValueInStack)
                                {
                                    isCurrentValueInStackNonterminal = true;

                                    string valueFromTable = (string)row[$"\'{text[..lastIndexOfTerminalInText]}\'"];
                                    if (valueFromTable != string.Empty && valueFromTable != "Synch")
                                    {
                                        if (valueFromTable != "$")
                                        {
                                            Stack<string> bufferForTextReverse = new();
                                            StringBuilder currentTerminalOrNonterminal = new();
                                            for (int j = 0; j < valueFromTable.Length; j++)
                                            {
                                                switch (valueFromTable[j])
                                                {
                                                    case '\'':
                                                        Seek(valueFromTable, ref currentTerminalOrNonterminal, '\'', '\'', ref j);
                                                        bufferForTextReverse.Push(currentTerminalOrNonterminal.ToString());
                                                        currentTerminalOrNonterminal.Clear();
                                                        break;
                                                    case '<':
                                                        Seek(valueFromTable, ref currentTerminalOrNonterminal, '<', '>', ref j);
                                                        bufferForTextReverse.Push(currentTerminalOrNonterminal.ToString());
                                                        currentTerminalOrNonterminal.Clear();
                                                        break;
                                                    default:
                                                        break;
                                                }
                                            }
                                            while (bufferForTextReverse.Count != 0)
                                            {
                                                string element = bufferForTextReverse.Pop();
                                                if (bufferForTextReverse.Count > 0)
                                                    changedStack.Add(element);
                                                stack.Push(element);
                                            }
                                            analysisResultsTable.Rows.Add(infoRow);
                                        }
                                        else
                                        {
                                            analysisResultsTable.Rows.Add(infoRow);
                                            changedStack.RemoveAt(changedStack.Count - 1);
                                        }
                                    }
                                    else if (valueFromTable == "Synch")
                                    {
                                        if (stack.Count < 3)
                                        {
                                            infoRow[2] = $"Error, skip <<{text[..lastIndexOfTerminalInText]}>>";
                                            analysisResultsTable.Rows.Add(infoRow);

                                            errorMessages.Add(new(indexOfCharacterInInitialText, $"Can't recognize the word! {text[..lastIndexOfTerminalInText]} was skipped"));
                                            indexOfCharacterInInitialText += lastIndexOfTerminalInText;

                                            text = text.Remove(0, lastIndexOfTerminalInText);
                                            stack.Push(currentValueInStack);
                                        }
                                        else
                                        {
                                            infoRow[2] = $"Error, PredictiveAnalysisTable[{currentValueInStack}, {text[..lastIndexOfTerminalInText]}] = Synch";
                                            analysisResultsTable.Rows.Add(infoRow);

                                            changedStack.RemoveAt(changedStack.Count - 1);

                                            errorMessages.Add(new(indexOfCharacterInInitialText, $"Invalid characters '{text[..lastIndexOfTerminalInText]}'"));
                                        }
                                    }
                                    else if (string.Empty == valueFromTable)
                                    {
                                        if (text.Length > 1)
                                        {
                                            infoRow[2] = $"Error, skip <<{text[..lastIndexOfTerminalInText]}>>";
                                            analysisResultsTable.Rows.Add(infoRow);

                                            errorMessages.Add(new(indexOfCharacterInInitialText, $"Can't recognize the word! {text[..lastIndexOfTerminalInText]} was skipped"));
                                            indexOfCharacterInInitialText += lastIndexOfTerminalInText;

                                            text = text.Remove(0, lastIndexOfTerminalInText);
                                            stack.Push(currentValueInStack);
                                        }
                                        else
                                        {
                                            throw new ArgumentException("Error in the text (5)");
                                        }
                                    }
                                    else
                                    {
                                        throw new ArgumentException("Error in the text (2)");
                                    }

                                    break;
                                }
                            }
                            if (!isCurrentValueInStackNonterminal)
                            {
                                infoRow[2] = $"Error, remove {currentValueInStack} from the top of the stack";
                                analysisResultsTable.Rows.Add(infoRow);

                                errorMessages.Add(new(indexOfCharacterInInitialText, $"Invalid character! {currentValueInStack} was expected"));
                                indexOfCharacterInInitialText += lastIndexOfTerminalInText;

                                changedStack.RemoveAt(changedStack.Count - 1);
                            }
                        }
                    }
                    else if (text == "$")
                    {
                        analysisResultsTable.Rows.Add(infoRow);
                        PrintTableOrView(analysisResultsTable, "Result Table");
                        break;
                    }
                    else
                    {
                        infoRow[2] = $"Error, skip <<{text}>>";

                        analysisResultsTable.Rows.Add(infoRow);
                        PrintTableOrView(analysisResultsTable, "Result Table");
                        errorMessages.Add(new(indexOfCharacterInInitialText, "Unexpected end of the text"));
                        break;
                    }

                }

                if (errorMessages.Count == 0)
                {
                    Console.WriteLine("Error(s) not found!\nCongratulations!");
                }
                else
                {
                    Console.WriteLine("Error(s) found!");

                    var orderedErrorList = errorMessages.OrderBy(x => x.Key).ToList();
                    DataTable errorTable = new();
                    string[] errorTableRow = { "Line", "Column", "Error Description" };
                    errorTable.Columns.AddRange(errorTableRow.Select(r => new DataColumn(r)).ToArray());

                    int endIndexOfCurrentLine = quantityOfSymbolsInEachLine[0];
                    foreach (var orderedErrorListRecord in orderedErrorList)
                    {
                        for (int i = 0; i < quantityOfSymbolsInEachLine.Length; i++)
                        {
                            if (orderedErrorListRecord.Key < endIndexOfCurrentLine + 1)
                            {
                                int currentCharNumber = orderedErrorListRecord.Key - (endIndexOfCurrentLine - (quantityOfSymbolsInEachLine[i] + 1));
                                errorTable.Rows.Add((i + 1).ToString(), currentCharNumber.ToString(), orderedErrorListRecord.Value);

                                endIndexOfCurrentLine = quantityOfSymbolsInEachLine[0];
                                break;
                            }
                            else
                            {
                                if (i + 1 < quantityOfSymbolsInEachLine.Length)
                                    endIndexOfCurrentLine += quantityOfSymbolsInEachLine[i + 1] + 1;
                            }
                        }
                    }
                    errorTable.Rows.Add("---", "Total:", orderedErrorList.Count.ToString());
                    PrintTableOrView(errorTable, "Table Of Errors");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static private void PrintTableOrView(DataTable table, string label)
        {
            Console.WriteLine("\n" + label);
            foreach (DataColumn coloumn in table.Columns)
            {
                Console.Write(coloumn.ColumnName);
                Console.Write("\t");
            }
            Console.WriteLine();
            foreach (DataRow row in table.Rows)
            {
                foreach (var item in row.ItemArray)
                {
                    Console.Write(item);
                    Console.Write("\t");
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }
    }


}
