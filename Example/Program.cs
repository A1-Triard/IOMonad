using Monada;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonadaExample {
    class Program {
        static void Main(string[] args) {
            AppInstance.Get().DoMain(IOMain);
        }
        static IO<None> IOMain() {
            return
                from _ in IO.Do(() => Console.WriteLine("What is your name?"))
                from name in IO.Do(() => Console.ReadLine())
                let message = "Hi, " + name + "!"
                from r in IO.Do(() => Console.WriteLine(message))
                select r;
        }
    }
}
