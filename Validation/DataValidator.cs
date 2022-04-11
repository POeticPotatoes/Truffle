using System;
using Truffle.Model;

namespace Truffle.Validation
{
    [AttributeUsage( AttributeTargets.Property )]
    public abstract class DataValidatorAttribute: Attribute {
        private string Message{get;set;}
        public abstract bool Validate(object value, SqlObject model);
        public string GetMessage()
        {
            return this.Message;
        }

        public void SetMessage(string m)
        {
            this.Message = m;
        }
    }

    public class SimpleStringAttribute: DataValidatorAttribute {

        public override bool Validate(object value, SqlObject model)
        {
            if (value == null) return true;
            foreach(var c in (string) value)
            {
                if ((c > 44 && c < 58)
                || (c > 64 && c < 91)
                || (c > 96 && c < 123)) continue;

                this.SetMessage($"Invalid character '{c}'");
                return false;
            }
            return true;
        }
    }

    public class MinValueAttribute: DataValidatorAttribute {
        private readonly double value;

        public MinValueAttribute(double value)
        {
            this.value = value;
        }

        public override bool Validate(object value, SqlObject model)
        {
            if (value == null) return true;
            if (Convert.ToDouble(value) >= this.value) return true;
            this.SetMessage($"{value} is less than the minimum value of {this.value}");
            return false;
        }
    }

    public class MaxValueAttribute: DataValidatorAttribute {
        private readonly double value;

        public MaxValueAttribute(double value)
        {
            this.value = value;
        }

        public override bool Validate(object value, SqlObject model)
        {
            if (value == null) return true;
            if (Convert.ToDouble(value) <= this.value) return true;
            this.SetMessage($"{value} is less than the minimum value of {this.value}");
            return false;
        }
    }

    public class RequiredAttribute: DataValidatorAttribute {

        public override bool Validate(object value, SqlObject model)
        {
            if (value != null 
                && (!typeof(string).IsInstanceOfType(value)) 
                || ((string)value).Length > 0) return true;

            this.SetMessage("Required field was left as null");
            return false;
        }
    }
}