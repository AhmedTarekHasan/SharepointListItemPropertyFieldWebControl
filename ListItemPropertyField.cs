using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Reflection;
using System.Collections;
using System.Web.UI;
using System.ComponentModel;
using Microsoft.SharePoint.WebControls;

[assembly: TagPrefix("DevelopmentSimplyPut.Sharepoint.WebControls", "DevelopmentSimplyPutControls")]
namespace DevelopmentSimplyPut.Sharepoint.WebControls
{
    [ParseChildren(ChildrenAsProperties = true)]
    [PersistChildren(false)]
    [ToolboxData("<{0}:ListItemPropertyField runat=\"server\"></{0}:ListItemPropertyField>")]
    public class ListItemPropertyField : BaseFieldControl, INamingContainer
    {
        public string Property
        {
            get;
            set;
        }

        public string RenderTemplate
        {
            get;
            set;
        }

        public string ItemIndex
        {
            get;
            set;
        }

        [PersistenceMode(PersistenceMode.InnerProperty)]
        public TypeConverter PropertyTypeConverter
        {
            get;
            set;
        }

        public override void UpdateFieldValueInItem()
        {

        }

        protected override void Render(System.Web.UI.HtmlTextWriter output)
        {
            try
            {
                object fieldValue = this.ListItem[this.FieldName];
                fieldValue = GetItemFromCollectionByIndex(fieldValue, ItemIndex);

                string subPropertyValue = string.Empty;
                object subPropertyObject = null;
                if (!string.IsNullOrEmpty(Property))
                {
                    subPropertyObject = GetSubPropertyValue(fieldValue, Property);
                }
                else
                {
                    subPropertyObject = fieldValue;
                }

                if (null != PropertyTypeConverter)
                {
                    subPropertyObject = ApplyTypeConverter(subPropertyObject, PropertyTypeConverter);
                }

                if (!string.IsNullOrEmpty(RenderTemplate))
                {
                    subPropertyValue = string.Format(CultureInfo.InvariantCulture, RenderTemplate, subPropertyObject);
                }
                else
                {
                    subPropertyValue = subPropertyObject.ToString();
                }
                output.Write(subPropertyValue);
            }
            catch (NullReferenceException ex)
            {
                //Do something
            }
            catch (ArgumentOutOfRangeException ex)
            {
                //Do something
            }
            catch (ArgumentNullException ex)
            {
                //Do something
            }
            catch (ArgumentException ex)
            {
                //Do something
            }
        }

        private static object GetSubProperty(object item, string property, string index)
        {
            Type type = item.GetType();
            PropertyInfo propertyInfo = type.GetProperty(property);
            object value = propertyInfo.GetValue(item, null);

            return GetItemFromCollectionByIndex(value, index);
        }

        private static object GetSubPropertyValue(object item, string property)
        {
            object parentItem = item;
            if (!string.IsNullOrEmpty(property))
            {
                string[] parts = property.Split('.');
                for (int i = 0; i < parts.Length; i++)
                {
                    string[] subParts = parts[i].Split('[');
                    string propertyName = parts[i];
                    string index = "0";

                    if (subParts.Length > 1)
                    {
                        propertyName = subParts[0];
                        index = subParts[1].Replace("]", "");
                    }
                    else
                    {
                        propertyName = parts[i];
                    }

                    parentItem = GetSubProperty(parentItem, propertyName, index);
                }
            }

            return parentItem;
        }

        private static object GetItemFromCollectionByIndex(object searchCollection, string index)
        {
            ICollection collection = searchCollection as ICollection;
            object result = searchCollection;
            if (collection != null)
            {
                int requestedIndex = 0;
                int itemsCount = collection.Count;

                if (!string.IsNullOrEmpty(index))
                {
                    if (!int.TryParse(index, out requestedIndex))
                    {
                        requestedIndex = 0;
                    }
                }

                IEnumerator enumObject = collection.GetEnumerator();
                requestedIndex = Math.Max(0, requestedIndex);
                requestedIndex = Math.Min(itemsCount, requestedIndex);

                for (int x = 0; x < itemsCount; x++)
                {
                    enumObject.MoveNext();

                    if (x == requestedIndex)
                    {
                        result = enumObject.Current;
                        break;
                    }
                }
            }

            return result;
        }

        private static object ApplyTypeConverter(object property, TypeConverter converter)
        {
            object result = property;

            if (null != property && null != converter)
            {
                if (!string.IsNullOrEmpty(converter.AssemblyFullyQualifiedName) && !string.IsNullOrEmpty(converter.ClassFullyQualifiedName) && !string.IsNullOrEmpty(converter.MethodName))
                {
                    AssemblyName assemblyName = new AssemblyName(converter.AssemblyFullyQualifiedName);
                    Assembly assembly = Assembly.Load(assemblyName);
                    Type classType = assembly.GetType(converter.ClassFullyQualifiedName);
                    object[] parametersArray = new object[] { property };
                    result = classType.InvokeMember(converter.MethodName, System.Reflection.BindingFlags.InvokeMethod, System.Type.DefaultBinder, "", parametersArray);
                }
            }

            return result;
        }
    }

    public class TypeConverter
    {
        public string AssemblyFullyQualifiedName
        {
            get;
            set;
        }
        public string ClassFullyQualifiedName
        {
            get;
            set;
        }
        public string MethodName
        {
            get;
            set;
        }
    }
}
