using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aac4
{
    internal class Utility
    {
        public static void Seek(string s, ref StringBuilder sb, char start, char end, ref int i)
        {
            sb.Append(start);
            while (s[++i] != end)
                sb.Append(s[i]);
            sb.Append(end);
        }


        static public void PrintTableOrView(DataTable table, string label)
        {
            Console.WriteLine("\n" + label);
            foreach (DataColumn coloumn in table.Columns)
            {
                Console.Write(coloumn.ColumnName);
                Console.Write("\t");
            }
            Console.WriteLine();
            foreach (DataRow row in table.Rows)
            {
                foreach (var item in row.ItemArray)
                {
                    Console.Write(item);
                    Console.Write("\t");
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }
    }
}
