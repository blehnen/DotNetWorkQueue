using System;
using System.Collections.Generic;
using DotNetWorkQueue.Dashboard.Api.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Dashboard.Api.Tests.Configuration
{
    [TestClass]
    public class InterceptorConfigurationBuilderTests
    {
        private static readonly Dictionary<string, Action<IContainer>> EmptyProfiles =
            new Dictionary<string, Action<IContainer>>(StringComparer.OrdinalIgnoreCase);

        [TestMethod]
        public void Returns_Null_When_Nothing_Configured()
        {
            var queueOptions = new DashboardQueueOptions { QueueName = "test" };
            var result = InterceptorConfigurationBuilder.Resolve(queueOptions, EmptyProfiles);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void Returns_Explicit_Delegate_When_Set()
        {
            Action<IContainer> expected = _ => { };
            var queueOptions = new DashboardQueueOptions
            {
                QueueName = "test",
                InterceptorConfiguration = expected
            };
            var result = InterceptorConfigurationBuilder.Resolve(queueOptions, EmptyProfiles);
            Assert.AreSame(expected, result);
        }

        [TestMethod]
        public void Explicit_Delegate_Takes_Priority_Over_Profile()
        {
            Action<IContainer> expected = _ => { };
            var profiles = new Dictionary<string, Action<IContainer>>(StringComparer.OrdinalIgnoreCase)
            {
                ["encrypted"] = _ => { }
            };
            var queueOptions = new DashboardQueueOptions
            {
                QueueName = "test",
                InterceptorConfiguration = expected,
                InterceptorProfile = "encrypted"
            };
            var result = InterceptorConfigurationBuilder.Resolve(queueOptions, profiles);
            Assert.AreSame(expected, result);
        }

        [TestMethod]
        public void Returns_Profile_When_Name_Matches()
        {
            Action<IContainer> profileAction = _ => { };
            var profiles = new Dictionary<string, Action<IContainer>>(StringComparer.OrdinalIgnoreCase)
            {
                ["encrypted"] = profileAction
            };
            var queueOptions = new DashboardQueueOptions
            {
                QueueName = "test",
                InterceptorProfile = "encrypted"
            };
            var result = InterceptorConfigurationBuilder.Resolve(queueOptions, profiles);
            Assert.AreSame(profileAction, result);
        }

        [TestMethod]
        public void Profile_Lookup_Is_Case_Insensitive()
        {
            Action<IContainer> profileAction = _ => { };
            var profiles = new Dictionary<string, Action<IContainer>>(StringComparer.OrdinalIgnoreCase)
            {
                ["Encrypted"] = profileAction
            };
            var queueOptions = new DashboardQueueOptions
            {
                QueueName = "test",
                InterceptorProfile = "ENCRYPTED"
            };
            var result = InterceptorConfigurationBuilder.Resolve(queueOptions, profiles);
            Assert.AreSame(profileAction, result);
        }

        [TestMethod]
        public void Throws_When_Profile_Not_Found()
        {
            var queueOptions = new DashboardQueueOptions
            {
                QueueName = "test",
                InterceptorProfile = "missing"
            };
            Action act = () => InterceptorConfigurationBuilder.Resolve(queueOptions, EmptyProfiles);
            var ex = Assert.Throws<InvalidOperationException>(act);
            Assert.IsTrue(ex.Message.Contains("missing", StringComparison.OrdinalIgnoreCase));
        }

        [TestMethod]
        public void Profile_Takes_Priority_Over_Json_Options()
        {
            Action<IContainer> profileAction = _ => { };
            var profiles = new Dictionary<string, Action<IContainer>>(StringComparer.OrdinalIgnoreCase)
            {
                ["encrypted"] = profileAction
            };
            var queueOptions = new DashboardQueueOptions
            {
                QueueName = "test",
                InterceptorProfile = "encrypted",
                Interceptors = new DashboardInterceptorOptions
                {
                    GZip = new GZipInterceptorOptions()
                }
            };
            var result = InterceptorConfigurationBuilder.Resolve(queueOptions, profiles);
            Assert.AreSame(profileAction, result);
        }

        [TestMethod]
        public void Returns_Null_When_Json_Options_All_Disabled()
        {
            var queueOptions = new DashboardQueueOptions
            {
                QueueName = "test",
                Interceptors = new DashboardInterceptorOptions
                {
                    GZip = new GZipInterceptorOptions { Enabled = false },
                    TripleDes = new TripleDesInterceptorOptions { Enabled = false }
                }
            };
            var result = InterceptorConfigurationBuilder.Resolve(queueOptions, EmptyProfiles);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void Returns_Action_When_GZip_Enabled()
        {
            var queueOptions = new DashboardQueueOptions
            {
                QueueName = "test",
                Interceptors = new DashboardInterceptorOptions
                {
                    GZip = new GZipInterceptorOptions { MinimumSize = 200 }
                }
            };
            var result = InterceptorConfigurationBuilder.Resolve(queueOptions, EmptyProfiles);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Returns_Action_When_TripleDes_Enabled()
        {
            var queueOptions = new DashboardQueueOptions
            {
                QueueName = "test",
                Interceptors = new DashboardInterceptorOptions
                {
                    TripleDes = new TripleDesInterceptorOptions
                    {
                        Key = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                        IV = "aaaaaaaaaaa="
                    }
                }
            };
            var result = InterceptorConfigurationBuilder.Resolve(queueOptions, EmptyProfiles);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Throws_When_TripleDes_Missing_Key()
        {
            var queueOptions = new DashboardQueueOptions
            {
                QueueName = "test",
                Interceptors = new DashboardInterceptorOptions
                {
                    TripleDes = new TripleDesInterceptorOptions { IV = "aaaaaaaaaaa=" }
                }
            };
            Action act = () => InterceptorConfigurationBuilder.Resolve(queueOptions, EmptyProfiles);
            var ex = Assert.Throws<InvalidOperationException>(act);
            Assert.IsTrue(ex.Message.Contains("Key", StringComparison.OrdinalIgnoreCase));
        }

        [TestMethod]
        public void Throws_When_TripleDes_Missing_IV()
        {
            var queueOptions = new DashboardQueueOptions
            {
                QueueName = "test",
                Interceptors = new DashboardInterceptorOptions
                {
                    TripleDes = new TripleDesInterceptorOptions
                    {
                        Key = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                }
            };
            Action act = () => InterceptorConfigurationBuilder.Resolve(queueOptions, EmptyProfiles);
            var ex = Assert.Throws<InvalidOperationException>(act);
            Assert.IsTrue(ex.Message.Contains("IV", StringComparison.OrdinalIgnoreCase));
        }

        [TestMethod]
        public void Returns_Action_When_Aes_Enabled()
        {
            var queueOptions = new DashboardQueueOptions
            {
                QueueName = "test",
                Interceptors = new DashboardInterceptorOptions
                {
                    Aes = new AesInterceptorOptions { Key = Convert.ToBase64String(new byte[32]) }
                }
            };
            var result = InterceptorConfigurationBuilder.Resolve(queueOptions, EmptyProfiles);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Throws_When_Aes_Missing_Key()
        {
            var queueOptions = new DashboardQueueOptions
            {
                QueueName = "test",
                Interceptors = new DashboardInterceptorOptions
                {
                    Aes = new AesInterceptorOptions()
                }
            };
            Action act = () => InterceptorConfigurationBuilder.Resolve(queueOptions, EmptyProfiles);
            var ex = Assert.Throws<InvalidOperationException>(act);
            Assert.IsTrue(ex.Message.Contains("Key", StringComparison.OrdinalIgnoreCase));
        }

        [TestMethod]
        public void Throws_When_Aes_Key_Wrong_Size()
        {
            var queueOptions = new DashboardQueueOptions
            {
                QueueName = "test",
                Interceptors = new DashboardInterceptorOptions
                {
                    Aes = new AesInterceptorOptions { Key = Convert.ToBase64String(new byte[16]) }
                }
            };
            Action act = () => InterceptorConfigurationBuilder.Resolve(queueOptions, EmptyProfiles);
            var ex = Assert.Throws<InvalidOperationException>(act);
            Assert.IsTrue(ex.Message.Contains("32", StringComparison.OrdinalIgnoreCase));
        }

        [TestMethod]
        public void Throws_When_Aes_Key_Not_Base64()
        {
            var queueOptions = new DashboardQueueOptions
            {
                QueueName = "test",
                Interceptors = new DashboardInterceptorOptions
                {
                    Aes = new AesInterceptorOptions { Key = "!!!not-base64!!!" }
                }
            };
            Action act = () => InterceptorConfigurationBuilder.Resolve(queueOptions, EmptyProfiles);
            var ex = Assert.Throws<InvalidOperationException>(act);
            Assert.IsTrue(ex.Message.Contains("Base64", StringComparison.OrdinalIgnoreCase));
        }

        [TestMethod]
        public void Returns_Null_When_Json_Options_Empty()
        {
            var queueOptions = new DashboardQueueOptions
            {
                QueueName = "test",
                Interceptors = new DashboardInterceptorOptions()
            };
            var result = InterceptorConfigurationBuilder.Resolve(queueOptions, EmptyProfiles);
            Assert.IsNull(result);
        }
    }
}
