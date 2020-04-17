#region Using Statements
using System;
using System.Collections.Generic;
using System.Linq;
#endregion

namespace Program
{
    /// <summary>
    /// The main class.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
#pragma warning disable IDE0063 // Use simple 'using' statement
            using (var game = new Harkkatyö())
#pragma warning restore IDE0063 // Use simple 'using' statement
                game.Run();
        }
    }
}
