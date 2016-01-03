namespace Monada {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;

    public static class AppInstanceIOExtensions {
        public static void DoMain(this AppInstance inst, Func<IO<None>> body) {
            None result;
            body().Execute(new RealWorld(inst, 0), out result);
        }
    }
}
