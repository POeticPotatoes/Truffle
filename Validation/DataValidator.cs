using System;
using System.Text.RegularExpressions;
using Truffle.Model;

namespace Truffle.Validation
{
    /// <summary>
    /// An abstract attribute class that can be extended to implement methods for validating data
    /// </summary>
    [AttributeUsage( AttributeTargets.Property )]
    public abstract class DataValidatorAttribute: Attribute 
    {
        private string message{get;set;}

        /// <summary>
        /// Validates a value by performing checks on it
        /// </summary>
        /// <param name="value">The value to be checked</param>
        /// <param name="model">The SqlObject being validated</param>
        /// <returns><Whether the validation passed/returns>
        public abstract bool Validate(string name, object value, SqlObject model);

        /// <summary>
        /// Returns any additional messages created during validation
        /// </summary>
        public string GetMessage()
        {
            if (message != null) return message;
            return "Data was not valid";
        }

        /// <summary>
        /// Sets an additional message returned by a validation attempt
        /// </summary>
        public void SetMessage(string m)
        {
            this.message = m;
        }
    }

    /// <summary>
    /// Checks that a string only contains upper and lowercase letters, . / and -
    /// </summary>
    public class SimpleStringAttribute: DataValidatorAttribute 
    {
        public override bool Validate(string name, object value, SqlObject model)
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

    /// <summary>
    /// Checks that a number is not any lower than a specified value
    /// </summary>
    public class MinValueAttribute: DataValidatorAttribute 
    {
        private readonly double value;

        /// <summary>
        /// Initiates an instance of a MinValueAttribute with the minimum value to accept
        /// </summary>
        public MinValueAttribute(double value)
        {
            this.value = value;
        }

        public override bool Validate(string name, object value, SqlObject model)
        {
            if (value == null) return true;
            if (Convert.ToDouble(value) >= this.value) return true;
            this.SetMessage($"{value} is less than the minimum value of {this.value}");
            return false;
        }
    }

    /// <summary>
    /// Checks that a number does not exceed a specified value
    /// </summary>
    public class MaxValueAttribute: DataValidatorAttribute 
    {
        private readonly double value;

        /// <summary>
        /// Initiates an instance of a MaxValueAttribute with the maximum value to accept
        /// </summary>
        public MaxValueAttribute(double value)
        {
            this.value = value;
        }

        public override bool Validate(string name, object value, SqlObject model)
        {
            if (value == null) return true;
            if (Convert.ToDouble(value) <= this.value) return true;
            this.SetMessage($"{value} is less than the minimum value of {this.value}");
            return false;
        }
    }

    /// <summary>
    /// Checks that a value is not null or an empty string
    /// </summary>
    public class RequiredAttribute: DataValidatorAttribute 
    {
        public override bool Validate(string name, object value, SqlObject model)
        {
            if (value != null 
                && (!typeof(string).IsInstanceOfType(value)
                || ((string)value).Length > 0)) return true;

            this.SetMessage("Required field was left blank");
            return false;
        }
    }

    /// <summary>
    /// Validates a string against a regular expression
    /// </summary>
    public class RegexValidationAttribute: DataValidatorAttribute
    {
        private readonly Regex expression;

        /// <summary>
        /// Initiates an instance of a RegexValidationAttribute with the regular expression to match
        /// </summary>
        public RegexValidationAttribute(string str)
        {
            this.expression = new Regex(str);
        }

        public override bool Validate(string name, object obj, SqlObject model)
        {
            if (obj == null) return true;
            if (expression.Match((string) obj).Success) return true;
            this.SetMessage($"'{obj}' did not match the regular expression '{expression}'");
            return false;
        }
    }

    public class MatchStringAttribute: DataValidatorAttribute
    {
        private readonly string[] valid;

        public MatchStringAttribute(params string[] valid) 
        {
            this.valid = valid;
        }

        public override bool Validate(string name, object value, SqlObject model)
        {
            if (value == null) return true;
            foreach (var str in valid)
                if (str.Equals(value)) return true;
            this.SetMessage($"{value} was not a valid string. Accepted values are {String.Join(", ",valid)}");
            return false;
        }
    }

    public class EqualsAttribute: DataValidatorAttribute
    {
        private readonly string str;

        public EqualsAttribute(string name)
        {
            this.str = name;
        }

        public override bool Validate(string name, object value, SqlObject model)
        {
            var raw = GetValue(model);
            if (value.Equals(raw)) return true;
            SetMessage($"{value} of column {name} did not match the desired value of {raw} from {str}");
            return false;
        }

        private object GetValue(SqlObject model)
        {
            if (typeof(PartialSqlObject).IsInstanceOfType(model))
                return ((PartialSqlObject) model).GetValue(str);
            var p = model.GetType().GetProperty(str);
            if (p == null) return null;
            return p.GetValue(model);
        }
        
    }
}
