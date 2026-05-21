using System;
using System.Windows.Data;
using System.Windows.Markup;

namespace Notification.Wpf.Converters
{
    [ValueConversion(typeof(double), typeof(double)), MarkupExtensionReturnType(typeof(ParameterMathConverter))]
    internal abstract class ParameterMathConverter : DoubleValueConverter
    {
        private readonly Func<double, double, double> _To;
        private readonly Func<double, double, double> _From;

        public double Parameter { get; set; }

        protected ParameterMathConverter(double Parameter, Func<double, double, double> to, Func<double, double, double> from = null)
        {
            this.Parameter = Parameter;
            _To = to;
            _From = from;
        }

        protected override double To(double v, object p) => _To(v, Parameter);

        protected override double From(double v, object p) => (_From ?? throw new NotSupportedException()).Invoke(v, Parameter);
    }
}