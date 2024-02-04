using System;
using System.Collections.Generic;

namespace RedSaw.CommandLineInterface{

    /// <summary>
    /// token type of lexer
    /// </summary>
    public enum TokenType{

        TK_NONE,            // nothing 
        TK_EOF,             // end of file
        TK_VAR,             // @<name>
        TK_INPUT,           // input
        TK_STRING,          // "string"
        TK_TRUE,            // true
        TK_FALSE,           // false
        TK_NULL,            // null
        TK_INT,             // 123
        TK_FLOAT,           // 1.3        
        TK_ASSIGN,          // =
        TK_DOT,             // .
        TK_COMMA,           // ,
        TK_COLON,           // :
        TK_L_BRACKET,       // [
        TK_R_BRACKET,       // ]
        TK_L_PAREN,         // (
        TK_R_PAREN,         // )
    }


    /// <summary>
    /// lexer token
    /// </summary>
    public readonly struct Token{

        public readonly TokenType type;
        public readonly int sourceIdx;
        public readonly int startIdx;
        public readonly int endIdx;

        public Token(TokenType type, int sourceId, int startIdx, int endIdx){

            this.type = type;
            this.sourceIdx = sourceId;
            this.startIdx = startIdx;
            this.endIdx = endIdx;
        }
    }

    /// <summary>
    /// lexer result, contains a group of LexerResult
    /// </summary>
    public readonly struct LexerResult{

        public readonly string source;
        public readonly Token[] tokens;
        public readonly string[] sourceBuffer;
        public readonly Token this[int index] => tokens[index];
        public readonly Token LastToken => tokens[^1];

        public LexerResult(string source, Token[] tokens, string[] sourceBuffer){

            this.source = source;
            this.tokens = tokens;
            this.sourceBuffer = sourceBuffer;
        }
        public readonly void Debug(Action<string> log){
                
            foreach(var tk in tokens){
                var src = tk.sourceIdx >= 0 ? sourceBuffer[tk.sourceIdx] : string.Empty;
                log($"<{tk.type}> {src}");
            }
        }
        public readonly string NearInfo(Token token, int radius = 7){

            int left = Math.Max(0, token.startIdx - radius);
            int right = Math.Min(token.endIdx + radius, source.Length - 1);
            return source[left..right];
        }
        public readonly string NearInfo(int LB, int RB){

            int left = Math.Max(0, LB);
            int right = Math.Min(RB, source.Length - 1);
            return source[left..right];
        }
    }

    public partial class Lexer{

        /// <summary>
        /// check if a string value is an id
        /// which starts with '_' or letter and contains only '_' or letter or digit
        /// </summary>
        public static bool IsId(string value){

            if(value == null || value.Length == 0)return false;
            char head = value[0];
            if(head == UNDERLINE || char.IsLetter(head)){
                for(int i = 1; i < value.Length; i ++){
                    char c = value[i];
                    if(c == UNDERLINE || char.IsLetterOrDigit(c))continue;
                }
                return true;
            }
            return false;
        }

        // SPECIAL CHARACTERS
        public const char EOL = '\n';
        public const char EOF = '\0';
        public const char TAB = '\t';
        public const char CR = '\r';
        public const char WHITE_SPACE = ' ';

        // SYMBOLS
        public const char VAR = '@';
        public const char DOT = '.';
        public const char COLON = ':';
        public const char COMMA = ',';
        public const char ASSIGN = '=';
        public const char L_PAREN = '(';
        public const char R_PAREN = ')';
        public const char L_BRACKET = '[';
        public const char R_BRACKET = ']';
        public const char SINGLE_QUOTE = '\'';
        public const char DOUBLE_QUOTE = '"';
        public const char UNDERLINE = '_';
        public const char NEGATIVE = '-';
        public const char E = 'e';


        // KEYWORDS
        public const string TRUE = "true";
        public const string FALSE = "false";
        public const string NULL = "null";

        /// <summary>
        /// use to preprocess the source input, the source input could only contains
        /// 'EOF' and 'WHITE_SPACE'
        /// </summary>
        public static char[] WHITE_SPACE_CHARS = { EOL, EOF, TAB, CR };
        public static char[] SYMBOLS = { VAR, DOT, COLON, COMMA, ASSIGN, L_PAREN, R_PAREN, L_BRACKET, R_BRACKET, SINGLE_QUOTE, DOUBLE_QUOTE };
        public static char[] ID_TERMINATORS = { WHITE_SPACE, EOF, DOT, COLON, COMMA, ASSIGN, L_PAREN, R_PAREN, L_BRACKET, R_BRACKET, SINGLE_QUOTE, DOUBLE_QUOTE };

        public static bool IsWhiteSpace(char c){

            foreach(char cc in WHITE_SPACE_CHARS){
                if( c == cc )return true;
            }
            return char.IsWhiteSpace(c);
        }

        bool IsInputTerminator(char c){
                
            foreach(char t in ID_TERMINATORS)if(c == t)return true;
            return false;
        }
        /// <summary>remove all white space in target source input</summary>
        string PreProcess(string input){
    
            foreach(char c in WHITE_SPACE_CHARS)input = input.Replace(c, WHITE_SPACE);
            return input + EOF;
        }
    }

    /// <summary>
    /// lexer main logic
    /// </summary>
    public partial class Lexer{

        int index;
        string src;
        readonly List<string> tokenValues = new();
        readonly List<Token> tokens = new();
        bool HasMore => index < src.Length;

        /// <summary>parse input string to tokens as LexerResult</summary>
        /// <param name="input">input string</param>
        /// <param name="result">LexerResult</param>
        public LexerResult Parse(string input){
            
            // check if target input is null or empty
            if(input == null || input.Length == 0)
                throw new CommandLexerException("Input string is empty");

            // preprocess to remove all white space, and recheck if target input is empty
            var pp = PreProcess(input);
            if(pp.Length == 0)
                throw new CommandLexerException("Input string is invalid");

            // start to parse
            return Walk(pp);
        }
        LexerResult Walk(string input){

            src = input;

            index = 0;
            tokens.Clear();
            tokenValues.Clear();

            while(HasMore){
                int startIdx = index;
                char c = src[ index ++ ];
                switch(c){

                    // skip white space
                    case WHITE_SPACE:break;

                    // stop at the end of file
                    case EOF:
                        tokens.Add(new Token(TokenType.TK_EOF, -1, startIdx, index));
                        break;

                    /* 
                        while start with '@', it would be treated as a property which 
                        could be visit directly by '@' in command line
                    */
                    case VAR:
                        string varName = NextInput(index);
                        tokens.Add(new Token(TokenType.TK_VAR, tokenValues.Count, startIdx, index));
                        tokenValues.Add(varName);
                        break;

                    // string of double quote
                    case DOUBLE_QUOTE:
                    case SINGLE_QUOTE:
                        string str = NextString(index, c);
                        tokens.Add(new Token(TokenType.TK_STRING, tokenValues.Count, startIdx, index));
                        tokenValues.Add(str);
                        break;

                    // :
                    case COLON:
                        tokens.Add(new Token(TokenType.TK_COLON, -1, startIdx, index));
                        break;

                    // ,
                    case COMMA:
                        startIdx = index - 1;
                        tokens.Add(new Token(TokenType.TK_COMMA, -1, startIdx, index));
                        break;

                    // .
                    case DOT:
                        tokens.Add(new Token(TokenType.TK_DOT, -1, startIdx, index));
                        break;

                    // =
                    case ASSIGN:
                        tokens.Add(new Token(TokenType.TK_ASSIGN, -1, startIdx, index));
                        break;
                        
                    // (
                    case L_PAREN:
                        tokens.Add(new Token(TokenType.TK_L_PAREN, -1, startIdx, index));
                        break;

                    // )
                    case R_PAREN:
                        tokens.Add(new Token(TokenType.TK_R_PAREN, -1, startIdx, index));
                        break;
                    
                    // [
                    case L_BRACKET:
                        tokens.Add(new Token(TokenType.TK_L_BRACKET, -1, startIdx, index));
                        break;
                    
                    // ]
                    case R_BRACKET:
                        tokens.Add(new Token(TokenType.TK_R_BRACKET, -1, startIdx, index));
                        break;

                    // input string
                    default:
                        // number
                        if(c == NEGATIVE || char.IsDigit(c)){
                            string number = NextNumber(index, out bool isFloat);
                            tokens.Add(new Token(isFloat ? TokenType.TK_FLOAT : TokenType.TK_INT, tokenValues.Count, startIdx, index));
                            tokenValues.Add(number);
                            break;
                        }

                        var nextInput = NextInput(index - 1);

                        // keywords
                        switch(nextInput){
                            case TRUE:
                                tokens.Add(new Token(TokenType.TK_TRUE, -2, startIdx, index));
                                break;
                            case FALSE:
                                tokens.Add(new Token(TokenType.TK_FALSE, -3, startIdx, index));
                                break;
                            case NULL:
                                tokens.Add(new Token(TokenType.TK_NULL, -4, startIdx, index));
                                break;
                        }

                        // default
                        tokens.Add(new Token(TokenType.TK_INPUT, tokenValues.Count, startIdx, index));
                        tokenValues.Add(nextInput);
                        break;
                }
            }

            return new LexerResult(input, tokens.ToArray(), tokenValues.ToArray());
        }
        string NextInput(int startIdx){

            while(HasMore){
                char c = src[index ++ ];
                if(IsInputTerminator(c)){
                    index -- ;
                    break;
                }
            }
            return src[startIdx..index];
        }
        string NextString(int startIdx, char quoteChar){

            while(HasMore){
                char c = src[index ++ ];
                if(c == quoteChar){
                    return src[startIdx..(index - 1)];
                }
            }
            var nearStr = src[startIdx..index];
            throw new CommandLexerException($"String not terminated near <{nearStr}>");
        }
        string NextNumber(int startIdx, out bool isFloat){

            while(HasMore){
                char c = src[index ++];
                if(c == DOT){
                    isFloat = true;
                    return NextFloat(startIdx);
                }
                if(char.IsDigit(c))continue;
                index -- ;
                break;
            }
            isFloat = false;
            return src[(startIdx - 1)..index];
        }
        string NextFloat(int startIdx){
                
            while(HasMore){
                char c = src[index ++ ];
                if(char.IsDigit(c))continue;
                if(c == E)return NextExponent(startIdx);
                index -- ;
                break;
            }
            return src[(startIdx - 1)..index];
        }
        string NextExponent(int startIdx){

            while(HasMore){
                char c = src[index ++ ];
                if(char.IsDigit(c))continue;
                index -- ;
                break;
            }
            return src[(startIdx - 1)..index];
        }

    }


}