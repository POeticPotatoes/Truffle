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
        private string Message{get;set;}

        /// <summary>
        /// Validates a value by performing checks on it
        /// </summary>
        /// <param name="value">The value to be checked</param>
        /// <param name="model">The SqlObject being validated</param>
        /// <returns><Whether the validation passed/returns>
        public abstract bool Validate(object value, SqlObject model);

        /// <summary>
        /// Returns any additional messages created during validation
        /// </summary>
        public string GetMessage()
        {
            return this.Message;
        }

        /// <summary>
        /// Sets an additional message returned by a validation attempt
        /// </summary>
        public void SetMessage(string m)
        {
            this.Message = m;
        }
    }

    /// <summary>
    /// Checks that a string only contains upper and lowercase letters, . / and -
    /// </summary>
    public class SimpleStringAttribute: DataValidatorAttribute 
    {
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

        public override bool Validate(object value, SqlObject model)
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

        public override bool Validate(object value, SqlObject model)
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
        public override bool Validate(object value, SqlObject model)
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

        public override bool Validate(object obj, SqlObject model)
        {
            if (obj == null) return true;
            if (expression.Match((string) obj).Success) return true;
            this.SetMessage($"'{obj}' did not match the regular expression '{expression}'");
            return false;
        }
    }
}