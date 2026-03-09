using System;
using System.Diagnostics.CodeAnalysis;
using DotNetWorkQueue.Transport.SQLite.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SQLite.Tests.Basic
{
    [TestClass]
    public class CreationScopeTests
    {
        [TestMethod]
        public void Create_CreationScope()
        {
            var disposable = new CanBeDisposed();
            using (var test = new CreationScope())
            {
                test.AddScopedObject(disposable);
            }
            Assert.IsTrue(disposable.WasDisposed);
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "part of test")]
        [TestMethod]
        public void Create_DisposeTwiceIsOk()
        {
            var disposable = new CanBeDisposed();
            using (var test = new CreationScope())
            {
                test.AddScopedObject(disposable);
                test.Dispose();
            }
            Assert.IsTrue(disposable.WasDisposed);
        }
    }
    internal class CanBeDisposed : IDisposable
    {
        public bool WasDisposed { get; private set; }
        public void Dispose()
        {
            WasDisposed = true;
        }
    }
}
