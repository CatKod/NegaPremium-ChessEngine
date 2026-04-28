using System;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace NegaPremium {

    /// <summary>
    /// Represents the terminal for engine input and output. 
    /// </summary>
    public static class Terminal {

        /// <summary>
        /// The width of the terminal window. 
        /// </summary>
        public const Int32 Width = 82;

        /// <summary>
        /// The height of the terminal window. 
        /// </summary>
        public const Int32 Height = 25;

        /// <summary>
        /// The row position of the terminal window. 
        /// </summary>
        public static Int32 CursorTop {
            get { return Console.CursorTop; }
            set { Console.SetCursorPosition(0, value); }
        }

        /// <summary>
        /// The text that has been processed. 
        /// </summary>
        private static StringBuilder _text = new StringBuilder();

        /// <summary>
        /// Initializes the terminal. 
        /// </summary>
        public static void Initialize() {
            try {
                Console.Title = "Engine Terminal";
                Console.SetWindowSize(Width, Height);
            } catch { }
        }

        /// <summary>
        /// Writes the given string, followed by the current line terminator, to the 
        /// standard output stream. 
        /// </summary>
        /// <param name="value">The value to write.</param>
        public static void WriteLine(String value = "") {
            _text.AppendLine(value);
            Console.WriteLine(value);
        }

        /// <summary>
        /// Writes the text representation of the given objects, followed by the 
        /// current line terminator, to the standard output stream using the given 
        /// formatting. 
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="value">The value to write.</param>
        public static void WriteLine(String format, params Object[] values) {
            WriteLine(String.Format(format, values));
        }

        /// <summary>
        /// Overwrites the text representation of the given objects, followed by the 
        /// current line terminator, to the standard output stream using the given 
        /// formatting, at the given row position. Moves back to previous position
        /// after the write.
        /// </summary>
        /// <param name="top">The row position to write at.</param>
        /// <param name="format">The format string.</param>
        /// <param name="value">The value to write.</param>
        public static void OverwriteLineAt(Int32 top, String format, params Object[] values) {
            Int32 oldTop = CursorTop;
            CursorTop = top;
            String line = String.Format(format, values);
            _text.AppendLine(line);
            Console.WriteLine(line.PadRight(Console.WindowWidth));
            CursorTop = oldTop;
        }

        /// <summary>
        /// Clears the output in the terminal.
        /// </summary>
        public static void Clear() {
            Console.Clear();
        }

        /// <summary>
        /// Writes all the text that has been written to the standard output stream 
        /// to a file with the specified path. 
        /// </summary>
        /// <param name="path">The path of the file to write to.</param>
        public static void SaveText(String path) {
            File.WriteAllText(path, _text.ToString());
        }

        /// <summary>
        /// Hides the terminal window. 
        /// </summary>
        public static void Hide() {
            Native.ShowWindow(Native.GetConsoleWindow(), Native.SW_HIDE);
        }
    }
}
