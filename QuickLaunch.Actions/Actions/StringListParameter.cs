using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using QuickLaunch.Core.Utils;
using QuickLaunch.Core.Utils.QuotedString;

namespace QuickLaunch.Core.Actions;


[TypeConverter(typeof(StringListParameterConverter))]
public record StringListParameter(List<string> List)
{
    public StringListParameter() : this(new List<string>())
    {
    }

    public StringListParameter(IEnumerable<string> list) : this(list.ToList())
    {
    }
}

public class StringListParameterConverter : TypeConverter
{
    #region ----- Properties. -----

    private readonly MaybeQuotedStringListTypeConverter _converter = new();

    public MaybeQuotedStringListTypeConverter MQSLConverter => _converter;


    #endregion

    #region ----- TypeConverter. -----
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return _converter.CanConvertFrom(context, sourceType);
    }

    public override bool CanConvertTo(ITypeDescriptorContext? context, [NotNullWhen(true)] Type? destinationType)
    {
        return _converter.CanConvertTo(context, destinationType);
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        var result = (List<string>?)_converter.ConvertFrom(context, culture, value);
        return result.Map(list => new StringListParameter(list));
    }

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        return value.Map((StringListParameter p) => _converter.ConvertTo(context, culture, p.List, destinationType), null);
    }

    #endregion

}
