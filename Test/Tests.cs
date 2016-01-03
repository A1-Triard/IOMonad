using Monada;
using Monada.Internal;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MonadaTest {
    [TestFixture]
    public class Tests {
        TestConsole console;
        AppInstance testInst;

        protected void Setup(string input) {
            console = new TestConsole(input);
            testInst = AppInstance.Get().ForTests((method, argTypes, args) => {
                var call = new object[] { console, console.In, console.Out }.Select(x => new { t = x, m = x.GetType().GetMethod(method, argTypes) }).Where(x => x.m != null).First();
                return call.m.Invoke(call.t, args);
            });
        }

        [Test]
        public void WriteChars() {
            Setup("");
            testInst.AssertIO(0, () => Console.Write('A'));
            testInst.AssertIO(1, () => Console.Write('B'));
            Assert.AreEqual("AB", console.Output);
        }
        [Test]
        public void WriteCharsInBackOrder() {
            Setup("");
            Assert.Throws<ArgumentOutOfRangeException>(() => testInst.AssertIO(1, () => Console.Write('B')));
            Assert.Throws<ArgumentOutOfRangeException>(() => testInst.AssertIO(0, () => Console.Write('A')));
            Assert.AreEqual("", console.Output);
        }
        [Test]
        public void WriteCharTwice() {
            Setup("");
            testInst.AssertIO(0, () => Console.Write('A'));
            testInst.AssertIO(0, () => Console.Write('A'));
            Assert.Throws<ArgumentException>(() => testInst.AssertIO(0, () => Console.Write('B')));
            Assert.AreEqual("A", console.Output);
        }
        [Test]
        public void GetWriteError() {
            Setup("");
            console.Out.Close();
            Assert.Throws<AggregateException>(() => testInst.AssertIO(0, () => Console.Write('A')));
            Assert.Throws<ArgumentException>(() => testInst.AssertIO(0, () => Console.Write('B')));
        }
        [Test]
        public void ReadChar() {
            Setup("123");
            Assert.AreEqual((int)'1', testInst.AssertIO(0, () => Console.Read()));
            Assert.AreEqual((int)'2', testInst.AssertIO(1, () => Console.Read()));
            Assert.AreEqual((int)'3', testInst.AssertIO(2, () => Console.Read()));
            Assert.AreEqual(-1, testInst.AssertIO(3, () => Console.Read()));
        }
        [Test]
        public void IOTest() {
            Setup("123\r\n234\r\n");
            testInst.DoMain(EchoStrings);
            Assert.AreEqual("123\r\n234\r\n", console.Output);
        }
        static IO<None> EchoStrings() {
            return
                from s in EchoString()
                from none in s ? EchoStrings() : IO.Return(None._)
                select none;
        }
        static IO<bool> EchoString() {
            return
                from line in IO.Do(() => Console.ReadLine())
                let lineIsEmpty = string.IsNullOrEmpty(line)
                from none in lineIsEmpty ? IO.Return(None._) : IO.Do(() => Console.WriteLine(line))
                select !lineIsEmpty;
        }
    }
    public class TestConsole {
        readonly MemoryStream output;
        StreamWriter writer;
        readonly MemoryStream input;
        StreamReader reader;

        public TestConsole(string input) {
            this.input = new MemoryStream(Encoding.UTF8.GetBytes(input));
            this.reader = new StreamReader(this.input);
            this.output = new MemoryStream();
            this.writer = new StreamWriter(this.output);
        }

        public TextWriter Out { get { return writer; } }
        public TextReader In { get { return reader; } }
        public string Output {
            get {
                if(writer != null) {
                    writer.Close();
                    writer = null;
                }
                return Encoding.UTF8.GetString(output.ToArray());
            }
        }
    }
}
