/* Copyright (C) 2013 by John Cronin
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:

 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.

 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace tydisasm
{
    public class Tokenizer
    {
        public class TokenDefinition
        {
            public enum def_type { String, Whitespace, Newline, AnyString };
            public def_type Type = def_type.String;
            public string String;

            public TokenDefinition(def_type type) { Type = type; }
            public TokenDefinition(string str) { Type = def_type.String; String = str; }
            public TokenDefinition(char c) { Type = def_type.String; String = new string(new char[] { c }); }

            public override string ToString()
            {
                switch (Type)
                {
                    case def_type.Newline:
                        return "Newline";
                    case def_type.Whitespace:
                        return "Whitespace";
                    case def_type.String:
                        return String;
                    case def_type.AnyString:
                        return "AnyString";
                    default:
                        throw new NotSupportedException();
                }
            }

            public override bool Equals(object obj)
            {
                if (!(obj is TokenDefinition))
                    return false;
                TokenDefinition other = obj as TokenDefinition;
                if (Type != other.Type)
                {
                    if ((Type == def_type.String) && (other.Type == def_type.AnyString))
                        return true;
                    if ((Type == def_type.AnyString) && (other.Type == def_type.String))
                        return true;

                    return false;
                }
                if ((Type == def_type.String) && (!String.Equals(other.String)))
                    return false;
                return true;
            }

            public override int GetHashCode()
            {
                if (Type == def_type.String)
                    return String.GetHashCode();
                else
                    return Type.GetHashCode();
            }
        }

        public class PreprocessorOptions
        {
            public bool StandardCTrigraphs;

            public bool Includes;
            public bool Defines;
            public bool IfDef;

            public ICollection<CommentsType> Comments;

            public class CommentsType
            {
                public string Start;
                public string End;
            }

            internal static TokenDefinition[] IncludeDefinition = new TokenDefinition[] { new TokenDefinition(TokenDefinition.def_type.Newline), new TokenDefinition("#"), new TokenDefinition("include") };
            internal static TokenDefinition[] DefineDefinition = new TokenDefinition[] { new TokenDefinition(TokenDefinition.def_type.Newline), new TokenDefinition("#"), new TokenDefinition("define") };
            internal static TokenDefinition[] IfDefDefinition = new TokenDefinition[] { new TokenDefinition(TokenDefinition.def_type.Newline), new TokenDefinition("#"), new TokenDefinition("ifdef") };
            internal static TokenDefinition[] ElseDefinition = new TokenDefinition[] { new TokenDefinition(TokenDefinition.def_type.Newline), new TokenDefinition("#"), new TokenDefinition("else") };
            internal static TokenDefinition[] EndifDefinition = new TokenDefinition[] { new TokenDefinition(TokenDefinition.def_type.Newline), new TokenDefinition("#"), new TokenDefinition("endif") };
        }

        public class TokenGrammar
        {
            public char[] Quotes;
            public ICollection<TokenDefinition> Definition;
            public bool IncludeNewlines;
            public bool IncludeWhitespace;
            public PreprocessorOptions PreOptions;

        }

        static ICollection<TokenDefinition> CAsmTokenDefinition
        {
            get
            {
                List<Tokenizer.TokenDefinition> def = new List<Tokenizer.TokenDefinition>();
                def.Add(new Tokenizer.TokenDefinition("//"));
                def.Add(new Tokenizer.TokenDefinition("/*"));
                def.Add(new Tokenizer.TokenDefinition("*/"));
                def.Add(new Tokenizer.TokenDefinition("["));
                def.Add(new Tokenizer.TokenDefinition("]"));
                def.Add(new Tokenizer.TokenDefinition(":"));
                def.Add(new Tokenizer.TokenDefinition(";"));
                def.Add(new Tokenizer.TokenDefinition("("));
                def.Add(new Tokenizer.TokenDefinition(")"));
                def.Add(new Tokenizer.TokenDefinition("++"));
                def.Add(new Tokenizer.TokenDefinition("--"));
                def.Add(new Tokenizer.TokenDefinition("+="));
                def.Add(new Tokenizer.TokenDefinition("-="));
                def.Add(new Tokenizer.TokenDefinition("*="));
                def.Add(new Tokenizer.TokenDefinition("/="));
                def.Add(new Tokenizer.TokenDefinition("%="));
                def.Add(new Tokenizer.TokenDefinition("!="));
                def.Add(new Tokenizer.TokenDefinition(">="));
                def.Add(new Tokenizer.TokenDefinition("<="));
                def.Add(new Tokenizer.TokenDefinition(">>"));
                def.Add(new Tokenizer.TokenDefinition("<<"));
                def.Add(new Tokenizer.TokenDefinition("{"));
                def.Add(new Tokenizer.TokenDefinition("}"));
                def.Add(new Tokenizer.TokenDefinition(">"));
                def.Add(new Tokenizer.TokenDefinition("<"));
                def.Add(new Tokenizer.TokenDefinition("=="));
                def.Add(new Tokenizer.TokenDefinition("="));
                def.Add(new Tokenizer.TokenDefinition("!"));
                def.Add(new Tokenizer.TokenDefinition("+"));
                def.Add(new Tokenizer.TokenDefinition("-"));
                def.Add(new Tokenizer.TokenDefinition("*"));
                def.Add(new Tokenizer.TokenDefinition("^"));
                def.Add(new Tokenizer.TokenDefinition("/"));
                def.Add(new Tokenizer.TokenDefinition("?"));
                def.Add(new Tokenizer.TokenDefinition(","));
                def.Add(new Tokenizer.TokenDefinition("%"));
                return def;
            }
        }

        static ICollection<PreprocessorOptions.CommentsType> C99Comments
        {
            get
            {
                return new PreprocessorOptions.CommentsType[] {
                    new PreprocessorOptions.CommentsType { Start = "/*", End = "*/" },
                    new PreprocessorOptions.CommentsType { Start = "//", End = null }
                };
            }
        }

        static ICollection<PreprocessorOptions.CommentsType> C89Comments
        {
            get
            {
                return new PreprocessorOptions.CommentsType[] {
                    new PreprocessorOptions.CommentsType { Start = "/*", End = "*/" },
                };
            }
        }

        static ICollection<PreprocessorOptions.CommentsType> NasmComments
        {
            get
            {
                return new PreprocessorOptions.CommentsType[] {
                    new PreprocessorOptions.CommentsType { Start = ";", End = null }
                };
            }
        }

        public static TokenGrammar CAsmTokenGrammar
        {
            get
            {
                TokenGrammar ret = new TokenGrammar();

                ret.Definition = CAsmTokenDefinition;

                ret.IncludeNewlines = true;
                ret.IncludeWhitespace = false;
                ret.Quotes = new char[] { '\'', '\"' };

                return ret;
            }
        }

        static PreprocessorOptions GenericCPreprocessorOptions
        {
            get
            {
                PreprocessorOptions ret = new PreprocessorOptions();
                ret.Defines = true;
                ret.IfDef = true;
                ret.Includes = true;
                ret.StandardCTrigraphs = true;
                return ret;
            }
        }

        public static PreprocessorOptions C99PreprocessorOptions
        {
            get
            {
                PreprocessorOptions ret = GenericCPreprocessorOptions;
                ret.Comments = C99Comments;
                return ret;
            }
        }

        public static PreprocessorOptions C89PreprocessorOptions
        {
            get
            {
                PreprocessorOptions ret = GenericCPreprocessorOptions;
                ret.Comments = C89Comments;
                return ret;
            }
        }

        public static PreprocessorOptions NasmPreprocessorOptions
        {
            get
            {
                PreprocessorOptions ret = GenericCPreprocessorOptions;
                ret.Comments = NasmComments;
                return ret;
            }
        }

        public static List<TokenDefinition> Tokenize(string input, TokenGrammar grammar, PreprocessorOptions preopts)
        { return Tokenize(input, grammar.Definition, grammar.Quotes, grammar.IncludeWhitespace, grammar.IncludeNewlines, preopts); }

        public static List<TokenDefinition> Tokenize(string input, ICollection<TokenDefinition> definition, char[] quote_types, bool include_whitespace, bool include_newlines, PreprocessorOptions preopts)
        {
            // First run preprocessor passes
            // Do trigraph replacement if required
            if (preopts.StandardCTrigraphs)
            {
                input = Replace(input, new string[][] {
                    new string[] { "??=", "#" },
                    new string[] { "??/", "\\" },
                    new string[] { "??'", "^" },
                    new string[] { "??(", "[" },
                    new string[] { "??)", "]" },
                    new string[] { "??!", "|" },
                    new string[] { "??<", "{" },
                    new string[] { "??>", "}" },
                    new string[] { "??-", "~" },
                }, false);
            }
            
            List<TokenDefinition> ret = new List<TokenDefinition>();

            if (quote_types == null)
                quote_types = new char[] { };
            char current_quote_type = '\0';
            StringBuilder cur_str = new StringBuilder();

            int i = 0;

            // Terminate input with a newline
            input = input + "\n";

            while (i < input.Length)
            {
                char c = input[i];

                // If we're within a quote, do interpret it
                if (current_quote_type != '\0')
                {
                    // Is it the end of the quote?
                    if (c == current_quote_type)
                    {
                        ret.Add(new TokenDefinition(cur_str.ToString()));
                        ret.Add(new TokenDefinition(current_quote_type));
                        i++;
                        cur_str = new StringBuilder();
                        current_quote_type = '\0';
                        continue;
                    }

                    // Is it an escaped character?
                    if (c == '\\')
                    {
                        i++;
                        c = input[i];
                        cur_str.Append(c);
                        i++;
                        continue;
                    }

                    // Else, add it to the current string
                    cur_str.Append(c);
                    i++;
                    continue;
                }

                // Not within a quote
                // Is it the start of a quote?
                foreach (char quote_start in quote_types)
                {
                    if (c == quote_start)
                    {
                        // We're at the start of a quote.
                        AddToken(ret, cur_str.ToString(), include_whitespace, include_newlines);
                        current_quote_type = c;
                        cur_str = new StringBuilder();
                        i++;
                        continue;
                    }
                }

                // Not the start of a quote, is it whitespace?
                if (IsWhiteSpace(c))
                {
                    // Is the current token only whitespace? If so add it
                    if (IsWhiteSpace(cur_str.ToString()))
                    {
                        cur_str.Append(c);
                        i++;
                        continue;
                    }

                    // If not, it marks the end of a string
                    if (cur_str.Length > 0)
                    {
                        // Now try and match the specific strings against this
                        string s = cur_str.ToString();
                        int j = 0;
                        int str_start = 0;

                        while (j < s.Length)
                        {
                            int chars_left = s.Length - j;
                            bool match = false;

                            foreach (TokenDefinition spec_str in definition)
                            {
                                if ((spec_str.Type == TokenDefinition.def_type.String) && (chars_left >= spec_str.String.Length))
                                {
                                    if (s.Substring(j, spec_str.String.Length) == spec_str.String)
                                    {
                                        // We have a match
                                        // Add the previous string if there was one
                                        if (str_start != j)
                                            AddToken(ret, s.Substring(str_start, j - str_start), include_whitespace, include_newlines);

                                        // Add this match
                                        AddToken(ret, spec_str.String, include_whitespace, include_newlines);

                                        // Advance on beyond this string
                                        j = j + spec_str.String.Length;
                                        str_start = j;
                                        match = true;
                                        break;
                                    }
                                }
                            }

                            if (!match)
                                j++;
                        }

                        // Now add whatever's left
                        if (str_start < s.Length)
                            AddToken(ret, s.Substring(str_start), include_whitespace, include_newlines);

                        cur_str = new StringBuilder();
                        cur_str.Append(c);
                        i++;
                        continue;
                    }
                }

                // Its just a regular character
                // Is the current string just whitespace?
                if (IsWhiteSpace(cur_str.ToString()))
                {
                    AddToken(ret, cur_str.ToString(), include_whitespace, include_newlines);
                    cur_str = new StringBuilder();
                }

                cur_str.Append(c);
                i++;
                continue;
            }

            // Now remove comments
            i = 0;
            int starting_comment_token = 0;
            PreprocessorOptions.CommentsType cur_comment = null;

            while (i < ret.Count)
            {
                TokenDefinition tok = ret[i];

                // Are we currently in a comment?
                if (cur_comment != null)
                {
                    // Do we match the end comment marker?
                    if (((cur_comment.End == null) && (tok.Type == TokenDefinition.def_type.Newline)) ||
                        ((cur_comment.End == tok.String) && (tok.Type == TokenDefinition.def_type.String)))
                    {
                        // Remove tokens from starting_comment_token up to and including i
                        ret.RemoveRange(starting_comment_token, i - starting_comment_token + 1);
                        i = starting_comment_token;

                        // Reinsert the newline if we removed one
                        if (cur_comment.End == null)
                        {
                            ret.Insert(i, new TokenDefinition(TokenDefinition.def_type.Newline));
                            i++;
                        }
                        cur_comment = null;
                        continue;
                    }
                    else
                    {
                        i++;
                        continue;
                    }
                }
                else
                {
                    // Do we match a new comment start?
                    if ((tok.Type == TokenDefinition.def_type.String) && (preopts.Comments != null))
                    {
                        foreach (PreprocessorOptions.CommentsType ct in preopts.Comments)
                        {
                            if (ct.Start == tok.String)
                            {
                                cur_comment = ct;
                                starting_comment_token = i;
                            }
                        }
                    }
                }

                // Continue
                i++;
            }

            // If still in a comment, remove the last tokens
            if (cur_comment != null)
                ret.RemoveRange(starting_comment_token, i - starting_comment_token);

            // Now parse looking for #define, #include etc
            // First add a newline at the start if not already there, to assist searching on 'Newline' '#' 'include' etc
            if (!((ret.Count > 0) && (ret[0].Type == TokenDefinition.def_type.Newline)))
                ret.Insert(0, new TokenDefinition(TokenDefinition.def_type.Newline));

            i = 0;
            while (i < ret.Count)
            {
                // TODO
                if (preopts.Includes && Match(ret, PreprocessorOptions.IncludeDefinition, i))
                {

                }


                i++;
            }

            // Now remove the newline again
            ret.RemoveAt(0);

            // Terminate with a newline
            ret.Add(new TokenDefinition(TokenDefinition.def_type.Newline));

            return ret;
        }

        private static void AddToken(List<TokenDefinition> ret, string p, bool include_whitespace, bool include_newlines)
        {
            if (IsNewLine(p))
            {
                if (include_newlines)
                    ret.Add(new TokenDefinition(TokenDefinition.def_type.Newline));
            }
            else if (IsWhiteSpace(p))
            {
                if (IncludesNewlines(p) && include_newlines)
                    ret.Add(new TokenDefinition(TokenDefinition.def_type.Newline));
                else if (include_whitespace)
                    ret.Add(new TokenDefinition(TokenDefinition.def_type.Whitespace));
            }
            else
                ret.Add(new TokenDefinition(p));
        }

        private static bool IncludesNewlines(string p)
        {
            foreach(char c in p)
            {
                if(IsNewLine(c))
                    return true;
            }
            return false;
        }

        private static bool IsWhiteSpace(string p)
        {
            foreach (char c in p)
            {
                if (!IsWhiteSpace(c))
                    return false;
            }
            return true;
        }

        private static bool IsWhiteSpace(char c)
        {
            switch (c)
            {
                case ' ':
                case '\t':
                case '\n':
                case '\r':
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsNewLine(string p)
        {
            foreach (char c in p)
            {
                if (!IsNewLine(c))
                    return false;
            }
            return true;
        }

        private static bool IsNewLine(char c)
        {
            switch (c)
            {
                case '\n':
                case '\r':
                    return true;
                default:
                    return false;
            }
        }

        public static bool Match(IList<TokenDefinition> input, IList<TokenDefinition> match, int idx)
        {
            if ((idx + match.Count) > input.Count)
                return false;

            for (int i = 0; i < match.Count; i++)
            {
                if (!input[idx + i].Equals(match[i]))
                    return false;
            }
            return true;
        }

        static string Replace(string input, string[][] matches, bool whole_word)
        {
            // order the lines to match into descending order of length

            List<string[]> new_matches = new List<string[]>(matches);
            new_matches.Sort(new match_sorter());
            StringBuilder ret = new StringBuilder();

            int i = 0;
            while (i < input.Length)
            {
                bool match_found = false;

                foreach (string[] match in new_matches)
                {
                    if (Match(input, i, match[0]))
                    {
                        i += match[0].Length;
                        ret.Append(match[1]);
                        match_found = true;
                        break;
                    }
                }

                if (!match_found)
                {
                    ret.Append(input[i]);
                    i++;
                }
            }

            return ret.ToString();
        }

        class match_sorter : IComparer<string[]>
        {
            public int Compare(string[] x, string[] y)
            {
                return x[0].Length - y[0].Length;
            }
        }

        static bool Match(string input, int start_index, string match)
        {
            for (int i = 0; i < match.Length; i++)
            {
                if ((i + start_index) >= input.Length)
                    return false;
                if (input[i + start_index] != match[i])
                    return false;
            }
            return true;
        }
    }
}
