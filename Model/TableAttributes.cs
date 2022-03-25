using System;

namespace Truffle.Model
{
    [AttributeUsage( AttributeTargets.Property )]
    public class ColumnAttribute : Attribute {
        public ColumnAttribute(string name) {
            this.theName = name;
        }

        protected string theName;

        public string Name {
            get { return theName; }
            set { theName = value; }
        }
    }

    [AttributeUsage( AttributeTargets.Property )]
    public class IdAttribute : Attribute {
    }

    [AttributeUsage( AttributeTargets.Class )]
    public class TableAttribute : Attribute {
        public TableAttribute(string name) {
            this.theName = name;
        }

        protected string theName;

        public string Name {
            get { return theName; }
            set { theName = value; }
        }
    }
}