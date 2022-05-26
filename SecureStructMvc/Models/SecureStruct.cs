using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace SecureStructMvc.Models
{
    public class SecureStructConverter : TypeConverter
    {
        public override bool GetCreateInstanceSupported(ITypeDescriptorContext context)
        {
            // Yes!
            return true;
        }

        public override object CreateInstance(ITypeDescriptorContext context, IDictionary propertyValues)
        {
            return base.CreateInstance(context, propertyValues);
        }

        public override bool GetPropertiesSupported(ITypeDescriptorContext context)
        {
            return true;
        }


        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
        {
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(value, attributes);

            string[] sortOrder = new string[2];

            sortOrder[0] = "ColName";
            sortOrder[1] = "ColType";

            return properties.Sort(sortOrder);
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, System.Type sourceType)
        {
            bool canConvert = CanConvertInternal(sourceType);

            if (!canConvert)
                canConvert = base.CanConvertFrom(context, sourceType);

            return canConvert;
        }
        private bool CanConvertInternal(Type type)
        {
            return
                type == typeof(string) ||
                type == typeof(int) ||
                type == typeof(DateTime);
        }

        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            if (value is string)
            {
                return new SecureStruct((string)value);
            }


            return base.ConvertFrom(context, culture, value);

        }

        public override bool CanConvertTo(ITypeDescriptorContext context, System.Type destinationType)
        {

            bool canConvert = CanConvertInternal(destinationType);

            if (!canConvert)
                canConvert = (destinationType == typeof(InstanceDescriptor));

            if (!canConvert)
                canConvert = base.CanConvertFrom(context, destinationType);

            return canConvert;
        }

        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, System.Type destinationType)
        {
            object retVal = null;
            SecureStruct colDefi = (SecureStruct)value;
            if (null == culture)
                culture = CultureInfo.CurrentCulture;

            // If this is an instance descriptor...
            if (destinationType == typeof(InstanceDescriptor))
            {
                System.Type[] argTypes = new System.Type[1];

                argTypes[0] = typeof(string);

                // Lookup the appropriate Doofer constructor
                ConstructorInfo constructor = typeof(SecureStruct).GetConstructor(argTypes);

                object[] arguments = new object[1];


                // And return an instance descriptor to the caller. Will fill in the CodeBehind stuff in VS.Net
                retVal = new InstanceDescriptor(constructor, arguments);
            }
            else if (destinationType == typeof(string))
            {
                TypeConverter numberConverter = TypeDescriptor.GetConverter(typeof(string));
                retVal = numberConverter.ConvertToString(context, culture, colDefi.ToString());
            }
            else
                retVal = base.ConvertTo(context, culture, value, destinationType);

            return retVal;
        }

    }

    public interface ISecureStruct
    {
        string ToPlainString();
    }



    [Serializable]
    [TypeConverter(typeof(SecureStructConverter))]
    public struct SecureStruct : ISecureStruct, ISerializable, IDisposable, IXmlSerializable
    {
        #region private members
        private SecureString _securedValue;
        private const int _unmaskedSize = 4;
        private StringBuilder _strBld;
        private static readonly List<char> _charsToIgnore = new List<char> { ' ', '\t', '\r', '\n' };
        private const char maskChar = 'x';
        private int _hashcode;

        #endregion private members

        #region ctors

        public SecureStruct(string value)
        {
            _securedValue = new SecureString();
            _strBld = new StringBuilder();
            _hashcode = value.GetHashCode();
            Init(value);
        }

        #endregion ctors

        #region logic methods
        private void Init(string value)
        {
            int length = value.Length;
            for (int i = 0; i < length; i++)
            {
                char c = value[i];
                //ignore whitespaces etc
                if (_charsToIgnore.Contains(c))
                {
                    continue;
                }
                // adding a char to securestring
                _securedValue.AppendChar(c);
                // mask for serialization
                // if last positions that should not be masked copy it
                if (i >= length - _unmaskedSize)
                {
                    _strBld.Append(c);
                }
                // mask character
                else
                {
                    _strBld.Append(maskChar);
                }
            }
        }
        public string ToPlainString()
        {
            SecureString value = _securedValue;
            if (value == null)
            {
                return null;
            }
            IntPtr valuePtr = IntPtr.Zero;

            valuePtr = Marshal.SecureStringToGlobalAllocUnicode(value);
            return Marshal.PtrToStringUni(valuePtr);
        }

        #endregion logic methods

        #region interface implementations
        public override string ToString()
        {
            if (_strBld == null)
            {
              return null;
            }
            //return masked value
            return _strBld.ToString();
        }

        public override bool Equals(object obj)
        {
            return ((SecureStruct)obj).GetHashCode() == this.GetHashCode();
        }
        public override int GetHashCode()
        {
            return _hashcode;
        }


        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Value", this.ToPlainString());
        }

        public XmlSchema GetSchema()
        {
            return new XmlSchema();
        }

        public void ReadXml(XmlReader reader)
        {
            reader.Read();
            string val = reader.Value;
            Init(val);
            reader.Read();
            reader.ReadEndElement();
        }



        public void WriteXml(XmlWriter writer)
        {
            writer.WriteString(this.ToPlainString());
        }

        public void Dispose()
        {
            _securedValue.Dispose(); ;
        }
        #endregion interface implementations
    }

}
