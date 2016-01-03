namespace Monada {
    using Monada.Internal;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;

    public sealed class None {
        public static None _ { get { return null; } }
        None() { }
    }
}
