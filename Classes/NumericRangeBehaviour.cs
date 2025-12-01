using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace wpfTDX
{



        public static class NumericRangeBehavior
        {
            public static readonly DependencyProperty EnableProperty =
                DependencyProperty.RegisterAttached("Enable", typeof(bool), typeof(NumericRangeBehavior),
                    new PropertyMetadata(false, OnEnableChanged));

            public static void SetEnable(DependencyObject obj, bool value) { obj.SetValue(EnableProperty, value); }
            public static bool GetEnable(DependencyObject obj) { return (bool)obj.GetValue(EnableProperty); }

            public static readonly DependencyProperty MinProperty =
                DependencyProperty.RegisterAttached("Min", typeof(double), typeof(NumericRangeBehavior), new PropertyMetadata(double.NegativeInfinity));
            public static void SetMin(DependencyObject obj, double value) { obj.SetValue(MinProperty, value); }
            public static double GetMin(DependencyObject obj) { return (double)obj.GetValue(MinProperty); }

            public static readonly DependencyProperty MaxProperty =
                DependencyProperty.RegisterAttached("Max", typeof(double), typeof(NumericRangeBehavior), new PropertyMetadata(double.PositiveInfinity));
            public static void SetMax(DependencyObject obj, double value) { obj.SetValue(MaxProperty, value); }
            public static double GetMax(DependencyObject obj) { return (double)obj.GetValue(MaxProperty); }

            private static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            {
                var tb = d as TextBox;
                if (tb == null) return;

                if ((bool)e.NewValue)
                {
                    tb.PreviewTextInput += OnPreviewTextInput;
                    tb.PreviewKeyDown += OnPreviewKeyDown; // for space, etc.
                    DataObject.AddPastingHandler(tb, OnPaste);
                }
                else
                {
                    tb.PreviewTextInput -= OnPreviewTextInput;
                    tb.PreviewKeyDown -= OnPreviewKeyDown;
                    DataObject.RemovePastingHandler(tb, OnPaste);
                }
            }

            private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
            {
                // allow control/navigation keys; block Space which would create invalid text
                if (e.Key == Key.Space) e.Handled = true;
            }

        //private static void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        //{
        //    var tb = sender as TextBox; if (tb == null) return;
        //    if (!IsProposedTextValid(tb, e.Text)) e.Handled = true;
        //}
        private static void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb == null) return;

            e.Handled = !IsProposedTextValid(tb, e.Text);
        }
        private static void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (!(sender is TextBox tb)) return;

            if (e.DataObject.GetDataPresent(DataFormats.Text))
            {
                string pasteText = e.DataObject.GetData(DataFormats.Text) as string ?? string.Empty;
                if (!IsProposedTextValid(tb, pasteText))
                    e.CancelCommand();
            }
            else
            {
                e.CancelCommand();
            }
        }

        //private static void OnPaste(object sender, DataObjectPastingEventArgs e)
        //    {
        //        var tb = sender as TextBox; if (tb == null) return;
        //        if (!e.SourceDataObject.GetDataPresent(DataFormats.Text)) { e.CancelCommand(); return; }
        //        var paste = e.SourceDataObject.GetData(DataFormats.Text) as string ?? "";
        //        if (!IsProposedTextValid(tb, paste)) e.CancelCommand();
        //    }
        private static bool IsProposedTextValid(TextBox tb, string incoming)
        {
            // Build the text as it would look *after* this keystroke
            var text = tb.Text ?? string.Empty;
            var selStart = tb.SelectionStart;
            var selLength = tb.SelectionLength;
            var proposed = (selLength > 0)
                ? text.Remove(selStart, selLength).Insert(selStart, incoming)
                : text.Insert(selStart, incoming);

            // Allow empty (user clearing the box)
            if (string.IsNullOrWhiteSpace(proposed))
                return true;

            // Only one decimal point allowed
            int dotCount = 0;
            foreach (char c in proposed)
                if (c == '.') dotCount++;
            if (dotCount > 1)
                return false;

            // Optional: allow leading sign if your range allows negatives
            // bool allowNegative = GetMin(tb) < 0;   // if you have attached Min
            // if (!allowNegative && proposed.Contains("-")) return false;

            // Normalize to parse:
            // - Accept ".25" -> "0.25"
            // - Accept "1."  -> "1.0" for parse-check only (user still sees "1.")
            string toParse = proposed;

            if (toParse.StartsWith(".", StringComparison.Ordinal))
                toParse = "0" + toParse;

            if (toParse.EndsWith(".", StringComparison.Ordinal))
                toParse += "0";

            // If you allow a trailing percent in the editor, strip it for parse-check:
            if (toParse.EndsWith("%", StringComparison.Ordinal))
                toParse = toParse.Substring(0, toParse.Length - 1).Trim();

            // Try parse (Invariant recommended for consistency)
            if (!double.TryParse(toParse,
                                 System.Globalization.NumberStyles.Float,
                                 System.Globalization.CultureInfo.InvariantCulture,
                                 out double value))
                return false;

            // Range check if you use attached Min/Max
            // (If you don’t have these, remove the next block.)
            double min = GetMin(tb);  // your attached property
            double max = GetMax(tb);  // your attached property
            if (value < min || value > max)
                return false;

            return true;
        }

            ////private static bool IsProposedTextValid(TextBox tb, string incoming)
            //{
            //    // Build the text as it would look after this input
            //    var text = tb.Text ?? string.Empty;
            //    int selStart = tb.SelectionStart;
            //    int selLength = tb.SelectionLength;

            //    string proposed = selLength > 0
            //        ? text.Remove(selStart, selLength).Insert(selStart, incoming)
            //        : text.Insert(selStart, incoming);

            //    // Allow empty -> treated as null
            //    if (string.IsNullOrWhiteSpace(proposed))
            //        return true;

            //    // Parse like your NullablePercentDoubleConverter: accept "75", "75%", "0.75"
            //    var culture = CultureInfo.CurrentCulture;
            //    string percentSymbol = culture.NumberFormat.PercentSymbol;

            //    string s = proposed.Trim();
            //    bool hadPercent = s.EndsWith(percentSymbol, StringComparison.Ordinal);
            //    if (hadPercent) s = s.Substring(0, s.Length - percentSymbol.Length).Trim();

            //    double val;
            //    if (!double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, culture, out val))
            //        return false;

            //    if (hadPercent || val >= 1.0) val /= 100.0; // interpret 75/75% as 0.75

            //    double min = GetMin(tb);
            //    double max = GetMax(tb);
            //    return val >= min && val <= max;
            //}
        }
    }


