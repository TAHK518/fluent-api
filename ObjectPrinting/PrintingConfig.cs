using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using NUnit.Framework;

namespace ObjectPrinting
{
    public class PrintingConfig<TOwner> : IPrintingConfig
    {
        private readonly List<Type> excludingTypes;
        private readonly Dictionary<Type, Delegate> customPrint;
        private readonly List<PropertyInfo> excludingFields;

        public PrintingConfig()
        {
            this.excludingTypes = new List<Type>();
            customPrint = new Dictionary<Type, Delegate>();
            excludingFields = new List<PropertyInfo>();
        }

        public string PrintToString(TOwner obj)
        {
            return PrintToString(obj, 0);
        }

        private string PrintToString(object obj, int nestingLevel)
        {
            //TODO apply configurations
            if (obj == null)
                return "null" + Environment.NewLine;

            var finalTypes = new[]
            {
                typeof(int), typeof(double), typeof(float), typeof(string),
                typeof(DateTime), typeof(TimeSpan)
            };
            if (finalTypes.Contains(obj.GetType()))
                return obj + Environment.NewLine;
            var identation = new string('\t', nestingLevel + 1);
            var sb = new StringBuilder();
            var type = obj.GetType();
            sb.AppendLine(type.Name);
            foreach (var propertyInfo in type.GetProperties())
            {
                if (excludingTypes.Contains(propertyInfo.PropertyType) || excludingFields.Contains(propertyInfo))
                    continue;
                if (customPrint.ContainsKey(propertyInfo.PropertyType))
                {
                    sb.Append(identation + propertyInfo.Name + " = " +
                              customPrint[propertyInfo.PropertyType].DynamicInvoke(propertyInfo.GetValue(obj)));
                    sb.Append(Environment.NewLine);
                }
                else
                {
                    sb.Append(identation + propertyInfo.Name + " = " +
                              PrintToString(propertyInfo.GetValue(obj),
                                  nestingLevel + 1));
                }
            }
            return sb.ToString();
        }

        public PrintingConfig<TOwner> Excluding<T>()
        {
            excludingTypes.Add(typeof(T));
            return this;
        }

        public PropertyPrintingConfig<TOwner, T> ChangePrintFor<T>()
        {
            return new PropertyPrintingConfig<TOwner, T>(this);
        }

        public PropertyPrintingConfig<TOwner, T> ChangePrintFor<T>(Expression<Func<TOwner, T>> func)
        {
            var propInfo = ((MemberExpression) func.Body).Member as PropertyInfo;
            return new PropertyPrintingConfig<TOwner, T>(this);
        }

        public PrintingConfig<TOwner> Excluding<T>(Expression<Func<TOwner, T>> func)
        {
            var propInfo = ((MemberExpression)func.Body).Member as PropertyInfo;
            excludingFields.Add(propInfo);
            return this;
        }
        Dictionary<Type, Delegate> IPrintingConfig.�ustomPrints => customPrint;
    }

    public class PropertyPrintingConfig<TOwner, TProperty> : IPropertyPrintingConfig<TOwner>
    {
        private readonly PrintingConfig<TOwner> parentConfig;
        public PropertyPrintingConfig(PrintingConfig<TOwner> parentConfig)
        {
            this.parentConfig = parentConfig;
        }

        public PrintingConfig<TOwner> Using(Func<TProperty, string> serializationFunc)
        {
            (parentConfig as IPrintingConfig).�ustomPrints[typeof(TProperty)] = serializationFunc;
            return parentConfig;
        }

        PrintingConfig<TOwner> IPropertyPrintingConfig<TOwner>.ParentConfig => this.parentConfig;
    }

    public static class PropertyPrintingConfigExtensions
    {
        public static PrintingConfig<TOwner> Using<TOwner>(this PropertyPrintingConfig<TOwner, int> config, CultureInfo currentCulture)
        {
            return (config as IPropertyPrintingConfig<TOwner>).ParentConfig;
        }

        public static PrintingConfig<TOwner> TrimToLength<TOwner>(this PropertyPrintingConfig<TOwner, string> config, int length)
        {
            var func = new Func<string, string>((a) => a.Substring(0, a.Length > length ? length : a.Length));
            ((config as IPropertyPrintingConfig<TOwner>).ParentConfig as IPrintingConfig).�ustomPrints[typeof(string)] = func;
            return (config as IPropertyPrintingConfig<TOwner>).ParentConfig;
        }
    }
}