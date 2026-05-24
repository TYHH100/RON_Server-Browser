using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using RON_Server_Browser.Services;

namespace RON_Server_Browser.Extensions;

[MarkupExtensionReturnType(typeof(string))]
public class LocalizeExtension : MarkupExtension
{
    public LocalizeExtension() { }

    public LocalizeExtension(string key)
    {
        Key = key;
    }

    [ConstructorArgument("key")]
    public string? Key { get; set; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (string.IsNullOrEmpty(Key))
            return string.Empty;

        string value = LocalizationService.Instance.GetString(Key);

        var binding = new Binding
        {
            Source = LocalizationService.Instance,
            Path = new PropertyPath("CurrentLanguage"),
            Mode = BindingMode.OneWay
        };

        var multiBinding = new MultiBinding
        {
            Converter = new LocalizeConverter(Key)
        };

        multiBinding.Bindings.Add(binding);

        return multiBinding.ProvideValue(serviceProvider);
    }
}

public class LocalizeConverter : IMultiValueConverter
{
    private readonly string _key;

    public LocalizeConverter(string key)
    {
        _key = key;
    }

    public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        return LocalizationService.Instance.GetString(_key);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}