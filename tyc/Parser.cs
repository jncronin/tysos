using System;
using System.Collections.Generic;
using System.Text;

namespace tyc
{
    class Parser
    {
        internal static Dictionary<string, List<Preprocessor.token>> typedefs = new Dictionary<string, List<Preprocessor.token>>();
        internal static Dictionary<string, List<Preprocessor.token>> struct_defs = new Dictionary<string, List<Preprocessor.token>>();

        internal class TokenProvider
        {
            internal List<Preprocessor.token> tokens;

            int i = 0;
            internal Preprocessor.token Current { get { if ((i >= tokens.Count) || (i < 0)) return new Preprocessor.token { type = Preprocessor.token.token_type.beyond_eof }; else return tokens[i]; } }
            internal Preprocessor.token MoveNext() { i++; return Current; }
            internal Preprocessor.token MoveBack() { i--; return Current; }
        }

        internal static ParsedEntry ParseFile(List<Preprocessor.token> tokens)
        { return ParseFile(new TokenProvider { tokens = tokens }); }

        internal static ParsedEntry ParseFile(TokenProvider tp)
        {
            ParsedEntry ret = new ParsedEntry { Type = ParsedEntry.type.file, Tokens = tp.tokens };

            Preprocessor.token t = tp.Current;

            while (t.type != Preprocessor.token.token_type.beyond_eof)
            {
                if (t.type == Preprocessor.token.token_type.comment)
                {
                    t = tp.MoveNext();
                    continue;
                }

                List<Preprocessor.token> cur_tokens = new List<Preprocessor.token>();

                ParsedEntry entry = new ParsedEntry { Tokens = cur_tokens };

                if (t.value == "typedef")
                {
                    cur_tokens.Add(t);
                    t = tp.MoveNext();
                    List<Preprocessor.token> typedef_to = MatchType(tp);
                    cur_tokens.AddRange(typedef_to);
                    t = tp.Current;
                    cur_tokens.Add(t);
                    string typedef_from = t.value;
                    t = tp.MoveNext();
                    if (t.value != ";")
                        throw new Exception();

                    typedefs.Add(typedef_from, typedef_to);
                    t = tp.MoveNext();
                    entry.Type = ParsedEntry.type.typedef;
                }
                else
                {
                    // Parse type
                    List<Preprocessor.token> mtype = MatchType(tp);
                    if (mtype.Count == 0)
                        throw new Exception("Invalid type: " + tp.Current.value);
                    cur_tokens.AddRange(mtype);

                    // Read identifier
                    t = tp.Current;
                    if (t.type == Preprocessor.token.token_type.identifier)
                    {
                        cur_tokens.Add(t);
                        t = tp.MoveNext();
                    }

                    // See if we identify a function
                    if (t.value == "(")
                    {
                        // Function header
                        t = tp.MoveNext();

                        List<ParsedEntry> args = new List<ParsedEntry>();

                        while (t.value != ")")
                        {
                            List<Preprocessor.token> type = MatchType(tp);
                            cur_tokens.AddRange(type);
                            t = tp.Current;

                            if ((t.value != ")") && (t.value != ","))
                            {
                                type.Add(t);
                                cur_tokens.Add(t);
                                t = tp.MoveNext();
                            }

                            if (t.value == ")")
                                break;

                            if (t.value != ",")
                                throw new Exception();

                            args.Add(new ParsedEntry { Type = ParsedEntry.type.variable_definition, Tokens = type });

                            cur_tokens.Add(t);
                            t = tp.MoveNext();
                        }

                        entry.Children.AddRange(args);

                        cur_tokens.Add(t);
                        t = tp.MoveNext();

                        if (t.value == ";")
                        {
                            cur_tokens.Add(t);
                            t = tp.MoveNext();
                            entry.Type = ParsedEntry.type.function_declaration;
                        }
                        else if (t.value == "{")
                        {
                            entry.Type = ParsedEntry.type.function_definition;
                            ParseStatementBlock(tp, entry);
                        }
                    }
                    else if (t.value == ";")
                    {
                        entry.Type = ParsedEntry.type.variable_definition;
                        cur_tokens.Add(t);
                        t = tp.MoveNext();
                    }
                    else if (t.value == "=")
                    {
                        entry.Type = ParsedEntry.type.variable_definition;
                        cur_tokens.Add(t);
                        t = tp.MoveNext();
                        ParseExpression(tp, entry);
                    }
                    else
                        throw new Exception();
                }

                ret.Children.Add(entry);
            }

            return ret;
        }

        private static void ParseExpression(TokenProvider tp, ParsedEntry entry)
        {
            throw new NotImplementedException();
        }

        private static void ParseStatementBlock(TokenProvider tp, ParsedEntry entry)
        {
            throw new NotImplementedException();
        }

        static List<Preprocessor.token> MatchType(TokenProvider tp)
        {
            List<Preprocessor.token> ret = new List<Preprocessor.token>();

            Preprocessor.token t = tp.Current;
            while (true)
            {
                if (t.type == Preprocessor.token.token_type.comment)
                    t = tp.MoveNext();
                else if ((t.value == "extern") || (t.value == "static") || (t.value == "const") || (t.value == "unsigned") ||
                    (t.value == "signed"))
                {
                    ret.Add(t);
                    t = tp.MoveNext();
                }
                else if ((t.value == "int") || (t.value == "long") || (t.value == "char") ||
                    (t.value == "short") || (t.value == "float") || (t.value == "double") ||
                    (t.value == "void"))
                {
                    ret.Add(t);
                    t = tp.MoveNext();

                    if (t.value == "*")
                    {
                        ret.Add(t);
                        t = tp.MoveNext();
                    }

                    if((t.value != "long") && (t.value != "short") && (t.value != "int"))
                        return ret;
                }
                else if (typedefs.ContainsKey(t.value))
                {
                    ret.AddRange(typedefs[t.value]);
                    t = tp.MoveNext();
                    return ret;
                }
                else if ((t.value == "struct") || (t.value == "union"))
                {
                    ret.Add(t);
                    t = tp.MoveNext();

                    string struct_name = null;
                    if (t.value != "{")
                    {
                        struct_name = t.value;
                        ret.Add(t);
                        t = tp.MoveNext();
                    }


                    if (t.value == "{")
                    {
                        ret.Add(t);
                        t = tp.MoveNext();

                        // Read the struct definition
                        List<Preprocessor.token> struct_def = new List<Preprocessor.token>();

                        while (true)
                        {
                            List<Preprocessor.token> type = MatchType(tp);
                            if (type.Count == 0)
                                throw new Exception("Invalid type: " + tp.Current.value);

                            struct_def.AddRange(type);
                            ret.AddRange(type);

                            t = tp.Current;
                            if (t.type == Preprocessor.token.token_type.identifier)
                            {
                                struct_def.Add(t);
                                ret.Add(t);
                                t = tp.MoveNext();
                            }

                            List<Preprocessor.token> array = MatchArrayPostscript(tp);
                            struct_def.AddRange(array);
                            ret.AddRange(array);
                            t = tp.Current;


                            if (t.value != ";")
                                throw new Exception();
                            struct_def.Add(t);
                            ret.Add(t);

                            t = tp.MoveNext();
                            if (t.value == "}")
                            {
                                ret.Add(t);
                                break;
                            }
                        }

                        t = tp.MoveNext();

                        if (struct_name != null)
                            struct_defs.Add(struct_name, struct_def);
                    }
                    else if (struct_name == null)
                        throw new Exception("Invalid struct definition");
                    else if (!struct_defs.ContainsKey(struct_name))
                        throw new Exception("struct " + struct_name + " is not defined");

                    return ret;
                }
                else
                    throw new Exception("Invalid type: " + t.value);
            }
        }

        private static List<Preprocessor.token> MatchArrayPostscript(TokenProvider tp)
        {
            List<Preprocessor.token> ret = new List<Preprocessor.token>();
            Preprocessor.token t = tp.Current;
            if (t.value == "[")
            {
                ret.Add(t);
                t = tp.MoveNext();
                if (t.type == Preprocessor.token.token_type.number)
                {
                    ret.Add(t);
                    t = tp.MoveNext();
                }

                if(t.value != "]")
                    throw new Exception();
                ret.Add(t);
                t = tp.MoveNext();
            }
            return ret;
        }

        internal class ParsedEntry
        {
            internal enum type { function_definition, function_declaration, variable_definition, statement, expression, file, typedef };
            internal type Type;
            internal List<Preprocessor.token> Tokens;
            internal List<ParsedEntry> Children = new List<ParsedEntry>();
        }
    }
}
