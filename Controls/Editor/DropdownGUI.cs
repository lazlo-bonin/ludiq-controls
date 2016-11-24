using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Ludiq.Controls.Editor
{
	/// <summary>
	/// Utility class to display complex editor popups.
	/// </summary>
	public static class DropdownGUI<T>
	{
		public delegate void SingleCallback(T value);
		public delegate void MultipleCallback(IEnumerable<T> value);

		public static int activeControlID = -1;
		public static T activeValue;
		public static IEnumerable<T> activeValues;
		public static bool activeValueChanged = false;

		public static T PopupSingle
		(
			Rect position,
			IEnumerable<DropdownOption<T>> options,
			T selectedValue,
			DropdownOption<T> noneOption,
			bool hasMultipleDifferentValues,
			GUIContent label = null,
			GUIStyle style = null
		)
		{
			return PopupSingle
			(
				position,
				options,
				options.FirstOrDefault(o => EqualityComparer<T>.Default.Equals(o.value, selectedValue)),
				noneOption,
				false,
				label,
				style
			);
		}

		public static T PopupSingle
		(
			Rect position,
			IEnumerable<DropdownOption<T>> options,
			DropdownOption<T> selectedOption,
			DropdownOption<T> noneOption,
			bool hasMultipleDifferentValues,
			bool allowOuterOption = true,
			GUIContent label = null,
			GUIStyle style = null
		)
		{
			if (label == null)
			{
				string text;

				if (hasMultipleDifferentValues)
				{
					text = "\u2014"; // Em Dash
				}
				else if (selectedOption == null)
				{
					if (noneOption != null)
					{
						text = noneOption.label;
					}
					else
					{
						text = string.Empty;
					}
				}
				else
				{
					text = selectedOption.label;
				}

				label = new GUIContent(text);
			}

			if (style == null)
			{
				style = EditorStyles.popup;
			}

			var buttonClicked = GUI.Button(position, label, style);
			var controlID = GetLastControlID();

			if (buttonClicked)
			{
				GUI.changed = false; // HACK: Cancel button click
				activeControlID = controlID;

				DropdownSingle
				(
					new Vector2(position.xMin, position.yMax),
					(value) =>
					{
						activeValue = value;
						activeValueChanged = true;
					},
					options,
					selectedOption,
					noneOption,
					hasMultipleDifferentValues
				);
			}

			if (controlID == activeControlID && activeValueChanged) // Selected option changed
			{
				// TODO: Use EditorWindow.SendEvent like EditorGUI.PopupCallbackInfo does.
				// Otherwise, there seems to be a 1-frame delay in update.
				GUI.changed = true;
				activeControlID = -1;
				activeValueChanged = false;
				return activeValue;
			}
			else if (selectedOption == null || (!allowOuterOption && !options.Any(o => EqualityComparer<T>.Default.Equals(o.value, selectedOption.value)))) // Selected option is null or outside of range
			{
				if (noneOption != null)
				{
					return noneOption.value;
				}
				else
				{
					return default(T);
				}
			}
			else
			{
				return selectedOption.value;
			}
		}

		public static void DropdownSingle
		(
			Vector2 position,
			SingleCallback callback,
			IEnumerable<DropdownOption<T>> options,
			DropdownOption<T> selectedOption,
			DropdownOption<T> noneOption,
			bool hasMultipleDifferentValues
		)
		{
			bool hasOptions = options != null && options.Any();

			GenericMenu menu = new GenericMenu();
			GenericMenu.MenuFunction2 menuCallback = (o) => { callback((T)o); };

			if (noneOption != null)
			{
				bool on = !hasMultipleDifferentValues && (selectedOption == null || EqualityComparer<T>.Default.Equals(selectedOption.value, noneOption.value));

				menu.AddItem(new GUIContent(noneOption.label), on, menuCallback, noneOption.value);
			}

			if (noneOption != null && hasOptions)
			{
				menu.AddSeparator("");
			}

			if (hasOptions)
			{
				foreach (var option in options)
				{
					bool on = !hasMultipleDifferentValues && (selectedOption != null && EqualityComparer<T>.Default.Equals(selectedOption.value, option.value));

					menu.AddItem(new GUIContent(option.label), on, menuCallback, option.value);
				}
			}

			menu.DropDown(new Rect(position, Vector2.zero));
		}

		private static IEnumerable<T> SanitizeMultipleOptions(IEnumerable<DropdownOption<T>> options, IEnumerable<T> selectedOptions)
		{
			// Remove outer options
			return selectedOptions.Where(so => options.Any(o => EqualityComparer<T>.Default.Equals(o.value, so)));
		}

		public static IEnumerable<T> PopupMultiple
		(
			Rect position,
			IEnumerable<DropdownOption<T>> options,
			IEnumerable<T> selectedOptions,
			bool hasMultipleDifferentValues,
			bool showNothingEverything = true,
			GUIContent label = null,
			GUIStyle style = null
		)
		{
			selectedOptions = SanitizeMultipleOptions(options, selectedOptions);

			if (label == null)
			{
				string text;

				if (hasMultipleDifferentValues)
				{
					text = "\u2014"; // Em Dash
				}
				else
				{
					var selectedOptionsCount = selectedOptions.Count();
					var optionsCount = options.Count();

					if (selectedOptionsCount == 0)
					{
						text = "Nothing";
					}
					else if (selectedOptionsCount == 1)
					{
						text = options.First(o => EqualityComparer<T>.Default.Equals(o.value, selectedOptions.First())).label;
					}
					else if (selectedOptionsCount == optionsCount)
					{
						text = "Everything";
					}
					else
					{
						text = "(Mixed)";
					}
				}

				label = new GUIContent(text);
			}

			if (style == null)
			{
				style = EditorStyles.popup;
			}

			var buttonClicked = GUI.Button(position, label, style);
			var controlID = GetLastControlID();

			if (buttonClicked)
			{
				GUI.changed = false; // HACK: Cancel button click
				activeControlID = controlID;

				DropdownMultiple
				(
					new Vector2(position.xMin, position.yMax),
					(values) =>
					{
						activeValues = values;
						activeValueChanged = true;
					},
					options,
					selectedOptions,
					hasMultipleDifferentValues,
					showNothingEverything
				);
			}

			if (controlID == activeControlID && activeValueChanged)
			{
				GUI.changed = true;
				activeControlID = -1;
				activeValueChanged = false;
				return activeValues;
			}
			else
			{
				return selectedOptions;
			}
		}

		public static void DropdownMultiple
		(
			Vector2 position,
			MultipleCallback callback,
			IEnumerable<DropdownOption<T>> options,
			IEnumerable<T> selectedOptions,
			bool hasMultipleDifferentValues,
			bool showNothingEverything = true
		)
		{
			selectedOptions = SanitizeMultipleOptions(options, selectedOptions);

			bool hasOptions = options != null && options.Any();

			GenericMenu menu = new GenericMenu();
			GenericMenu.MenuFunction2 switchCallback = (o) =>
			{
				var switchOption = (T)o;

				var newSelectedOptions = selectedOptions.ToList();

				if (newSelectedOptions.Contains(switchOption))
				{
					newSelectedOptions.Remove(switchOption);
				}
				else
				{
					newSelectedOptions.Add(switchOption);
				}

				callback(newSelectedOptions);
			};

			GenericMenu.MenuFunction nothingCallback = () =>
			{
				callback(Enumerable.Empty<T>());
			};

			GenericMenu.MenuFunction everythingCallback = () =>
			{
				callback(options.Select((o) => o.value).ToArray());
			};

			if (showNothingEverything)
			{
				menu.AddItem(new GUIContent("Nothing"), !hasMultipleDifferentValues && !selectedOptions.Any(), nothingCallback);
				menu.AddItem(new GUIContent("Everything"), !hasMultipleDifferentValues && selectedOptions.Count() == options.Count() && Enumerable.SequenceEqual(selectedOptions.OrderBy(t => t), options.Select(o => o.value).OrderBy(t => t)), everythingCallback);
			}

			if (showNothingEverything && hasOptions)
			{
				menu.AddSeparator(""); // Not in Unity default, but pretty
			}

			if (hasOptions)
			{
				foreach (var option in options)
				{
					bool on = !hasMultipleDifferentValues && (selectedOptions.Any(selectedOption => EqualityComparer<T>.Default.Equals(selectedOption, option.value)));

					menu.AddItem(new GUIContent(option.label), on, switchCallback, option.value);
				}
			}

			menu.DropDown(new Rect(position, Vector2.zero));
		}

		private static int GetLastControlID()
		{
			return (int)typeof(EditorGUIUtility).GetField("s_LastControlID", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
		}
	}
}
