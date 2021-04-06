using System;
using System.Diagnostics.CodeAnalysis;
using DotNetWorkQueue.Transport.SQLite.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.SQLite.Tests.Basic
{
    public class CreationScopeTests
    {
        [Fact]
        public void Create_CreationScope()
        {
            var disposable = new CanBeDisposed();
            using (var test = new CreationScope())
            {
                test.AddScopedObject(disposable);
            }
            Assert.True(disposable.WasDisposed);
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "part of test")]
        [Fact]
        public void Create_DisposeTwiceIsOk()
        {
            var disposable = new CanBeDisposed();
            using (var test = new CreationScope())
            {
                test.AddScopedObject(disposable);
                test.Dispose();
            }
            Assert.True(disposable.WasDisposed);
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
