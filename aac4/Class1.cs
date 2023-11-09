using Microsoft.Win32;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;

namespace TA4
{

    using Grammar = Dictionary<string, List<string>>;

    public class BrokenFileException : Exception
    {
        public BrokenFileException() : base("The file is broken!") { }
    }

    public class DefaultDialogService
    {

        public string FilePath { get; set; }
        public string BuildGrammarDictionary(string[] rules, out Grammar grammar)
        {
            grammar = new Grammar();

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
            //grammar.Add("<__>", new List<string> { "$"});
            //foreach (var (k, v) in grammar)
            //{
            //    Console.WriteLine("'" + k + "'");
            //    foreach (var s in v)
            //    {
            //        Console.WriteLine(s);
            //    }
            //}
            listOfReplacementsForNonterminal.Clear();

            return Regex.Replace(rules[0], @"(?<nonterminal><.*?>):\s.*",
                        "${nonterminal}");
        }

        public Grammar ConstructFIRST(Grammar grammar, List<string> grammarRules, string currentNonterminal, Grammar oldFIRST)
        {
            Grammar newFIRST = new(oldFIRST);

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
                            currentSentence.Append("<");
                            while (grammarRule[++i] != '>')
                                currentSentence.Append(grammarRule[i]);
                            currentSentence.Append(">");
                            break;
                        case '\'':
                            currentSentence.Append("'");
                            while (grammarRule[++i] != '\'')
                                currentSentence.Append(grammarRule[i]);
                            currentSentence.Append("'");
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

        public Grammar ConstructFOLLOW(Grammar FIRST, string startNonterminal, Grammar grammar)
        {
            Dictionary<string, List<string>> newFOLLOW = new();
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
                                Dictionary<string, List<string>> temporaryFIRST = new Dictionary<string, List<string>>();
                                List<string> betaList = new List<string>
                                {
                                    beta
                                };

                                bool doesTemporaryFirstContainEpsilon = false;

                                temporaryFIRST = ConstructFIRST(grammar, betaList, "beta", temporaryFIRST);
                                if (temporaryFIRST["beta"].Contains("ε"))
                                    doesTemporaryFirstContainEpsilon = true;
                                temporaryFIRST["beta"] = new List<string>((temporaryFIRST["beta"]).Except(newFOLLOW[B]));
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
                            List<string> rangeToAdd = new((newFOLLOW[grammarRules.Key]).Except(newFOLLOW[B]));
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

        public DataTable GeneratePredictiveAnalysisTable(Grammar grammar, Grammar FIRST, Grammar FOLLOW)
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
                foreach (var grammarRules in grammar)
                {
                    headerColumn.Add(grammarRules.Key);
                    foreach (var grammarRule in grammarRules.Value)
                    {
                        for (int i = 0; i < grammarRule.Count(); i++)
                        {
                            if (grammarRule[i] == '\'')
                            {
                                i++;
                                buffer.Append('\'');
                                while (grammarRule[i] != '\'')
                                {
                                    buffer.Append(grammarRule[i]);
                                    i++;
                                }
                                buffer.Append('\'');
                                if (!headerRow.Contains(buffer.ToString()))
                                    headerRow.Add(buffer.ToString());
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
                for (int i = 1; i < headerRow.Count; i++)
                    yetAnotherHeaderRow.Add("");
                predictiveAnalysisTable.Rows.Add(yetAnotherHeaderRow.ToArray());
            }

            // Adds synch symbols.
            foreach (var grammarRules in grammar)
            {
                for (int i = 0; i < predictiveAnalysisTable.Rows.Count; i++)
                    if (predictiveAnalysisTable.Rows[i].Field<string>("Nonterminals").Equals(grammarRules.Key))
                    {
                        foreach (var elementFromFOLLOW in FOLLOW[grammarRules.Key])
                        {
                            predictiveAnalysisTable.Rows[i][elementFromFOLLOW] = "Synch";
                        }
                    }
            }

            // Fills in the table.
            foreach (var grammarRules in grammar)
            {
                foreach (var grammarRule in grammarRules.Value)
                {
                    Grammar constructedFIRSTforProduction = new();
                    List<string> listForCurrentGrammarRule = new()
                    {
                        grammarRule
                    };
                    constructedFIRSTforProduction = this.ConstructFIRST(grammar, listForCurrentGrammarRule,
                        "test", constructedFIRSTforProduction);
                    foreach (var terminal in constructedFIRSTforProduction["test"])
                    {
                        if (!terminal.Equals("ε"))
                            for (int i = 0; i < predictiveAnalysisTable.Rows.Count; i++)
                                if (predictiveAnalysisTable.Rows[i].Field<string>("Nonterminals").Equals(grammarRules.Key))
                                    predictiveAnalysisTable.Rows[i][terminal]
                                        = grammarRule;
                    }
                    if (constructedFIRSTforProduction["test"].Contains("ε"))
                        for (int i = 0; i < predictiveAnalysisTable.Rows.Count; i++)
                            if (predictiveAnalysisTable.Rows[i].Field<string>("Nonterminals").Equals(grammarRules.Key))
                                foreach (var terminalFromFOLLOW in FOLLOW[grammarRules.Key])
                                    predictiveAnalysisTable.Rows[i][terminalFromFOLLOW]
                                        = grammarRule;
                }
            }

            return predictiveAnalysisTable;
        }

        public void TextCorrectnessVerification(DataTable predictiveAnalysisTable, Grammar FIRST,
            Grammar FOLLOW, string startNonterminal, string text, int[] quantityOfSymbolsInEachLine)
        {
            List<KeyValuePair<int, string>> errorMessages = new();
            int indexOfCharacterInInitialText = 0;

            DataTable analysisResultsTable = new();
            string[] yetAnotherRow = new string[3] { "Stack", "Input", "Remark" };
            ArrayList changedStack = new();

            analysisResultsTable.Columns.AddRange(yetAnotherRow.Select(r => new DataColumn(r)).ToArray());
            Array.Clear(yetAnotherRow, 0, yetAnotherRow.Length);

            text = text.Replace("\r\n", " ") + "$";
            string initialText = text;
            Stack<string> stack = new();
            stack.Push("$");
            stack.Push(startNonterminal);
            yetAnotherRow[0] = "$" + startNonterminal;
            yetAnotherRow[1] = text;
            yetAnotherRow[2] = "start";
            changedStack.Add("$");
            List<string> terminalsFromTable = new();
            foreach (DataColumn terminal in predictiveAnalysisTable.Columns)
                if (!terminal.ColumnName.Equals("Nonterminals"))
                    terminalsFromTable.Add(terminal.ColumnName);
            List<string> nonterminalsFromTable = new();
            for (int i = 0; i < predictiveAnalysisTable.Rows.Count; i++)
                nonterminalsFromTable.Add((string)predictiveAnalysisTable.Rows[i][0]);

            try
            {
                while (true)
                {
                    string currentValueInStack = stack.Pop();
                    if (!yetAnotherRow[2].Equals("start"))
                    {
                        Array.Clear(yetAnotherRow, 0, yetAnotherRow.Length);
                        yetAnotherRow[0] = string.Join("", changedStack.ToArray().Select(x => x.ToString()).ToArray())
                            + currentValueInStack;
                        yetAnotherRow[1] = text;
                        yetAnotherRow[2] = "";
                    }
                    else
                    {
                        yetAnotherRow[2] = "";
                    }
                    if (!currentValueInStack.Equals("$"))
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
                            yetAnotherRow[2] = "Error, remove " + currentValueInStack +
                                " from the top of the stack and skip <<" + text[0] + ">>";
                            analysisResultsTable.Rows.Add(yetAnotherRow);

                            errorMessages.Add(new KeyValuePair<int, string>(indexOfCharacterInInitialText,
                                "Invalid character '" + text[0] + "'"));
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
                        if (currentValueInStack.Equals('\'' + text.Substring(0, lastIndexOfTerminalInText) + '\''))
                        {
                            text = text.Remove(0, lastIndexOfTerminalInText);
                            analysisResultsTable.Rows.Add(yetAnotherRow);
                            changedStack.RemoveAt(changedStack.Count - 1);

                            indexOfCharacterInInitialText += lastIndexOfTerminalInText;
                        }
                        else
                        {
                            bool isCurrentValueInStackNonterminal = false;
                            for (int i = 0; i < predictiveAnalysisTable.Rows.Count; i++)
                            {
                                if (predictiveAnalysisTable.Rows[i].Field<string>("Nonterminals").Equals(currentValueInStack))
                                {
                                    isCurrentValueInStackNonterminal = true;

                                    string valueFromTable = (string)predictiveAnalysisTable.
                                        Rows[i]['\'' + text.Substring(0, lastIndexOfTerminalInText) + '\''];
                                    if (!valueFromTable.Equals("") && !valueFromTable.Equals("Synch"))
                                    {
                                        if (!valueFromTable.Equals("$"))
                                        {
                                            Stack<string> bufferForTextReverse = new();
                                            StringBuilder currentTerminalOrNonterminal = new();
                                            for (int j = 0; j < valueFromTable.Length; j++)
                                            {
                                                if (valueFromTable[j] == '\'')
                                                {
                                                    currentTerminalOrNonterminal.Append(valueFromTable[j++]);
                                                    while (valueFromTable[j] != '\'')
                                                        currentTerminalOrNonterminal.Append(valueFromTable[j++]);
                                                    currentTerminalOrNonterminal.Append(valueFromTable[j]);
                                                    bufferForTextReverse.Push(currentTerminalOrNonterminal.ToString());
                                                    currentTerminalOrNonterminal.Clear();
                                                }
                                                else if (valueFromTable[j] == '<')
                                                {
                                                    currentTerminalOrNonterminal.Append(valueFromTable[j++]);
                                                    while (valueFromTable[j] != '>')
                                                        currentTerminalOrNonterminal.Append(valueFromTable[j++]);
                                                    currentTerminalOrNonterminal.Append(valueFromTable[j]);
                                                    bufferForTextReverse.Push(currentTerminalOrNonterminal.ToString());
                                                    currentTerminalOrNonterminal.Clear();
                                                }
                                            }
                                            while (bufferForTextReverse.Count != 0)
                                            {
                                                string popedBufferElement = bufferForTextReverse.Pop();
                                                if (bufferForTextReverse.Count > 0)
                                                    changedStack.Add(popedBufferElement);
                                                stack.Push(popedBufferElement);
                                            }
                                            analysisResultsTable.Rows.Add(yetAnotherRow);
                                        }
                                        else
                                        {
                                            analysisResultsTable.Rows.Add(yetAnotherRow);
                                            changedStack.RemoveAt(changedStack.Count - 1);
                                        }
                                    }
                                    else if (valueFromTable.Equals("Synch"))
                                    {
                                        if (stack.Count < 3)
                                        {
                                            yetAnotherRow[2] = "Error, skip <<" + text.Substring(0, lastIndexOfTerminalInText) + ">>";
                                            analysisResultsTable.Rows.Add(yetAnotherRow);

                                            errorMessages.Add(new KeyValuePair<int, string>(indexOfCharacterInInitialText,
                                                "Can't recognize the word! " + text.Substring(0, lastIndexOfTerminalInText) + "was skipped"));
                                            indexOfCharacterInInitialText += lastIndexOfTerminalInText;

                                            text = text.Remove(0, lastIndexOfTerminalInText);
                                            stack.Push(currentValueInStack);
                                        }
                                        else
                                        {
                                            yetAnotherRow[2] = "Error, PredictiveAnalysisTable[" + currentValueInStack + ", " +
                                                text.Substring(0, lastIndexOfTerminalInText) + "] = Synch";
                                            analysisResultsTable.Rows.Add(yetAnotherRow);

                                            changedStack.RemoveAt(changedStack.Count - 1);

                                            errorMessages.Add(new KeyValuePair<int, string>(indexOfCharacterInInitialText,
                                                "Invalid characters '" + text.Substring(0, lastIndexOfTerminalInText) + "'"));
                                        }
                                    }
                                    else if (valueFromTable.Equals(""))
                                    {
                                        if (text.Count() > 1)
                                        {
                                            yetAnotherRow[2] = "Error, skip <<" + text.Substring(0, lastIndexOfTerminalInText) + ">>";
                                            analysisResultsTable.Rows.Add(yetAnotherRow);

                                            errorMessages.Add(new KeyValuePair<int, string>(indexOfCharacterInInitialText,
                                                "Can't recognize the word! " + text.Substring(0, lastIndexOfTerminalInText) + " was skipped"));
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
                                yetAnotherRow[2] = "Error, remove " + currentValueInStack + " from the top of the stack";
                                analysisResultsTable.Rows.Add(yetAnotherRow);

                                errorMessages.Add(new KeyValuePair<int, string>(indexOfCharacterInInitialText,
                                                "Invalid character! " + currentValueInStack + " was expected"));
                                indexOfCharacterInInitialText += lastIndexOfTerminalInText;

                                changedStack.RemoveAt(changedStack.Count - 1);
                            }
                        }
                    }
                    else
                    {
                        if (text.Equals("$"))
                        {
                            analysisResultsTable.Rows.Add(yetAnotherRow);
                            PrintTableOrView(analysisResultsTable, "Result Table");
                            break;
                        }
                        else
                        {
                            yetAnotherRow[2] = "Error, skip <<" + text + ">>";

                            analysisResultsTable.Rows.Add(yetAnotherRow);
                            PrintTableOrView(analysisResultsTable, "Result Table");
                            errorMessages.Add(new KeyValuePair<int, string>(indexOfCharacterInInitialText, "Unexpected end of the text"));
                            break;
                        }
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
                        for (int i = 0; i < quantityOfSymbolsInEachLine.Count(); i++)
                        {
                            if (orderedErrorListRecord.Key < endIndexOfCurrentLine + 1)
                            {
                                int currentCharNumber =
                                    orderedErrorListRecord.Key - (endIndexOfCurrentLine - (quantityOfSymbolsInEachLine[i] + 1));
                                errorTable.Rows.Add((i + 1).ToString(), currentCharNumber.ToString(), orderedErrorListRecord.Value);

                                endIndexOfCurrentLine = quantityOfSymbolsInEachLine[0];
                                break;
                            }
                            else
                            {
                                if (i + 1 < quantityOfSymbolsInEachLine.Count())
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
            for (int i = 0; i < table.Columns.Count; i++)
            {
                Console.Write(table.Columns[i].ColumnName);
                Console.Write("\t");
            }
            Console.WriteLine();
            for (int i = 0; i < table.Rows.Count; i++)
            {
                var row = table.Rows[i];
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
