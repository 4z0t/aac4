using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace aac4
{
    class ErrorCorrector
    {
        public ErrorCorrector(DataTable pat)
        {
            _predictiveAnalysisTable = pat;
        }

        public struct AnalysisResult
        {
            public DataTable? result;
            public DataTable? errors;
        }

        private int[] CountLinesInText(string[] programText)
        {
            int[] lengths = new int[programText.Length];
            for (int i = 0; i < programText.Length; i++)
                lengths[i] = programText[i].Length;
            return lengths;
        }

        public AnalysisResult CheckText(string startNonterminal, string programText)
        {
            int[] lengthsOfLines = CountLinesInText(programText.Replace("\r\n", "\n").Split('\n'));

            List<KeyValuePair<int, string>> errorMessages = new();
            int indexOfCharacterInInitialText = 0;

            DataTable analysisResultsTable = new();
            string[] infoRow = new string[3] { "Stack", "Input", "Remark" };
            ArrayList changedStack = new();

            analysisResultsTable.Columns.AddRange(infoRow.Select(r => new DataColumn(r)).ToArray());
            Array.Clear(infoRow, 0, infoRow.Length);

            programText = programText.Replace("\r\n", " ") + "$";
            string initialText = programText;
            Stack<string> stack = new();
            stack.Push("$");
            stack.Push(startNonterminal);
            infoRow[0] = $"${startNonterminal}";
            infoRow[1] = programText;
            infoRow[2] = "start";
            changedStack.Add("$");
            List<string> terminalsFromTable = new();
            foreach (DataColumn terminal in _predictiveAnalysisTable.Columns)
            {
                if (terminal.ColumnName != "Nonterminals")
                {
                    terminalsFromTable.Add(terminal.ColumnName);
                }
            }

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
                        infoRow[1] = programText;
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
                                if (programText[j] == terminalFromTable[j + 1])
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
                            lastIndexOfTerminalInText = variantsOfTerminals.Aggregate((max, cur) => max.Length > cur.Length ? max : cur).Count() - 3;
                        if (lastIndexOfTerminalInText == -1)
                        {
                            infoRow[2] = $"Error, remove {currentValueInStack} from the top of the stack and skip <<{programText[0]}>>";
                            analysisResultsTable.Rows.Add(infoRow);

                            errorMessages.Add(new(indexOfCharacterInInitialText, $"Invalid character '{programText[0]}'"));
                            indexOfCharacterInInitialText += 1;

                            programText = programText.Remove(0, 1);
                            changedStack.RemoveAt(changedStack.Count - 1);

                            continue;
                            //throw new ArgumentException("Error in the text (3)");
                        }
                        else
                        {
                            lastIndexOfTerminalInText++;
                        }
                        if (currentValueInStack == $"\'{programText[..lastIndexOfTerminalInText]}\'")
                        {
                            programText = programText.Remove(0, lastIndexOfTerminalInText);
                            analysisResultsTable.Rows.Add(infoRow);
                            changedStack.RemoveAt(changedStack.Count - 1);

                            indexOfCharacterInInitialText += lastIndexOfTerminalInText;
                        }
                        else
                        {
                            bool isCurrentValueInStackNonterminal = false;
                            foreach (DataRow row in _predictiveAnalysisTable.Rows)
                            {
                                if (row.Field<string>("Nonterminals") == currentValueInStack)
                                {
                                    isCurrentValueInStackNonterminal = true;

                                    string valueFromTable = (string)row[$"\'{programText[..lastIndexOfTerminalInText]}\'"];
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
                                                        Utility.Seek(valueFromTable, ref currentTerminalOrNonterminal, '\'', '\'', ref j);
                                                        bufferForTextReverse.Push(currentTerminalOrNonterminal.ToString());
                                                        currentTerminalOrNonterminal.Clear();
                                                        break;
                                                    case '<':
                                                        Utility.Seek(valueFromTable, ref currentTerminalOrNonterminal, '<', '>', ref j);
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
                                            infoRow[2] = $"Error, skip <<{programText[..lastIndexOfTerminalInText]}>>";
                                            analysisResultsTable.Rows.Add(infoRow);

                                            errorMessages.Add(new(indexOfCharacterInInitialText, $"Can't recognize the word! {programText[..lastIndexOfTerminalInText]} was skipped"));
                                            indexOfCharacterInInitialText += lastIndexOfTerminalInText;

                                            programText = programText.Remove(0, lastIndexOfTerminalInText);
                                            stack.Push(currentValueInStack);
                                        }
                                        else
                                        {
                                            infoRow[2] = $"Error, PredictiveAnalysisTable[{currentValueInStack}, {programText[..lastIndexOfTerminalInText]}] = Synch";
                                            analysisResultsTable.Rows.Add(infoRow);

                                            changedStack.RemoveAt(changedStack.Count - 1);

                                            errorMessages.Add(new(indexOfCharacterInInitialText, $"Invalid characters '{programText[..lastIndexOfTerminalInText]}'"));
                                        }
                                    }
                                    else if (string.Empty == valueFromTable)
                                    {
                                        if (programText.Length > 1)
                                        {
                                            infoRow[2] = $"Error, skip <<{programText[..lastIndexOfTerminalInText]}>>";
                                            analysisResultsTable.Rows.Add(infoRow);

                                            errorMessages.Add(new(indexOfCharacterInInitialText, $"Can't recognize the word! {programText[..lastIndexOfTerminalInText]} was skipped"));
                                            indexOfCharacterInInitialText += lastIndexOfTerminalInText;

                                            programText = programText.Remove(0, lastIndexOfTerminalInText);
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
                    else if (programText == "$")
                    {
                        analysisResultsTable.Rows.Add(infoRow);
                        Utility.PrintTableOrView(analysisResultsTable, "Result Table");
                        break;
                    }
                    else
                    {
                        infoRow[2] = $"Error, skip <<{programText}>>";

                        analysisResultsTable.Rows.Add(infoRow);
                        Utility.PrintTableOrView(analysisResultsTable, "Result Table");
                        errorMessages.Add(new(indexOfCharacterInInitialText, "Unexpected end of the text"));
                        break;
                    }

                }

                if (errorMessages.Count == 0)
                {
                    //Console.WriteLine("Error(s) not found!\nCongratulations!");
                    return new()
                    {
                        result = analysisResultsTable,
                        errors = null
                    };
                }
                else
                {
                    //Console.WriteLine("Error(s) found!");

                    var orderedErrorList = errorMessages.OrderBy(x => x.Key).ToList();
                    DataTable errorTable = new();
                    string[] errorTableRow = { "Line", "Column", "Error Description" };
                    errorTable.Columns.AddRange(errorTableRow.Select(r => new DataColumn(r)).ToArray());

                    int endIndexOfCurrentLine = lengthsOfLines[0];
                    foreach (var orderedErrorListRecord in orderedErrorList)
                    {
                        for (int i = 0; i < lengthsOfLines.Length; i++)
                        {
                            if (orderedErrorListRecord.Key < endIndexOfCurrentLine + 1)
                            {
                                int currentCharNumber = orderedErrorListRecord.Key - (endIndexOfCurrentLine - (lengthsOfLines[i] + 1));
                                errorTable.Rows.Add((i + 1).ToString(), currentCharNumber.ToString(), orderedErrorListRecord.Value);

                                endIndexOfCurrentLine = lengthsOfLines[0];
                                break;
                            }
                            else if (i + 1 < lengthsOfLines.Length)
                            {
                                endIndexOfCurrentLine += lengthsOfLines[i + 1] + 1;
                            }
                        }
                    }
                    errorTable.Rows.Add("---", "Total:", orderedErrorList.Count.ToString());
                    return new()
                    {
                        result = analysisResultsTable,
                        errors = errorTable
                    };
                    //Utility.PrintTableOrView(errorTable, "Table Of Errors");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        DataTable _predictiveAnalysisTable;
    }
}
