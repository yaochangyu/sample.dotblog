using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            BatchWork work=new BatchWork();
            work.Start();

            Console.WriteLine("按任意鍵繼續，按ESC離開");
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Escape)
            {
                Console.WriteLine("再會~!");

                Console.ReadLine();
                return;
            }
        }
    }
}
