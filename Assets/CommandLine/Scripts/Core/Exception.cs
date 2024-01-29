namespace RedSaw.CommandLineInterface{


    /// <summary>
    /// command system exception
    /// </summary>
    public class CommandSystemException : System.Exception{
        public CommandSystemException(string message) : base(message){}
    }

    /// <summary>
    /// command lexer exception
    /// </summary>
    public class CommandLexerException : CommandSystemException{
        public CommandLexerException(string message) : base(message){}
    }

    /// <summary>
    /// command syntax exception
    /// </summary>
    public class CommandSyntaxException : CommandSystemException{
        public CommandSyntaxException(string message) : base(message){}
    }

    /// <summary>
    /// command execute exception
    /// </summary>
    public class CommandExecuteException : CommandSystemException{
        public CommandExecuteException(string message) : base(message){}
    }
}