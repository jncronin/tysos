using System;
using System.Collections.Generic;
using System.Text;

namespace tyc
{
    class Preprocessor
    {
        internal static List<token> Process(string input, Dictionary<string, List<token>> defines, Program.IncludeFileLocator file_locator)
        {
            /* The C standard defines eight phases of translation
             * 
             * The first four are handled by the preprocessor:
             *  - Trigraph replacement
             *  - Line splicing (joining lines separated by the \ character)
             *  - Tokenization + replacing comments with whitespace
             *  - Marco expansion and directive handling
             */

            string post_trigraph = Replace(input, new string[][] {
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

            string spliced_lines = SpliceLines(post_trigraph);
            List<token> tokens = Tokenize(spliced_lines);

            int i = 0;
            List<token> expanded_tokens = ConvertToTree(tokens, ref i);

            List<token> new_tokens = HandleDirectives(expanded_tokens, defines, file_locator);

            return new_tokens;
        }

        private static List<token> ConvertToTree(List<token> tokens, ref int i)
        {
            token tok;

            List<token> ret = new List<token>();

            while ((tok = GetToken(tokens, i)).type != token.token_type.beyond_eof)
            {
                if (tok.type == token.token_type.preprocessor_directive)
                {
                    string directive = tok.directive;

                    if ((directive == "#if") || (directive == "#ifdef") || (directive == "#ifndef"))
                    {
                        token if_tok = tok;
                        i++;
                        if_tok.if_tokens = ConvertToTree(tokens, ref i);

                        tok = GetToken(tokens, i);
                        if (tok.type != token.token_type.preprocessor_directive)
                            throw new Exception();
                        if (tok.directive == "#else")
                        {
                            i++;
                            if_tok.else_tokens = ConvertToTree(tokens, ref i);
                        }
                        else if (tok.directive == "#endif")
                            i++;
                        else
                            throw new Exception();

                        ret.Add(if_tok);
                    }
                    else if ((directive == "#else") || (directive == "#endif"))
                        return ret;
                    else
                    {
                        i++;
                        ret.Add(tok);
                    }
                }
                else
                {
                    i++;
                    ret.Add(tok);
                }
            }

            return ret;
        }

        private static List<token> HandleDirectives(List<token> tokens, Dictionary<string, List<token>> defines, Program.IncludeFileLocator file_locator)
        {
            List<token> ret = new List<token>();
            if (tokens == null)
                return ret;

            for (int i = 0; i < tokens.Count; i++)
            {
                token tok = tokens[i];

                if (tok.type == token.token_type.preprocessor_directive)
                {
                    string directive = tok.directive;

                    if (directive == "#include")
                        HandleInclude(ret, tokens, i, defines, file_locator);
                    else if (directive == "#if")
                        HandleIf(ret, tokens, i, defines, file_locator);
                    else if (directive == "#ifdef")
                        HandleIfDef(ret, tokens, i, defines, file_locator);
                    else if (directive == "#define")
                        HandleDefine(ret, tokens, i, defines, file_locator);
                    else if (directive == "#error")
                        HandleError(ret, tokens, i, defines, file_locator);
                    else if (directive == "#warning")
                        HandleWarning(ret, tokens, i, defines, file_locator);
                    else if (directive == "#pragma")
                        HandlePragma(ret, tokens, i, defines, file_locator);
                    else if (directive == "#ifndef")
                        HandleIfndef(ret, tokens, i, defines, file_locator);
                    else
                        throw new Exception("Unsupported preprocessor directive: " + tok.value);
                }
                else
                {
                    if ((tok.type == token.token_type.identifier) && defines.ContainsKey(tok.value))
                        ret.AddRange(defines[tok.value]);
                    else
                        ret.Add(tok);
                }
            }

            return ret;
        }

        private static void HandleIfndef(List<token> ret, List<token> tokens, int i, Dictionary<string, List<token>> defines, Program.IncludeFileLocator file_locator)
        {
            if (tokens[i].parameters.Count != 1)
                throw new Exception();
            if (defines.ContainsKey(tokens[i].parameters[0].value))
                ret.AddRange(HandleDirectives(tokens[i].else_tokens, defines, file_locator));
            else
                ret.AddRange(HandleDirectives(tokens[i].if_tokens, defines, file_locator));
        }

        private static void HandleIf(List<token> ret, List<token> tokens, int i, Dictionary<string, List<token>> defines, Program.IncludeFileLocator file_locator)
        {
            throw new NotImplementedException();
        }

        private static void HandleIfDef(List<token> ret, List<token> tokens, int i, Dictionary<string, List<token>> defines, Program.IncludeFileLocator file_locator)
        {
            if (tokens[i].parameters.Count != 1)
                throw new Exception();
            if (defines.ContainsKey(tokens[i].parameters[0].value))
                ret.AddRange(HandleDirectives(tokens[i].if_tokens, defines, file_locator));
            else
                ret.AddRange(HandleDirectives(tokens[i].else_tokens, defines, file_locator));
        }

        private static void HandleDefine(List<token> ret, List<token> tokens, int i, Dictionary<string, List<token>> defines, Program.IncludeFileLocator file_locator)
        {
            token macro_name = tokens[i].parameters[0];
            List<token> defined_as = new List<token>();
            for (int j = 1; j < tokens[i].parameters.Count; j++)
                defined_as.Add(tokens[i].parameters[j]);
            List<token> new_defined_as = HandleDirectives(defined_as, defines, file_locator);

            defines[macro_name.value] = new_defined_as;
        }

        private static void HandleError(List<token> ret, List<token> tokens, int i, Dictionary<string, List<token>> defines, Program.IncludeFileLocator file_locator)
        {
            throw new NotImplementedException();
        }

        private static void HandleWarning(List<token> ret, List<token> tokens, int i, Dictionary<string, List<token>> defines, Program.IncludeFileLocator file_locator)
        {
            throw new NotImplementedException();
        }

        private static void HandlePragma(List<token> ret, List<token> tokens, int i, Dictionary<string, List<token>> defines, Program.IncludeFileLocator file_locator)
        {
            throw new NotImplementedException();
        }

        private static void HandleInclude(List<token> ret, List<token> tokens, int i, Dictionary<string, List<token>> defines, Program.IncludeFileLocator file_locator)
        {
            token file_name = tokens[i].parameters[0];
            if (file_name.value == "<")
                file_name = new token { type = token.token_type.identifier, value = tokens[i].value.Split(' ', '\t')[1].Trim() };
            if ((file_name.type != token.token_type.identifier) && (file_name.type != token.token_type.string_literal))
                throw new Exception();

            bool search_cur_dir = false;
            if (file_name.value.StartsWith("\""))
                search_cur_dir = true;
            string fname = file_name.value.Trim('\"', '<', '>');

            string file_text = file_locator.ReadFile(fname, search_cur_dir);

            ret.AddRange(Process(file_text, defines, file_locator));
        }

        private static token GetToken(List<token> tokens, int p)
        {
            if (p >= tokens.Count)
                return new token { type = token.token_type.beyond_eof, value = "" };
            else
                return tokens[p];
        }

        private static List<token> Tokenize(string spliced_lines)
        {
            /* The tokenizer tries to identify various preprocessor tokens, in a 
             * greedy manner (i.e. it attempts to match the longest possible token
             * first)
             */

            StringBuilder cur_str = new StringBuilder();
            List<token> ret = new List<token>();

            int i = 0;
            while (i < spliced_lines.Length)
            {
                if (IsWhitespace(spliced_lines[i]))
                {
                    if (cur_str.ToString() != "")
                    {
                        string identifier = cur_str.ToString();

                        if (((identifier[0] == '.') && (IsDigit(identifier[1]))) || (IsDigit(identifier[0])))
                            ret.Add(new token { type = token.token_type.number, value = identifier });
                        else
                            ret.Add(new token { type = token.token_type.identifier, value = identifier });
                    }
                    cur_str = new StringBuilder();
                    i++;
                    continue;
                }

                List<token> matched_tokens = new List<token>();
                foreach (token_match tok_match in tokens)
                {
                    token tok = MatchToken(spliced_lines, i, tok_match);
                    if (tok != null)
                        matched_tokens.Add(tok);
                }

                if (matched_tokens.Count > 0)
                {
                    if (cur_str.ToString() != "")
                    {
                        string identifier = cur_str.ToString();

                        if (((identifier[0] == '.') && (IsDigit(identifier[1]))) || (IsDigit(identifier[0])))
                            ret.Add(new token { type = token.token_type.number, value = identifier });
                        else
                            ret.Add(new token { type = token.token_type.identifier, value = identifier });
                    }
                    cur_str = new StringBuilder();

                    // identify the longest token
                    token largest = null;
                    int largest_length = -1;

                    foreach (token matched_token in matched_tokens)
                    {
                        if (matched_token.value.Length > largest_length)
                        {
                            largest = matched_token;
                            largest_length = matched_token.value.Length;
                        }
                    }

                    ret.Add(largest);
                    i += largest_length;
                }
                else
                {
                    cur_str.Append(spliced_lines[i]);
                    i++;
                }
            }

            return ret;
        }

        private static bool IsWhitespace(char p)
        {
            if (p == ' ')
                return true;
            if (p == '\n')
                return true;
            if (p == '\r')
                return true;
            if (p == '\t')
                return true;
            return false;
        }

        private static bool IsDigit(char p)
        {
            if (p < '0')
                return false;
            if (p > '9')
                return false;
            return true;
        }

        private static token MatchToken(string input, int start_index, token_match tok_match)
        {
            // match start characters
            if (Match(input, start_index, tok_match.start_string))
            {
                if (tok_match.end_strings == null)
                    return new token { type = tok_match.type, value = tok_match.start_string };

                // now iterate through and try to match the end characters
                for (int i = start_index + tok_match.start_string.Length; i < input.Length; i++)
                {
                    foreach (string end_match in tok_match.end_strings)
                    {
                        if (Match(input, i, end_match))
                        {
                            int length = i + end_match.Length - start_index;
                            string val = input.Substring(start_index, length);
                            return new token { type = tok_match.type, value = val };
                        }

                    }
                }

                return null;
            }
            else
                return null;
        }

        private static string SpliceLines(string post_trigraph)
        {
            string spliced = Replace(post_trigraph, new string[][] {
                new string[] { "\\\n", "" },
                new string[] { "\\\r\n", "" },
                new string[] { "\r\n", "\n" },
            }, false);

            return spliced;
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

        internal class token
        {
            internal enum token_type { identifier, number, string_literal, punctuator, other, preprocessor_directive, comment, beyond_eof };

            internal token_type type;
            internal string value;

            internal string directive { get { return value.Split(' ', '\t')[0].Trim(); } }
            internal List<token> parameters { get { return Preprocessor.Tokenize(value.Substring(directive.Length + 1)); } }
            internal List<token> if_tokens;
            internal List<token> else_tokens;

            public override string ToString()
            {
                return type.ToString() + ": " + value;
            }
        }

        class token_match
        {
            internal token.token_type type;
            internal string start_string;
            internal string[] end_strings;
        }

        static List<token_match> tokens = new List<token_match>();

        static Preprocessor()
        {
            InitTokens();
        }

        static void InitTokens()
        {
            tokens.Add(new token_match { type = token.token_type.comment, start_string = "//", end_strings = new string[] { "\n" } });
            tokens.Add(new token_match { type = token.token_type.comment, start_string = "/*", end_strings = new string[] { "*/" } });
            tokens.Add(new token_match { type = token.token_type.string_literal, start_string = "\"", end_strings = new string[] { "\"" } });
            tokens.Add(new token_match { type = token.token_type.string_literal, start_string = "\'", end_strings = new string[] { "\'" } });
            tokens.Add(new token_match { type = token.token_type.punctuator, start_string = "!" });
            tokens.Add(new token_match { type = token.token_type.punctuator, start_string = "!=" });
            tokens.Add(new token_match { type = token.token_type.punctuator, start_string = "%" });
            tokens.Add(new token_match { type = token.token_type.punctuator, start_string = "%=" });
            tokens.Add(new token_match { type = token.token_type.punctuator, start_string = "^" });
            tokens.Add(new token_match { type = token.token_type.punctuator, start_string = "&" });
            tokens.Add(new token_match { type = token.token_type.punctuator, start_string = "&&" });
            tokens.Add(new token_match { type = token.token_type.punctuator, start_string = "*" });
            tokens.Add(new token_match { type = token.token_type.punctuator, start_string = "*=" });
            tokens.Add(new token_match { type = token.token_type.punctuator, start_string = "(" });
            tokens.Add(new token_match { type = token.token_type.punctuator, start_string = ")" });
            tokens.Add(new token_match { type = token.token_type.punctuator, start_string = "-" });
            tokens.Add(new token_match { type = token.token_type.punctuator, start_string = "--" });
            tokens.Add(new token_match { type = token.token_type.punctuator, start_string = "-=" });
            tokens.Add(new token_match { type = token.token_type.punctuator, start_string = "+" });
            tokens.Add(new token_match { type = token.token_type.punctuator, start_string = "++" });
            tokens.Add(new token_match { type = token.token_type.punctuator, start_string = "+=" });
            tokens.Add(new token_match { type = token.token_type.punctuator, start_string = "=" });
            tokens.Add(new token_match { type = token.token_type.punctuator, start_string = "==" });
            tokens.Add(new token_match { type = token.token_type.punctuator, start_string = "{" });
            tokens.Add(new token_match { type = token.token_type.punctuator, start_string = "}" });
            tokens.Add(new token_match { type = token.token_type.punctuator, start_string = "[" });
            tokens.Add(new token_match { type = token.token_type.punctuator, start_string = "]" });
            tokens.Add(new token_match { type = token.token_type.punctuator, start_string = ";" });
            tokens.Add(new token_match { type = token.token_type.punctuator, start_string = ":" });
            tokens.Add(new token_match { type = token.token_type.punctuator, start_string = "~" });
            tokens.Add(new token_match { type = token.token_type.punctuator, start_string = "<" });
            tokens.Add(new token_match { type = token.token_type.punctuator, start_string = "<=" });
            tokens.Add(new token_match { type = token.token_type.punctuator, start_string = "<<" });
            tokens.Add(new token_match { type = token.token_type.punctuator, start_string = ">" });
            tokens.Add(new token_match { type = token.token_type.punctuator, start_string = ">=" });
            tokens.Add(new token_match { type = token.token_type.punctuator, start_string = ">>" });
            tokens.Add(new token_match { type = token.token_type.punctuator, start_string = "," });
            tokens.Add(new token_match { type = token.token_type.punctuator, start_string = "." });
            tokens.Add(new token_match { type = token.token_type.punctuator, start_string = "?" });
            tokens.Add(new token_match { type = token.token_type.punctuator, start_string = "/" });
            tokens.Add(new token_match { type = token.token_type.punctuator, start_string = "/=" });
            tokens.Add(new token_match { type = token.token_type.punctuator, start_string = "<<=" });
            tokens.Add(new token_match { type = token.token_type.punctuator, start_string = ">>=" });
            tokens.Add(new token_match { type = token.token_type.punctuator, start_string = "&=" });
            tokens.Add(new token_match { type = token.token_type.punctuator, start_string = "^=" });
            tokens.Add(new token_match { type = token.token_type.punctuator, start_string = "|=" });
            tokens.Add(new token_match { type = token.token_type.punctuator, start_string = "|" });
            tokens.Add(new token_match { type = token.token_type.punctuator, start_string = "||" });
            tokens.Add(new token_match { type = token.token_type.preprocessor_directive, start_string = "#include", end_strings = new string[] { "\n" } });
            tokens.Add(new token_match { type = token.token_type.preprocessor_directive, start_string = "#if", end_strings = new string[] { "\n" } });
            tokens.Add(new token_match { type = token.token_type.preprocessor_directive, start_string = "#ifdef", end_strings = new string[] { "\n" } });
            tokens.Add(new token_match { type = token.token_type.preprocessor_directive, start_string = "#ifndef", end_strings = new string[] { "\n" } });
            tokens.Add(new token_match { type = token.token_type.preprocessor_directive, start_string = "#endif", end_strings = new string[] { "\n" } });
            tokens.Add(new token_match { type = token.token_type.preprocessor_directive, start_string = "#else", end_strings = new string[] { "\n" } });
            tokens.Add(new token_match { type = token.token_type.preprocessor_directive, start_string = "#define", end_strings = new string[] { "\n" } });
            tokens.Add(new token_match { type = token.token_type.preprocessor_directive, start_string = "#error", end_strings = new string[] { "\n" } });
            tokens.Add(new token_match { type = token.token_type.preprocessor_directive, start_string = "#pragma", end_strings = new string[] { "\n" } });
            tokens.Add(new token_match { type = token.token_type.preprocessor_directive, start_string = "#warning", end_strings = new string[] { "\n" } });
        }
    }
}
