using System;
using System.Globalization;
using System.Windows.Controls;

namespace wpfTDX
{


    public class RangeValidationRule : ValidationRule
    {
        public double Min { get; set; }
        public double Max { get; set; }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            // Allow nulls (empty cell) — remove this if you want to require a value
            if (value == null || value == System.Windows.DependencyProperty.UnsetValue)
                return ValidationResult.ValidResult;

            double d;
            // value may already be a double/double? after your converter
            if (value is double)
            {
                d = (double)value;
            }
            else if (value is double?)
            {
                var dn = (double?)value;
                if (!dn.HasValue) return ValidationResult.ValidResult;
                d = dn.Value;
            }
            else
            {
                // fallback parse (should be rare if converter is in place)
                if (!double.TryParse(Convert.ToString(value, cultureInfo), NumberStyles.Float, cultureInfo, out d))
                    return new ValidationResult(false, "Invalid number.");
            }

            if (d < Min || d > Max)
                return new ValidationResult(false, string.Format("Value must be between {0} and {1}.", Min, Max));

            return ValidationResult.ValidResult;
        }
    }

}
