using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace tyc
{
    class Parser2
    {
        internal bool ParseTokens(IList<Preprocessor.token> tokens, string start_bnf)
        {
            int x = 0;

            while (x < tokens.Count)
            {
                int new_x = ParseToken(tokens, x, start_bnf);
                if (x == new_x)
                    return false;
                x = new_x;
            }
            return true;
        }

        private int ParseToken(IList<Preprocessor.token> tokens, int x, string bnf)
        {
            BNFEntry bne = bnf_entries[bnf];

            System.Diagnostics.Debug.WriteLine("Testing position " + x.ToString() + " against " + bnf);

            int max_x = x;
            foreach (BNFEntryReference[] bnr in bne.Children)
            {
                int new_x = ParseToken(tokens, x, bnr);
                if (new_x > max_x)
                    max_x = new_x;
            }

            if (max_x != x)
                System.Diagnostics.Debug.WriteLine("Matched position " + x.ToString() + " against " + bnf);

            return max_x;
        }

        private int ParseToken(IList<Preprocessor.token> tokens, int x, BNFEntryReference[] bnr)
        {
            int orig_x = x;

            for (int i = 0; i < bnr.Length; i++)
            {
                if (x >= tokens.Count)
                    return orig_x;

                // Match the token
                BNFEntryReference tok = bnr[i];

                switch (tok.Type)
                {
                    case BNFEntryReference.type.Constant:
                        if (tokens[x].value != tok.Value)
                            return orig_x;
                        x++;
                        break;

                    case BNFEntryReference.type.TokenReference:
                        x++;
                        break;

                    case BNFEntryReference.type.EntryReference:
                        {
                            int new_x = ParseToken(tokens, x, tok.Value);
                            if (new_x == x)
                                return orig_x;
                            x = new_x;
                        }
                        break;
                }
            }

            return x;
        }

        List<string> tokens = new List<string>();
        Dictionary<string, BNFEntry> bnf_entries = new Dictionary<string, BNFEntry>();
        string bnf_name;

        class BNFEntry
        {
            internal string Name;
            internal List<BNFEntryReference[]> Children = new List<BNFEntryReference[]>();

            public override string ToString()
            {
                return Name;
            }
        }

        class BNFEntryReference
        {
            internal enum type { EntryReference, Constant, TokenReference, Empty };
            internal type Type;
            internal string Value;

            public override string ToString()
            {
                switch (Type)
                {
                    case type.Constant:
                        return "\"" + Value + "\"";
                    case type.Empty:
                        return "empty";
                    default:
                        return Value;
                }
            }
        }

        public Parser2(string bnf_file)
        {
            // Load the language bnf

            FileStream fs = new FileStream(bnf_file, FileMode.Open, FileAccess.Read);
            StreamReader sr = new StreamReader(fs);
            string s = sr.ReadToEnd();
            ParseBNF(s);
            RemoveLeftRecursion();

            bnf_name = bnf_file;
        }

        private void RemoveLeftRecursion()
        {
            Dictionary<string, BNFEntry> new_rules = new Dictionary<string, BNFEntry>();

            foreach (KeyValuePair<string, BNFEntry> old_rule in bnf_entries)
            {
                RemoveImmediateLeftRecursion(old_rule.Value, new_rules);


            }

            bnf_entries = new_rules;
        }

        private void RemoveImmediateLeftRecursion(BNFEntry e, Dictionary<string, BNFEntry> new_rules)
        {
            // First group the entries into those which begin with the current entry and those which do not

            List<BNFEntryReference[]> bce = new List<BNFEntryReference[]>();
            List<BNFEntryReference[]> nbce = new List<BNFEntryReference[]>();

            foreach (BNFEntryReference[] child in e.Children)
            {
                if ((child.Length > 0) && (child[0].Type == BNFEntryReference.type.EntryReference) &&
                    (child[0].Value == e.Name))
                    bce.Add(child);
                else
                    nbce.Add(child);
            }

            // If none begin with the current entry then this entry is not left-recursive, therefore return
            if (bce.Count == 0)
            {
                new_rules.Add(e.Name, e);
                return;
            }

            // Create two new rules A and A_
            BNFEntry A = new BNFEntry { Name = e.Name };
            BNFEntry A_ = new BNFEntry { Name = e.Name + "_" };

            // Set A-> nbce[0]A_ | nbce[1]A_ | nbce[2]A_ ...
            foreach (BNFEntryReference[] nbce_i in nbce)
            {
                List<BNFEntryReference> new_nbce_i = new List<BNFEntryReference>(nbce_i);
                new_nbce_i.Add(new BNFEntryReference { Type = BNFEntryReference.type.EntryReference, Value = A_.Name });
                A.Children.Add(new_nbce_i.ToArray());
            }

            // Set A_-> empty | bce[0]A_ | bce[1]A_ ...
            A_.Children.Add(new BNFEntryReference[] { new BNFEntryReference { Type = BNFEntryReference.type.Empty } });
            foreach (BNFEntryReference[] bce_i in bce)
            {
                List<BNFEntryReference> new_bce_i = new List<BNFEntryReference>(bce_i);
                // Trim the 'current entry' from the start of the current list
                new_bce_i.RemoveAt(0);
                new_bce_i.Add(new BNFEntryReference { Type = BNFEntryReference.type.EntryReference, Value = A_.Name });
                A_.Children.Add(new_bce_i.ToArray());
            }

            new_rules.Add(A.Name, A);
            new_rules.Add(A_.Name, A_);
        }

        private void ParseBNF(string s)
        {
            int x = 0;

            ParseBNFHeaders(s, ref x);

            while (x < s.Length)
                ParseBNFEntry(s, ref x);
        }

        private void ParseBNFHeaders(string s, ref int x)
        {
            ParseBNFWhitespace(s, ref x);

            if(BNFMatch(s, ref x, "%token"))
            {
                ParseBNFWhitespace(s, ref x);
                while (!BNFMatch(s, ref x, "%%"))
                {
                    BNFEntryReference tok_name = ParseBNFToken(s, ref x);
                    tokens.Add(tok_name.Value);
                    ParseBNFWhitespace(s, ref x);
                }
            }

            ParseBNFWhitespace(s, ref x);
        }

        private bool BNFMatch(string s, ref int x, string p)
        {
            for (int i = 0; i < p.Length; i++)
            {
                if ((x + i) >= s.Length)
                    return false;
                if (p[i] != s[x + i])
                    return false;
            }

            x += p.Length;
            return true;
        }

        private void ParseBNFEntry(string s, ref int x)
        {
            BNFEntry ret = new BNFEntry();

            ParseBNFWhitespace(s, ref x);
            BNFEntryReference name = ParseBNFToken(s, ref x);
            ret.Name = name.Value;

            if (ret.Name.EndsWith(":"))
                ret.Name = ret.Name.TrimEnd(':');
            else
            {
                ParseBNFWhitespace(s, ref x);
                if (!BNFMatch(s, ref x, ":"))
                    throw new Exception();
            }
            ParseBNFWhitespace(s, ref x);

            bool cont = true;
            List<BNFEntryReference> entries = new List<BNFEntryReference>();

            while (cont)
            {
                BNFEntryReference entry = ParseBNFToken(s, ref x);
                entries.Add(entry);
                ParseBNFWhitespace(s, ref x);

                if (BNFMatch(s, ref x, ";"))
                {
                    ret.Children.Add(entries.ToArray());
                    cont = false;
                    ParseBNFWhitespace(s, ref x);
                }
                else if (BNFMatch(s, ref x, "|"))
                {
                    ret.Children.Add(entries.ToArray());
                    entries = new List<BNFEntryReference>();
                    ParseBNFWhitespace(s, ref x);
                }               
            }

            bnf_entries.Add(ret.Name, ret);
        }

        private BNFEntryReference ParseBNFToken(string s, ref int x)
        {
            BNFEntryReference ret = new BNFEntryReference();
            StringBuilder sb = new StringBuilder();

            if (s[x] == '\'')
            {
                ret.Type = BNFEntryReference.type.Constant;
                x++;
                while (s[x] != '\'')
                {
                    sb.Append(s[x]);
                    x++;
                }
                x++;
                ret.Value = sb.ToString();
                return ret;
            }
            else
            {
                while (!char.IsWhiteSpace(s[x]))
                {
                    sb.Append(s[x]);
                    x++;
                }

                ret.Value = sb.ToString();
                if (tokens.Contains(ret.Value))
                    ret.Type = BNFEntryReference.type.TokenReference;
                else
                    ret.Type = BNFEntryReference.type.EntryReference;
                return ret;
            }
        }

        private void ParseBNFWhitespace(string s, ref int x)
        {
            while ((x < s.Length) &&char.IsWhiteSpace(s[x]))
                x++;
        }
    }
}
