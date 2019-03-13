using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace NTracktive
{
	public class EditModelReader
	{
		static string ToPascalCase (string camelCase)
		{
			return string.IsNullOrEmpty (camelCase) ? camelCase : char.ToUpper (camelCase [0]) + camelCase.Substring (1);
		}

		static IEnumerable<byte> ToHexBinary (string value)
		{
			for (int i = 0; i < value.Length; i += 2)
				yield return byte.Parse (value.Substring (i, 2), System.Globalization.NumberStyles.HexNumber);
		}

		object GetTypedValue (PropertyInfo pi, string value, IXmlLineInfo li)
		{
			var dta = pi.GetCustomAttribute<DataTypeAttribute> ();
			DataType type = DataType.Unknown;
			if (dta != null)
				type = dta.DataType;

			if (pi.PropertyType == typeof (byte [])) {
				// Actually JUCE base64 serialization is weird, it fails to deserialize their base64 string.
				if (type == DataType.Base64Binary)
					return Convert.FromBase64String (value);
				else if (type == DataType.HexBinary)
					return ToHexBinary (value).ToArray ();
				else
					throw new XmlException ("Missing DataType attribute on byte array.", null, li.LineNumber, li.LinePosition);
			}

			var nonNullableType = pi.PropertyType;
			if (pi.PropertyType.IsGenericType &&
			    pi.PropertyType.GetGenericTypeDefinition () == typeof (Nullable<>)) {
				if (string.IsNullOrEmpty (value))
					return null;
				nonNullableType = pi.PropertyType.GenericTypeArguments [0];
			}

			switch (Type.GetTypeCode (nonNullableType)) {
			case TypeCode.String:
				return value;
			case TypeCode.Boolean:
				switch (value) {
				case "1": return true;
				case "0": return false;
				}
				throw new XmlException ("Invalid value for boolean", null, li.LineNumber, li.LinePosition);
			case TypeCode.Double:
				double d;
				if (double.TryParse (value, out d))
					return d;
				throw new XmlException ("Invalid value for number", null, li.LineNumber, li.LinePosition);
			case TypeCode.Int32:
				int i;
				if (int.TryParse (value, out i))
					return i;
				throw new XmlException ("Invalid value for int", null, li.LineNumber, li.LinePosition);
			case TypeCode.Int64:
				long l;
				if (long.TryParse (value, NumberStyles.HexNumber, null, out l))
					return l;
				throw new XmlException ("Invalid value for long", null, li.LineNumber, li.LinePosition);
			}

			throw new XmlException ($"Unexpected data for {pi}", null, li.LineNumber, li.LinePosition);
		}

		public EditElement Read (XmlReader reader)
		{
			return (EditElement) DoRead (reader);
		}

		public object DoRead (XmlReader reader)
		{
			var li = reader as IXmlLineInfo;
			reader.MoveToContent ();
			switch (reader.NodeType) {
			case XmlNodeType.Element:
				var typeName = reader.LocalName + "Element";
				var type = GetType ().Assembly.GetTypes ().FirstOrDefault (t => string.Equals (t.Name, typeName, StringComparison.OrdinalIgnoreCase));
				if (type == null)
					throw new XmlException ($"Type {typeName} does not exist", null, li.LineNumber, li.LinePosition);
				var obj = Activator.CreateInstance (type);
				if (reader.MoveToFirstAttribute ()) {
					do {
						// JUCE XML is awkward and is invalid if XML namespace is enabled, because it lacks namespace declaration for "base64" prefix.
						// To workaround that problem, we use XmlTextReader that can disable namespace handling (XmlTextReader.Namespaces = false).
						// Therefore, "base64:layout" attribute is parsed as "prefix = '', localname='base64:layout'".
						var propName = string.Join ("_", reader.LocalName.Split (':').Select (s => ToPascalCase (s)));
						//var propName = (string.IsNullOrEmpty (reader.Prefix) ? "" : ToPascalCase (reader.Prefix) + '_') + ToPascalCase (reader.LocalName);
						var prop = type.GetProperty (propName);
						if (prop == null)
							throw new XmlException ($"In {type}, property {propName} not found.", null, li.LineNumber, li.LinePosition);
						prop.SetValue (obj, GetTypedValue (prop, reader.Value, li));
					} while (reader.MoveToNextAttribute ());
				}
				reader.MoveToElement ();

				if (reader.IsEmptyElement) {
					reader.Read ();
					return obj;
				}
				reader.Read ();
				reader.MoveToContent ();
				while (reader.NodeType != XmlNodeType.EndElement) {
					var propTypeName = reader.LocalName + "Element";
					var prop = type.GetProperties ().FirstOrDefault (p => string.Equals (p.PropertyType.Name, propTypeName, StringComparison.OrdinalIgnoreCase));
					if (prop != null)
						prop.SetValue (obj, DoRead (reader));
					else {
						var itemObj = DoRead (reader);
						prop = type.GetProperties ().Where (p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition () == typeof (IList<>))
							.Select (p => new { Property = p, ItemType = p.PropertyType.GetGenericArguments () [0] })
							.FirstOrDefault (m => m.ItemType.IsAssignableFrom(itemObj.GetType ())/*string.Equals (m.ItemType.Name, propTypeName, StringComparison.OrdinalIgnoreCase)*/)?.Property;
						if (prop == null)
							throw new XmlException ($"In {type}, property of collection of type {propTypeName} not found.", null, li.LineNumber, li.LinePosition);
						var propValue = prop.GetValue (obj);
						if (propValue == null)
							throw new XmlException ($"In {type}, property {prop} has null value unexpectedly.", null, li.LineNumber, li.LinePosition);
						// IList<T> doesn't contain Add method, this is in the same dirty manner as the serializers.
						var add = propValue.GetType ().GetMethods (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault (m => (m.Name == "Add" || m.Name.EndsWith (".Add")));
						if (add == null)
							throw new XmlException ($"In {type}, property {prop} has no Add method.", null, li.LineNumber, li.LinePosition);
						add.Invoke (propValue, new object [] { itemObj });
					}
					reader.MoveToContent ();
				}
				reader.ReadEndElement ();
				return obj;
			}
			throw new XmlException ($"Unexpected XML content {reader.NodeType} {reader.Name}.", null, li.LineNumber, li.LinePosition);
		}
	}
}
