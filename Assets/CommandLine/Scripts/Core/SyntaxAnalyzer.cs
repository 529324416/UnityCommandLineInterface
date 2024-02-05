using System;
using System.Linq;
using System.Collections.Generic;

namespace RedSaw.CommandLineInterface
{

    /// <summary>
    /// syntax tree code, tell VirualMachine how to execute given node
    /// </summary>
    public enum SyntaxTreeCode
    {

        FACTOR_STRING,
        FACTOR_FLOAT,
        FACTOR_TRUE,
        FACTOR_FALSE,
        FACTOR_NULL,
        FACTOR_INT,
        FACTOR_ID,
        FACTOR_INPUT,
        FACTOR_PARAMS,

        OP_SET_ELEMENT,
        OP_SET_FIELD,
        OP_LOADVAR,
        OP_ASSIGN,
        OP_INDEX,
        OP_CALL,
        OP_CVT,
        OP_DOT,
    }

    /// <summary>
    /// syntax tree, tree type, and represent the whole execution logic
    /// </summary>
    public class SyntaxTree
    {

        public readonly string data;
        public readonly SyntaxTreeCode opcode;
        public readonly SyntaxTree[] children;

        public SyntaxTree L => children[0];
        public SyntaxTree R => children[1];
        public bool IsLeaf => children.Length == 0;

        /// <summary>provide debug info about current node</summary>
        public string DebugInfo
        {
            get
            {
                return opcode switch
                {
                    SyntaxTreeCode.OP_DOT => $"{L?.DebugInfo}.{R?.DebugInfo}",
                    SyntaxTreeCode.OP_CALL => $"{L?.DebugInfo}{R?.DebugInfo}",
                    SyntaxTreeCode.OP_INDEX => $"{L?.DebugInfo}[{R?.DebugInfo}]",
                    SyntaxTreeCode.OP_ASSIGN => $"{L?.DebugInfo} = {R?.DebugInfo}",
                    SyntaxTreeCode.OP_SET_FIELD => $"{L?.DebugInfo}.{R?.DebugInfo} = {children[2]?.DebugInfo}",
                    SyntaxTreeCode.OP_CVT => $"{L?.DebugInfo}:{R?.DebugInfo}",
                    SyntaxTreeCode.FACTOR_PARAMS => $"({string.Join(", ", children.Select(x => x.DebugInfo))})",
                    SyntaxTreeCode.FACTOR_FLOAT => $"{data}f",
                    SyntaxTreeCode.FACTOR_STRING => data,
                    SyntaxTreeCode.FACTOR_TRUE => data,
                    SyntaxTreeCode.FACTOR_FALSE => data,
                    SyntaxTreeCode.FACTOR_NULL => data,
                    SyntaxTreeCode.FACTOR_INT => data,
                    SyntaxTreeCode.FACTOR_ID => "@" + data,
                    SyntaxTreeCode.FACTOR_INPUT => "?" + data,
                    _ => ToString(),
                };
            }
        }

        public SyntaxTree(SyntaxTreeCode opcode, string data)
        {

            this.opcode = opcode;
            this.data = data;
            this.children = Array.Empty<SyntaxTree>();
        }
        public SyntaxTree(SyntaxTreeCode opcode, string data, SyntaxTree L)
        {

            this.opcode = opcode;
            this.data = data;
            this.children = new SyntaxTree[] { L };
        }
        public SyntaxTree(SyntaxTreeCode opcode, string data, SyntaxTree L, SyntaxTree R)
        {

            this.opcode = opcode;
            this.data = data;
            this.children = new SyntaxTree[] { L, R };
        }
        public SyntaxTree(SyntaxTreeCode opcode, string data, SyntaxTree[] children)
        {

            this.opcode = opcode;
            this.data = data;
            this.children = children;
        }
        public override string ToString()
        {
            if (data != null || data.Length > 0) return data;
            return opcode.ToString();
        }
    }

    /// <summary>
    /// analyze lexer result and generate syntax tree
    /// </summary>
    public class SyntaxAnalyzer
    {

        LexerResult lexerResult;
        int index;

        /// <summary>
        /// indicate that while system has occur a 'command' parse process
        /// and if true, then the next input would be treated as command parameters
        /// </summary>
        bool __command_latch;

        Token[] Tokens => lexerResult.tokens;
        string[] TokenValues => lexerResult.sourceBuffer;

        public bool HasMore => index < Tokens.Length;
        public Token CurrentToken => Tokens[index];
        public Token NextToken => Tokens[index++];

        /// <summary>
        /// analyze lexer result and generate syntax tree
        /// </summary>
        /// <param name="lexerResult">lexer result</param>
        /// <returns>syntax tree</returns>
        /// <exception cref="CommandSyntaxException">when syntax error occured</exception>
        public SyntaxTree Analyze(LexerResult lexerResult)
        {

            this.lexerResult = lexerResult;

            index = 0;
            __command_latch = false;
            return NextExpr(true);
        }

        /// <summary>
        /// match current token type, if match, then move to next token
        /// </summary>
        bool Match(TokenType tokenType)
        {

            if (HasMore && CurrentToken.type == tokenType)
            {
                index++;
                return true;
            }
            return false;
        }
        /// <summary>
        /// only check if current token type is matched, don't move
        /// </summary>
        bool PeekMatch(TokenType tokenType)
        {
            return HasMore && CurrentToken.type == tokenType;
        }

        /// <summary>
        /// check if token is Input and is Id legal
        /// </summary>
        bool MatchId(out SyntaxTree factor)
        {

            var token = CurrentToken;
            if (token.type == TokenType.TK_INPUT && Lexer.IsId(TokenValues[token.sourceIdx]))
            {
                factor = new SyntaxTree(SyntaxTreeCode.FACTOR_ID, TokenValues[token.sourceIdx]);
                index++;
                return true;
            }
            factor = null;
            return false;
        }
        SyntaxTree NextParameterList()
        {

            if (Match(TokenType.TK_R_PAREN))
                return new SyntaxTree(SyntaxTreeCode.FACTOR_PARAMS, string.Empty);

            __command_latch = true;
            List<SyntaxTree> parameters = new();
            while (HasMore)
            {
                parameters.Add(NextExpr());
                if (Match(TokenType.TK_R_PAREN)) break;
                if (Match(TokenType.TK_COMMA)) continue;
            }
            __command_latch = false;
            return new SyntaxTree(SyntaxTreeCode.FACTOR_PARAMS, string.Empty, parameters.ToArray());
        }
        SyntaxTree NextParameterList_Command()
        {

            __command_latch = true;
            List<SyntaxTree> parameters = new();
            while (HasMore)
            {
                if (Match(TokenType.TK_EOF)) break;
                parameters.Add(NextExpr());
            }
            __command_latch = false;
            return new SyntaxTree(SyntaxTreeCode.FACTOR_PARAMS, string.Empty, parameters.ToArray());
        }
        SyntaxTree NextFactor()
        {

            Token tk = NextToken;
            switch (tk.type)
            {

                case TokenType.TK_INT:
                    return new SyntaxTree(SyntaxTreeCode.FACTOR_INT, TokenValues[tk.sourceIdx]);

                case TokenType.TK_FLOAT:
                    return new SyntaxTree(SyntaxTreeCode.FACTOR_FLOAT, TokenValues[tk.sourceIdx]);

                case TokenType.TK_STRING:
                    return new SyntaxTree(SyntaxTreeCode.FACTOR_STRING, TokenValues[tk.sourceIdx]);

                case TokenType.TK_NULL:
                    return new SyntaxTree(SyntaxTreeCode.FACTOR_NULL, Lexer.NULL);

                case TokenType.TK_TRUE:
                    return new SyntaxTree(SyntaxTreeCode.FACTOR_TRUE, Lexer.TRUE);

                case TokenType.TK_FALSE:
                    return new SyntaxTree(SyntaxTreeCode.FACTOR_FALSE, Lexer.FALSE);

                case TokenType.TK_VAR:
                    return new SyntaxTree(SyntaxTreeCode.OP_LOADVAR, TokenValues[tk.sourceIdx]);

                case TokenType.TK_INPUT:
                    /* 
                        all other input would be treated as command, if next token is '(', 
                        then treat as function call, otherwise treat as command
                    */
                    if (Match(TokenType.TK_L_PAREN))
                    {
                        var functionCmd = new SyntaxTree(SyntaxTreeCode.FACTOR_INPUT, TokenValues[tk.sourceIdx]);
                        return new SyntaxTree(SyntaxTreeCode.OP_CALL, string.Empty, functionCmd, NextParameterList());
                    }
                    if (__command_latch)
                    {
                        return new SyntaxTree(SyntaxTreeCode.FACTOR_INPUT, TokenValues[tk.sourceIdx]);
                    }
                    var commandCall = new SyntaxTree(SyntaxTreeCode.FACTOR_INPUT, TokenValues[tk.sourceIdx]);
                    return new SyntaxTree(SyntaxTreeCode.OP_CALL, string.Empty, commandCall, NextParameterList_Command());

                case TokenType.TK_L_PAREN:
                    SyntaxTree expr = NextExpr();
                    if (!Match(TokenType.TK_R_PAREN))
                    {
                        throw new CommandSyntaxException($"Missing ')' near \"..{lexerResult.NearInfo(tk)}\"");
                    }
                    return expr;
            }
            throw new CommandSyntaxException($"Unexpected token {tk.type} near \"..{lexerResult.NearInfo(tk)}\"");
        }
        SyntaxTree NextConverter()
        {

            var factor = NextFactor();
            if (Match(TokenType.TK_COLON))
            {
                var currentTk = NextToken;
                if (currentTk.type == TokenType.TK_STRING || currentTk.type == TokenType.TK_INPUT)
                {
                    var opFactor = new SyntaxTree(SyntaxTreeCode.FACTOR_INPUT, TokenValues[currentTk.sourceIdx]);
                    var opConverter = new SyntaxTree(SyntaxTreeCode.OP_CVT, string.Empty, factor, opFactor);
                    return opConverter;
                }
                throw new CommandSyntaxException($"Unexpected token near \"..{lexerResult.NearInfo(currentTk)}\"");
            }
            return factor;
        }
        SyntaxTree NextAccess()
        {

            SyntaxTree tmp = NextConverter();
            while (HasMore)
            {
                int startPosition = CurrentToken.startIdx;

                // dot
                if (Match(TokenType.TK_DOT))
                {
                    if (MatchId(out var id))
                    {

                        // function call
                        if (Match(TokenType.TK_L_PAREN))
                        {
                            var callable = new SyntaxTree(SyntaxTreeCode.OP_DOT, string.Empty, tmp, id);
                            tmp = new SyntaxTree(SyntaxTreeCode.OP_CALL, string.Empty, callable, NextParameterList());
                            continue;
                        }

                        tmp = new SyntaxTree(SyntaxTreeCode.OP_DOT, string.Empty, tmp, id);
                        continue;
                    }
                    throw new CommandSyntaxException($"Unexpected token near \"..{lexerResult.NearInfo(startPosition, CurrentToken.endIdx)}\"");
                }

                // index
                if (Match(TokenType.TK_L_BRACKET))
                {
                    SyntaxTree expr = NextExpr();
                    if (!Match(TokenType.TK_R_BRACKET))
                    {
                        throw new CommandSyntaxException($"Missing ']' near \"..{lexerResult.NearInfo(startPosition, CurrentToken.endIdx)}\"");
                    }
                    tmp = new SyntaxTree(SyntaxTreeCode.OP_INDEX, string.Empty, tmp, expr);
                    continue;
                }
                break;
            }
            return tmp;
        }
        public SyntaxTree NextExpr(bool isRoot = false)
        {

            SyntaxTree access = NextAccess();
            if (!HasMore || PeekMatch(TokenType.TK_EOF))
                return access;

            if (Match(TokenType.TK_ASSIGN))
            {

                // instance.fieldName = <expr>
                if (access.opcode == SyntaxTreeCode.OP_DOT)
                {
                    SyntaxTree expr = NextExpr();
                    return new SyntaxTree(SyntaxTreeCode.OP_SET_FIELD, string.Empty, new SyntaxTree[]{
                        access.children[0],
                        access.children[1],
                        expr
                    });
                }

                // instance["test"] = 10 or instance[10] = 10
                if (access.opcode == SyntaxTreeCode.OP_INDEX)
                {
                    SyntaxTree expr = NextExpr();
                    return new SyntaxTree(SyntaxTreeCode.OP_SET_ELEMENT, string.Empty, new SyntaxTree[]{
                        access.children[0],
                        access.children[1],
                        expr
                    });
                }

                return new SyntaxTree(SyntaxTreeCode.OP_ASSIGN, string.Empty, access, NextExpr());
            }
            if (isRoot)
            {
                throw new CommandSyntaxException($"Unexpected token {CurrentToken.type} near \"..{lexerResult.NearInfo(CurrentToken)}\"");
            }
            return access;
        }
    }
}