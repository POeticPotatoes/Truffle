using System;

namespace Truffle.Model
{
    /// <summary>
    /// Flags an object property as a column in a database
    /// </summary>
    [AttributeUsage( AttributeTargets.Property )]
    public class ColumnAttribute : Attribute 
    {
        public string Name {get;}

        /// <summary>
        /// Flags an object property as a column in a database
        /// </summary>
        /// <param name="name">The name of the column</param>
        public ColumnAttribute(string name) 
        {
            Name = name;
        }
    }

    /// <summary>
    /// Flags a class as a key in a database.
    /// </summary>
    [AttributeUsage( AttributeTargets.Property )]
    public class IdAttribute : Attribute {}

    /// <summary>
    /// Flags a class as a representation of a table in a database.
    /// </summary>
    [AttributeUsage( AttributeTargets.Class )]
    public class TableAttribute : Attribute 
    {
        public string Name {get;}


        /// <summary>
        /// Flags a class as a representation of a table in a database.
        /// </summary>
        /// <param name="name">The name of the table</param>
        public TableAttribute(string name) 
        {
            Name = name;
        }
    }

    [AttributeUsage( AttributeTargets.Property)]
    public class IdentityAttribute: Attribute {}

    [AttributeUsage( AttributeTargets.Property)]
    public class OptionalAttribute: Attribute {}
}
