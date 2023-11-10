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


        public static void Seek(string s, ref StringBuilder sb, char start, char end, ref int i)
        {
            sb.Append(start);
            while (s[++i] != end)
                sb.Append(s[i]);
            sb.Append(end);
        }

        private Rules ContinueFIRST(Rules oldFirst, string currentNonterminal, List<string> grammarRules)
        {
            Rules newFirst = new(oldFirst);

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
                    string nonTerm = currentSentence.ToString();

                    if (_rules.ContainsKey(nonTerm) && isItFirstTime)
                    {
                        newFirst = this.ContinueFIRST(newFirst, nonTerm, _rules[nonTerm]);
                        if (newFirst[nonTerm].Contains("ε"))
                        {
                            doAllPreviousFIRSTsHaveEmptySymbol = true;
                            newFirst[nonTerm].Remove("ε");
                        }
                        terminals.AddRange(newFirst[nonTerm].Except(terminals));
                    }
                    else if (_rules.ContainsKey(nonTerm) && !isItFirstTime)
                    {
                        if (doAllPreviousFIRSTsHaveEmptySymbol)
                        {
                            newFirst = this.ContinueFIRST(newFirst, nonTerm, _rules[nonTerm]);
                            if (!newFirst[nonTerm].Contains("ε") &&
                                doAllPreviousFIRSTsHaveEmptySymbol)
                            {
                                doAllPreviousFIRSTsHaveEmptySymbol = false;
                            }
                            else if (newFirst[nonTerm].Contains("ε") &&
                                !doAllPreviousFIRSTsHaveEmptySymbol)
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
                            doAllPreviousFIRSTsHaveEmptySymbol = true;
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
                    isItFirstTime = false;
                }
                if (doAllPreviousFIRSTsHaveEmptySymbol)
                {
                    terminals.Add("ε");
                }
                currentSentence.Clear();
                doAllPreviousFIRSTsHaveEmptySymbol = false;
            }

            if (!newFirst.ContainsKey(currentNonterminal))
                newFirst.Add(currentNonterminal, terminals);

            return newFirst;
        }

        public void ConstructFOLLOW()
        {

        }

        public DataTable MakePredictiveAnalysisTable()
        {
            DataTable predictiveAnalysisTable = new();

            return predictiveAnalysisTable;
        }


        private string? BreakNonTerm(string replacement, in StringBuilder subline)
        {
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
                                    var s = subline.ToString();
                                    subline.Clear();
                                    return s;
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
            return null;
        }

        private Rules _rules;
        private Rules _first;
        private Rules _follow;
        private string _startNonTerm = string.Empty;
    }
}
