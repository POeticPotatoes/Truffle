using System;
using System.Text;
using Truffle.Model;

namespace Truffle.Validation
{
    [AttributeUsage( AttributeTargets.Property )]
    public abstract class DataCleanerAttribute: Attribute {
        public abstract object Clean(object value, SqlObject model);
    }

    public class DecimalAttribute: DataCleanerAttribute
    {
        private readonly int places;

        public DecimalAttribute(int i)
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