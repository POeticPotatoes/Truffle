using System;
using System.Text;
using Truffle.Model;

namespace Truffle.Validation
{
    /// <summary>
    /// An abstract attribute class that can be extended to implement methods for cleaning and converting data
    /// </summary>
    [AttributeUsage( AttributeTargets.Property )]
    public abstract class DataCleanerAttribute: Attribute {
        /// <summary>
        /// Parses a value from a column and returns the result
        /// </summary>
        /// <param name="value">The value to be parsed</param>
        /// <param name="model">The SqlObject being cleaned</param>
        /// <returns></returns>
        public abstract object Clean(object value, SqlObject model);
    }

    /// <summary>
    /// Rounds a double value to a desired number of decimals
    /// </summary>
    public class DecimalsAttribute: DataCleanerAttribute
    {
        private readonly int places;

        /// <summary>
        /// Initiates an instance of a DecimalsAttribute with the number of decimal places desired
        /// </summary>
        public DecimalsAttribute(int i)
        {
            places = i;
        }

        public override object Clean(object value, SqlObject model)
        {
            if (value == null) return null;
            var val = Convert.ToDouble(value);
            return Math.Round(val, places);
        }
    }

    /// <summary>
    /// Simplifies a string such that it only contains upper and lowercase letters, . / and -
    /// </summary>
    public class SimplifyStringAttribute: DataCleanerAttribute {

        public override object Clean(object value, SqlObject model)
        {
            if (value == null) return null;
            var str = (string) value;
            StringBuilder builder = new StringBuilder(str);
            for (var i=0; i< str.Length; i++)
            {
                var c = str[i];
                if ((c > 44 && c < 58)
                || (c > 64 && c < 91)
                || (c > 96 && c < 123)) continue;

                builder[i] = '-';
            }
            return builder.ToString();
        }
    }
}