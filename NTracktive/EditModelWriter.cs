using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;

#nullable enable

namespace NTracktive
{
	public class EditModelWriter
	{
		static string ToCamelCase (string pascalCase)
		{
			return string.IsNullOrEmpty (pascalCase) ? pascalCase : char.ToLower (pascalCase [0]) + pascalCase.Substring (1);
		}

		static string ToHexBinaryString (byte [] value)
		{
			return string.Concat (value.Select (v => v.ToString ("x02")));
		}

		static string? ToValueString (PropertyInfo pi, object obj)
		{
			if (obj == null)
				return null;
			if (obj is byte [])
				return ToHexBinaryString ((byte []) obj);

			switch (Type.GetTypeCode (pi.PropertyType)) {
			case TypeCode.Boolean:
				return (bool) obj ? "1" : "0";
			default:
				return obj.ToString ();
			}
		}

		bool IsIList (Type type) => type.FullName.StartsWith ("System.Collections.Generic.IList", StringComparison.OrdinalIgnoreCase);

		bool IsListPropertyType (Type type)
		{
			// only care about non-primitives. arrays and strings should not return true here.
			switch (Type.GetTypeCode (type)) {
			case TypeCode.Object:
				break;
			default:
				return false;
			}
			if (type == typeof (byte []))
				return false;

			for (var t = type; t != null; t = t.BaseType) {
				// if you don't hack around it, it's going to be super messy...
				if (IsIList (t) || t.GetInterfaces ().Any (IsIList))
					return true;
			}
			return false;
		}

		public void Write (TextWriter textWriter, object o)
		{
			using (var writer = new XmlTextWriter (textWriter) { Namespaces = false, Formatting = Formatting.Indented })
				Write (writer, o, null);
		}

		public void Write (XmlTextWriter writer, object o, PropertyInfo? hintProperty)
		{
			if (o == null)
				return;

			var typeName = o.GetType ().Name;
			if (typeName.EndsWith ("Element", StringComparison.Ordinal)) {
				var elementName = typeName.Substring (0, typeName.Length - "Element".Length).ToUpper ();
				// write as element
				writer.WriteStartElement (elementName);
				//attributes
				var listProps = o.GetType ().GetProperties ().Where (p => IsListPropertyType (p.PropertyType));
				var attProps = o.GetType ().GetProperties ().Except (listProps).Where (p => !p.PropertyType.Name.EndsWith ("Element", StringComparison.Ordinal));
				var nonListElementProps = o.GetType ().GetProperties ().Except (attProps).Except (listProps);
				foreach (var prop in attProps) {
					// see EditModelReader.cs for XML namespace problem...
					var attrName = ToCamelCase (prop.Name).Replace ('_', ':');
					var value = prop.GetValue (o);
					if (value != null)
						writer.WriteAttributeString (attrName, ToValueString (prop, value));
				}
				// elements
				foreach (var prop in nonListElementProps) {
					Write (writer, prop.GetValue (o), prop);
				}
				foreach (var prop in listProps) {
					var list = prop.GetValue (o) as IEnumerable;
					foreach (var item in list!)
						Write (writer, item, prop);
				}
				writer.WriteEndElement ();
			} else
				throw new ArgumentException ($"For property '{hintProperty}' in '{hintProperty?.DeclaringType}', unexpected object element appeared: {o.GetType ()}");
		}
	}
}
