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

    public sealed class AppInstance {
        readonly static Lazy<AppInstance> inst = new Lazy<AppInstance>(() => new AppInstance((method, argTypes, args) => typeof(Console).GetMethod(method, BindingFlags.Static | BindingFlags.Public, null, argTypes, null).Invoke(null, args)));
        public static AppInstance Get() { return inst.Value; }

        readonly Func<string, Type[], object[], object> consoleDescriptor;

        internal AppInstance(Func<string, Type[], object[], object> consoleDescriptor) {
            this.consoleDescriptor = consoleDescriptor;
        }

        bool rejectOperations = false;
        readonly List<IOOperationWithResult> completedOperations = new List<IOOperationWithResult>();

        public None AssertIO(int index, Expression<Action> ioExpression) {
            return AssertIO(index, new IOOperation<None>(ioExpression));
        }
        public TResult AssertIO<TResult>(int index, Expression<Func<TResult>> ioExpression) {
            return AssertIO(index, new IOOperation<TResult>(ioExpression));
        }
        TResult AssertIO<TResult>(int index, IOOperation<TResult> operation) {
            if(index < 0)
                throw new ArgumentOutOfRangeException("index");
            if(index < completedOperations.Count) {
                var completedOperation = completedOperations[index] as IOOperationWithResult<TResult>;
                if(completedOperation == null || completedOperation.Operation != operation)
                    throw new ArgumentException("", "operation");
                return completedOperation.Result.Result;
            }
            if(rejectOperations)
                throw new ArgumentOutOfRangeException("index");
            if(index == completedOperations.Count) {
                var completedOperation = new IOOperationWithResult<TResult>(operation, new IOOperationResult<TResult>(() => operation.Do(consoleDescriptor)));
                completedOperations.Add(completedOperation);
                return completedOperation.Result.Result;
            }
            rejectOperations = true;
            throw new ArgumentOutOfRangeException("index");
        }

        abstract class IOOperationResult { }
        sealed class IOOperationResult<TResult> : IOOperationResult {
            readonly TResult returnValue;
            readonly Exception exception;

            public IOOperationResult(Func<TResult> getResult) {
                try {
                    returnValue = getResult();
                    exception = null;
                } catch(Exception e) {
                    returnValue = default(TResult);
                    exception = e;
                }
            }

            public TResult Result {
                get {
                    if(exception != null)
                        throw new AggregateException(exception);
                    return returnValue;
                }
            }
        }
        abstract class IOOperationWithResult { }
        sealed class IOOperationWithResult<TResult> : IOOperationWithResult {
            public IOOperationWithResult(IOOperation<TResult> operation, IOOperationResult<TResult> result) {
                Operation = operation;
                Result = result;
            }
            public readonly IOOperation<TResult> Operation;
            public readonly IOOperationResult<TResult> Result;
        }
        class IOOperation<TResult> {
            readonly string method;
            readonly Type[] argTypes;
            readonly object[] args;

            public IOOperation(LambdaExpression callExpression) {
                var methodExpr = (MethodCallExpression)callExpression.Body;
                this.args = methodExpr.Arguments.Select(x => Expression.Lambda<Func<object>>(Expression.Convert(x, typeof(object))).Compile()()).ToArray();
                this.method = methodExpr.Method.Name;
                this.argTypes = methodExpr.Method.GetParameters().Select(x => x.ParameterType).ToArray();
            }

            public TResult Do(Func<string, Type[], object[], object> consoleDescriptor) {
                return (TResult)consoleDescriptor(method, argTypes, args);
            }

            public static bool operator ==(IOOperation<TResult> a, IOOperation<TResult> b) {
                bool aIsNull = ReferenceEquals(a, null);
                bool bIsNull = ReferenceEquals(b, null);
                return
                    aIsNull && bIsNull ||
                    !aIsNull && !bIsNull &&
                    string.Equals(a.method, b.method, StringComparison.Ordinal) &&
                    a.args.Length == b.args.Length &&
                    !a.args.Zip(b.args, Equals).Where(x => !x).Any();
            }
            public override int GetHashCode() {
                return method.GetHashCode() ^ args.Length;
            }
            public static bool operator !=(IOOperation<TResult> a, IOOperation<TResult> b) {
                return !(a == b);
            }
            public override bool Equals(object obj) {
                return this == obj as IOOperation<TResult>;
            }
        }
    }
}
