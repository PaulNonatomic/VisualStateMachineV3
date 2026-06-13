using System;
using System.Text;

namespace Nonatomic.VSM.Utils
{
	/// <summary>
	/// Utility class for formatting strings, particularly for converting field names to display names.
	/// </summary>
	public static class StringFormatUtils
	{
		/// <summary>
		/// Formats a field name into a display name by removing the leading underscore
		/// and converting CamelCase to Title Case with spaces.
		/// </summary>
		/// <param name="fieldName">The field name to format.</param>
		/// <returns>The formatted display name.</returns>
		/// <example>
		/// "_playerHealth" -> "Player Health"
		/// "maxSpeed" -> "Max Speed"
		/// "XMLParser" -> "XML Parser"
		/// </example>
		public static string FormatFieldName(string fieldName)
		{
			if (string.IsNullOrEmpty(fieldName))
			{
				return fieldName;
			}

			// Remove leading underscore if present
			if (fieldName.StartsWith("_"))
			{
				fieldName = fieldName.Substring(1);
			}

			return ConvertCamelCaseToTitleCase(fieldName);
		}

		/// <summary>
		/// Formats a transition field name into a display name by removing the leading underscore,
		/// removing the "on" prefix, and converting CamelCase to Title Case with spaces.
		/// </summary>
		/// <param name="fieldName">The transition field name to format.</param>
		/// <returns>The formatted display name.</returns>
		/// <example>
		/// "_onComplete" -> "Complete"
		/// "onSuccess" -> "Success"
		/// "OnFailure" -> "Failure"
		/// </example>
		public static string FormatTransitionName(string fieldName)
		{
			if (string.IsNullOrEmpty(fieldName))
			{
				return fieldName;
			}

			// Remove leading underscore if present
			if (fieldName.StartsWith("_"))
			{
				fieldName = fieldName.Substring(1);
			}

			// Remove "on" prefix (case insensitive)
			if (fieldName.StartsWith("on", StringComparison.OrdinalIgnoreCase) && fieldName.Length > 2)
			{
				fieldName = fieldName.Substring(2);
			}

			return ConvertCamelCaseToTitleCase(fieldName);
		}

		/// <summary>
		/// Converts a CamelCase string to Title Case with spaces.
		/// </summary>
		/// <param name="input">The CamelCase string to convert.</param>
		/// <returns>The Title Case string with spaces.</returns>
		private static string ConvertCamelCaseToTitleCase(string input)
		{
			if (string.IsNullOrEmpty(input))
			{
				return input;
			}

			var result = new StringBuilder();

			// Capitalize first character
			if (input.Length > 0)
			{
				result.Append(char.ToUpper(input[0]));
			}

			// Add spaces before uppercase letters (except when preceded by another uppercase)
			for (var i = 1; i < input.Length; i++)
			{
				if (char.IsUpper(input[i]) && !char.IsUpper(input[i - 1]))
				{
					result.Append(' ');
				}
				result.Append(input[i]);
			}

			return result.ToString();
		}
	}
}
