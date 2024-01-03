using System;

namespace RedSaw.CommandLineInterface{

    /// <summary>
    /// command attribute
    /// <para>any static method has attach this attribute would be treated
    /// as a command method, and it woule be collected by command system automatically</para>
    /// </summary>
    public class CommandAttribute : Attribute{
            
        public string Name { get; private set; }
        public string Desc { get; set; }

        public CommandAttribute(string name){

            this.Name = name;
            this.Desc = "command has no description";
        }
        public CommandAttribute(){

            this.Name = null;
            this.Desc = "command has no description";
        }
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