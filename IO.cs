namespace Monada {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;

    public static class IO {
        public static IO<T> Return<T>(T value) {
            return new IO<T>(x => Tuple.Create(x, value));
        }
        public static IO<R> Select<T, R>(this IO<T> io, Func<T, R> selector) {
            return new IO<R>(x => {
                T t;
                var index = io.Execute(x, out t);
                return Tuple.Create(index, selector(t));
            });
        }
        public static IO<R> SelectMany<T, C, R>(this IO<T> io, Func<T, IO<C>> selector, Func<T, C, R> projector) {
            return new IO<R>(x => {
                T t;
                var index = io.Execute(x, out t);
                var ioc = selector(t);
                C c;
                var resultIndex = ioc.Execute(index, out c);
                return Tuple.Create(resultIndex, projector(t, c));
            });
        }
        public static IO<None> Do(Expression<Action> callExpression) {
            return new IO<None>(x => x.Do(callExpression));
        }
        public static IO<TResult> Do<TResult>(Expression<Func<TResult>> callExpression) {
            return new IO<TResult>(x => x.Do(callExpression));
        }
        public static IO<T> Handle<T>(this IO<T> io, Func<Exception, IO<T>> handler) {
            return new IO<T>(x => {
                RealWorld rw;
                T t;
                try {
                    rw = io.Execute(x, out t);
                } catch(Exception e) {
                    rw = handler(e).Execute(x.Yield(), out t);
                }
                return Tuple.Create(rw, t);
            });
        }

    }
    public sealed class IO<T> {
        readonly Func<RealWorld, Tuple<RealWorld, T>> func;

        internal IO(Func<RealWorld, Tuple<RealWorld, T>> func) {
            this.func = func;
        }
        internal RealWorld Execute(RealWorld index, out T result) {
            var resultTuple = func(index);
            result = resultTuple.Item2;
            return resultTuple.Item1;
        }
    }
    class RealWorld {
        readonly AppInstance inst;
        readonly int index;

        public RealWorld(AppInstance inst, int index) {
            this.inst = inst;
            this.index = index;
        }
        public Tuple<RealWorld, None> Do(Expression<Action> callExpression) {
            return Tuple.Create(Yield(), inst.AssertIO(index, callExpression));
        }
        public Tuple<RealWorld, TResult> Do<TResult>(Expression<Func<TResult>> callExpression) {
            return Tuple.Create(Yield(), inst.AssertIO(index, callExpression));
        }
        public RealWorld Yield() {
            return new RealWorld(inst, index + 1);
        }
    }
}
