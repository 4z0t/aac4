using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;


namespace aac4
{
    using Rules = Dictionary<string, List<string>>;

    class Grammar
    {
        public Grammar(string[] rules)
        {
            _rules = new();
            _first = new();
            _follow = new();

            ParseRules(rules);
        }

        public Rules Rules { get { return _rules; } }

        public Rules First { get => _first; }
        public Rules Follow { get => _follow; }
        public string StartNonTerm { get { return _startNonTerm; } }

        private static Regex ruleRegex = new(@"(<.*?>):\s(.*)");
        private static Regex ruleNextRegex = new(@"\t\|\s(.*)");

        private void ParseRules(string[] rules)
        {
            if (rules.Length == 0)
                return;

            string nonterminal = string.Empty;
            List<string> replacementsForNonterminal = new();
            StringBuilder subline = new();

            foreach (string rule in rules)
            {
                string replacementForNonTerm;
                Match match = ruleRegex.Match(rule);
                if (match.Success)
                {
                    if (nonterminal != string.Empty)
                    {
                        replacementsForNonterminal.Add(subline.ToString());
                        subline.Clear();
                        _rules.Add(nonterminal, new List<string>(replacementsForNonterminal));
                        replacementsForNonterminal.Clear();
                    }
                    nonterminal = match.Groups[1].Value;
                    replacementForNonTerm = match.Groups[2].Value;
                }
                else
                {
                    match = ruleNextRegex.Match(rule);
                    if (match.Success)
                    {
                        if (subline.Length != 0)
                        {
                            replacementsForNonterminal.Add(subline.ToString());
                            subline.Clear();
                        }
                        replacementForNonTerm = match.Groups[1].Value;
                    }
                    else
                    {
                        throw new BrokenFileException();
                    }
                }

                if (replacementForNonTerm == string.Empty)
                {
                    replacementsForNonterminal.Add("$");
                }
                else
                {
                    string replacement = replacementForNonTerm;
                    for (int i = 0; i < replacement.Length; i++)
                    {
                        switch (replacement[i])
                        {
                            case '<':
                                subline.Append(replacement[i++]);
                                if (i >= replacement.Length)
                                    throw new ArgumentException();
                                if (replacement[i] != '>')
                                    do
                                    {
                                        if (i >= replacement.Length)
                                            throw new BrokenFileException();
                                        subline.Append(replacement[i]);
                                    } while (replacement[i++] != '>');
                                else
                                    throw new BrokenFileException();
                                i--;
                                break;

                            case '\'':
                                subline.Append(replacement[i++]);
                                if (i >= replacement.Length)
                                    throw new BrokenFileException();
                                if (replacement[i] != '\'')
                                    do
                                    {
                                        if (i >= replacement.Length)
                                            throw new BrokenFileException();
                                        subline.Append(replacement[i]);
                                    } while (replacement[i++] != '\'');
                                else
                                    throw new BrokenFileException();
                                i--;
                                break;

                            case ' ':
                                if (i + 1 < replacement.Length)
                                {
                                    if (replacement[i + 1] != ' ' &&
                                        replacement[i + 1] != '|')
                                    {
                                        //subline.Append("<__>");
                                        subline.Append("' '");
                                    }
                                    else if (i + 3 < replacement.Length)
                                    {
                                        if (replacement[++i] == '|' &&
                                           replacement[++i] == ' ')
                                        {
                                            replacementsForNonterminal.Add(subline.ToString());
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
            }

            replacementsForNonterminal.Add(subline.ToString());

            _rules.Add(nonterminal, new List<string>(replacementsForNonterminal));

            //foreach (var (k, v) in _rules)
            //{
            //    Console.WriteLine("'" + k + "'");
            //    foreach (var s in v)
            //    {
            //        Console.WriteLine(s);
            //    }
            //}

            _startNonTerm = Regex.Replace(rules[0], @"(?<nonterminal><.*?>):\s.*", "${nonterminal}");
        }

        public void ConstructFIRST()
        {
            foreach (var (currentNonterminal, grammarRules) in _rules)
                _first = ContinueFIRST(_first, currentNonterminal, grammarRules);
        }

        private Rules ContinueFIRST(Rules oldFirst, string currentNonterminal, List<string> grammarRules)
        {
            Rules newFirst = new(oldFirst);

            List<string> terminals = new();
            StringBuilder currentSentence = new();
            bool isFirst = true;
            bool allPrevHaveEmptySymbol = false;
            foreach (var rule in grammarRules)
            {
                for (int i = 0; i < rule.Length; i++)
                {
                    switch (rule[i])
                    {
                        case '<':
                            Utility.Seek(rule, ref currentSentence, '<', '>', ref i);
                            break;
                        case '\'':
                            Utility.Seek(rule, ref currentSentence, '\'', '\'', ref i);
                            break;
                        case '$':
                            currentSentence.Append("ε");
                            break;
                    }
                    string nonTerm = currentSentence.ToString();

                    if (_rules.ContainsKey(nonTerm) && isFirst)
                    {
                        newFirst = this.ContinueFIRST(newFirst, nonTerm, _rules[nonTerm]);
                        if (newFirst[nonTerm].Contains("ε"))
                        {
                            allPrevHaveEmptySymbol = true;
                            newFirst[nonTerm].Remove("ε");
                        }
                        terminals.AddRange(newFirst[nonTerm].Except(terminals));
                    }
                    else if (_rules.ContainsKey(nonTerm) && !isFirst)
                    {
                        if (allPrevHaveEmptySymbol)
                        {
                            newFirst = this.ContinueFIRST(newFirst, nonTerm, _rules[nonTerm]);
                            if (!newFirst[nonTerm].Contains("ε") &&
                                allPrevHaveEmptySymbol)
                            {
                                allPrevHaveEmptySymbol = false;
                            }
                            else if (newFirst[nonTerm].Contains("ε") &&
                                !allPrevHaveEmptySymbol)
                            {
                                newFirst[nonTerm].Remove("ε");
                            }
                            terminals.AddRange(newFirst[nonTerm].Except(terminals));
                        }
                    }
                    else
                    {
                        if (nonTerm == "ε")
                        {
                            allPrevHaveEmptySymbol = true;
                        }
                        else
                        {
                            if (!terminals.Contains(nonTerm))
                                terminals.Add(nonTerm);
                            currentSentence.Clear();
                        }
                        break;
                    }
                    currentSentence.Clear();
                    isFirst = false;
                }
                if (allPrevHaveEmptySymbol)
                {
                    terminals.Add("ε");
                }
                currentSentence.Clear();
                allPrevHaveEmptySymbol = false;
            }

            if (!newFirst.ContainsKey(currentNonterminal))
                newFirst.Add(currentNonterminal, terminals);

            return newFirst;
        }

        public void ConstructFOLLOW()
        {
            if (_follow.Count != _rules.Count)
                foreach (var nonterminal in _rules.Keys)
                    _follow.Add(nonterminal, new List<string>());

            if (!_follow[_startNonTerm].Contains("$"))
                _follow[_startNonTerm].Add("'$'");

            bool hasChanged = true;
            while (hasChanged)
            {
                hasChanged = false;
                foreach (var grammarRules in _rules)
                {
                    foreach (string rule in grammarRules.Value)
                    {
                        string grammarRule = rule;
                        Match match;
                        while (true)
                        {
                            match = Regex.Match(grammarRule, @"^.*?(<.*?>).+$");
                            if (!match.Success)
                            {
                                break;
                            }

                            string B = match.Groups[1].Value;

                            string beta = rule.Remove(0, rule.IndexOf(B) + B.Length);
                            if (_first.ContainsKey(beta))
                            {
                                List<string> rangeToAdd = new(_first[beta].Where(x => x != "ε").Except(_follow[B]));
                                if (rangeToAdd.Count != 0)
                                {
                                    _follow[B].AddRange(rangeToAdd);
                                    hasChanged = true;
                                }

                                if (_first[beta].Contains("ε"))
                                {
                                    rangeToAdd = new List<string>(_follow[grammarRules.Key].Except(_follow[B]));
                                    if (rangeToAdd.Count != 0)
                                    {
                                        _follow[B].AddRange(rangeToAdd);
                                        hasChanged = true;
                                    }
                                }
                            }
                            else
                            {

                                var temporaryFIRST = ContinueFIRST(new(), "beta", new List<string> { beta });

                                bool containsEps = temporaryFIRST["beta"].Contains("ε");

                                temporaryFIRST["beta"] = new List<string>(temporaryFIRST["beta"].Except(_follow[B]));
                                if (temporaryFIRST["beta"].Count != 0)
                                {
                                    _follow[B].AddRange(temporaryFIRST["beta"]);
                                    hasChanged = true;
                                }

                                if (containsEps)
                                {
                                    List<string> rangeToAdd = new List<string>(_follow[grammarRules.Key].Except(_follow[B]));
                                    if (rangeToAdd.Count != 0)
                                    {
                                        _follow[B].AddRange(rangeToAdd);
                                        hasChanged = true;
                                    }
                                }
                            }
                            grammarRule = Regex.Replace(grammarRule, B, "");

                        }
                        match = Regex.Match(grammarRule, @"^.*(<.*?>)$");
                        if (match.Success)
                        {
                            string B = match.Groups[1].Value;
                            List<string> rangeToAdd = new(_follow[grammarRules.Key].Except(_follow[B]));
                            if (rangeToAdd.Count != 0)
                            {
                                _follow[B].AddRange(rangeToAdd);
                                hasChanged = true;
                            }
                        }
                    }
                }
            }
        }


        public DataTable MakePredictiveAnalysisTable()
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
                foreach (var (term, rule) in _rules)
                {
                    headerColumn.Add(term);
                    foreach (var grammarRule in rule)
                    {
                        for (int i = 0; i < grammarRule.Length; i++)
                        {
                            if (grammarRule[i] == '\'')
                            {
                                Utility.Seek(grammarRule, ref buffer, '\'', '\'', ref i);
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
            foreach (var (term, _) in _rules)
            {
                foreach (DataRow row in predictiveAnalysisTable.Rows)
                    if (row.Field<string>("Nonterminals") == term)
                    {
                        foreach (var elementFromFOLLOW in _follow[term])
                        {
                            row[elementFromFOLLOW] = "Synch";
                        }
                    }
            }

            // Fills in the table.
            foreach (var (term, rule) in _rules)
            {
                foreach (string grammarRule in rule)
                {
                    var constructedFIRSTforProduction = ContinueFIRST(new(), "test", new() { grammarRule });
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
                                foreach (var terminalFromFOLLOW in _follow[term])
                                    row[terminalFromFOLLOW] = grammarRule;
                }
            }

            return predictiveAnalysisTable;
        }


        private Rules _rules;
        private Rules _first;
        private Rules _follow;
        private string _startNonTerm = string.Empty;
    }
}
