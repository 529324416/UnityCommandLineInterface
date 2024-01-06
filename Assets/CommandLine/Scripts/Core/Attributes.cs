using System;

namespace RedSaw.CommandLineInterface{

    /// <summary>
    /// command attribute
    /// <para>any static method has attach this attribute would be treated
    /// as a command method, and it woule be collected by command system automatically</para>
    /// </summary>
    public class CommandAttribute : Attribute{

        /// <summary>
        /// name of command
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// description of command
        /// </summary>
        public string Desc { get; set; }

        /// <summary>
        /// tag of command, for you can query commands by tag, or constraint commands by tag
        /// </summary>
        public string Tag { get; set; }

        public CommandAttribute(string name){

            this.Name = name;
            this.Tag = null;
            this.Desc = "command has no description";
        }
        public CommandAttribute(){

            this.Name = null;
            this.Tag = null;
            this.Desc = "command has no description";
        }
    }

    /// <summary>
    /// the command defined in core module
    /// it could be dismiss by client
    /// </summary>
    class DefaultCommandAttribute : CommandAttribute{

        public DefaultCommandAttribute(string name) : base(name){}
        public DefaultCommandAttribute() : base(){}
    }

    /// <summary>
    /// a static method which defined this attribute would be treated as
    /// parameter parser.
    /// </summary>
    public class CommandParameterParserAttribute : Attribute{

        public readonly Type type;
        public CommandParameterParserAttribute(Type type){
            this.type = type;
        }
    }
}