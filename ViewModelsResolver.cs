using Microsoft.Practices.Unity;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Markup;

namespace PP.Blog.WpfViewModelsResolver
{
    class ViewModelsResolver
    {
        public void ConfigureViewModels(IUnityContainer container)
        {
            Assembly currentAssembly = Assembly.GetCallingAssembly();
            IEnumerable<Type> allAssemblyTypes = currentAssembly.GetTypes();

            foreach (var type in allAssemblyTypes)
            {
                if (this.ViewModelNamesOnly(type) != null)
                {
                    Type viewType = this.GetMatchingViewType(type);
                    if (viewType != null)
                    {
                        this.AddDataTemplate(viewType, type);
                    }

                    Type interfaceType = this.GetMatchingInterfaceType(type);
                    if (interfaceType != null)
                    {
                        container.RegisterType(interfaceType, type);
                    }
                }
            }
        }

        private Type GetMatchingInterfaceType(Type viewModelType)
        {
            var typeName = viewModelType.FullName;
            var namespacePath = typeName.Substring(0, typeName.Length - viewModelType.Name.Length); // Get namespace path
            var interfaceTypeName = namespacePath + "I" + viewModelType.Name;
            return viewModelType.Assembly.GetType(interfaceTypeName, false);
        }

        private Type GetMatchingViewType(Type viewModelType)
        {
            var typeName = viewModelType.FullName;
            var viewTypeName = typeName.Substring(0, typeName.Length - "Model".Length); // Get matching
            return viewModelType.Assembly.GetType(viewTypeName, false);
        }

        private string ViewModelNamesOnly(Type type)
        {
            string typeName = type.FullName;
            if (typeName.EndsWith("ViewModel") && !type.IsInterface)
            {
                return type.Name;
            }

            return null;
        }

        private void AddDataTemplate(Type viewType, Type viewModelType)
        {
            DataTemplate dataTemplate = this.CreateTemplate(viewType, viewModelType);

            if (!Application.Current.Resources.Contains(dataTemplate.DataTemplateKey))
            {
                Application.Current.Resources.Add(dataTemplate.DataTemplateKey, dataTemplate);
            }
            else
            {
                Console.WriteLine("Duplicated resource key: " + dataTemplate.DataTemplateKey.ToString());
            }
        }

        private DataTemplate CreateTemplate(Type viewType, Type viewModelType)
        {
            const string xamlTemplate =
               "<DataTemplate DataType=\"{{x:Type xViewModels:{0}}}\"><xViews:{1}></xViews:{1}></DataTemplate>";
            var xaml = String.Format(xamlTemplate, viewModelType.Name, viewType.Name);

            var context = new ParserContext();
            context.XamlTypeMapper = new XamlTypeMapper(new string[0]);
            context.XamlTypeMapper.AddMappingProcessingInstruction("xViewModels", viewModelType.Namespace, viewModelType.Assembly.FullName);
            context.XamlTypeMapper.AddMappingProcessingInstruction("xViews", viewType.Namespace, viewType.Assembly.FullName);
            context.XmlnsDictionary.Add("", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");
            context.XmlnsDictionary.Add("x", "http://schemas.microsoft.com/winfx/2006/xaml");
            context.XmlnsDictionary.Add("xViewModels", "xViewModels");
            context.XmlnsDictionary.Add("xViews", "xViews");

            return (DataTemplate)XamlReader.Parse(xaml, context);
        }
    }
}
