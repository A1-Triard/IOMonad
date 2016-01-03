namespace Monada.Internal {
    using System;
    using System.Reflection;

    public static class AppInstanceTestExtensions {
        public static AppInstance ForTests(this AppInstance inst, Func<string, Type[], object[], object> consoleDescriptor) {
            return new AppInstance(consoleDescriptor);
        }
    }
}
