using System;
using System.Collections.Generic;
using System.Linq;
using DotNetWorkQueue.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace DotNetWorkQueue.Tests.Serialization
{
    [TestClass]
    public class AllowListSerializationBinderTests
    {
        [TestMethod]
        public void BindToType_Unregistered_Type_Throws_JsonSerializationException()
        {
            var binder = Create();
            Assert.ThrowsExactly<JsonSerializationException>(
                delegate
                {
                    binder.BindToType(typeof(string).Assembly.FullName, "System.String");
                });
        }

        [TestMethod]
        public void BindToType_Registered_Type_Returns_Type()
        {
            var binder = Create();
            binder.AddAllowedType("System.String");
            var result = binder.BindToType(typeof(string).Assembly.FullName, "System.String");
            Assert.AreEqual(typeof(string), result);
        }

        [TestMethod]
        public void AddAllowedType_Type_Overload_Enables_Deserialization()
        {
            var binder = Create();
            binder.AddAllowedType(typeof(string));
            var result = binder.BindToType(typeof(string).Assembly.FullName, "System.String");
            Assert.AreEqual(typeof(string), result);
        }

        [TestMethod]
        public void AddAllowedTypes_Enables_Multiple_Types()
        {
            var binder = Create();
            binder.AddAllowedTypes(new List<string>
            {
                "System.String",
                "System.Int32"
            });

            var stringResult = binder.BindToType(typeof(string).Assembly.FullName, "System.String");
            Assert.AreEqual(typeof(string), stringResult);

            var intResult = binder.BindToType(typeof(int).Assembly.FullName, "System.Int32");
            Assert.AreEqual(typeof(int), intResult);
        }

        [TestMethod]
        public void BindToType_Registered_Type_Still_Blocks_Others()
        {
            var binder = Create();
            binder.AddAllowedType("System.String");
            Assert.ThrowsExactly<JsonSerializationException>(
                delegate
                {
                    binder.BindToType(typeof(int).Assembly.FullName, "System.Int32");
                });
        }

        [TestMethod]
        public void BindToName_Delegates_To_Default()
        {
            var binder = Create();
            binder.BindToName(typeof(AllowListSerializationBinderTests), out var assemblyName, out var typeName);
            Assert.IsNotNull(assemblyName, "BindToName should delegate to DefaultSerializationBinder and populate assemblyName");
        }

        [TestMethod]
        public void AddAllowedType_Null_String_Throws_ArgumentException()
        {
            var binder = Create();
            Assert.ThrowsExactly<ArgumentNullException>(
                delegate
                {
                    binder.AddAllowedType((string)null);
                });
        }

        [TestMethod]
        public void AddAllowedType_Null_Type_Throws_ArgumentNullException()
        {
            var binder = Create();
            Assert.ThrowsExactly<ArgumentNullException>(
                delegate
                {
                    binder.AddAllowedType((Type)null);
                });
        }

        [TestMethod]
        public void Empty_Allow_List_Blocks_Everything()
        {
            var binder = Create();
            Assert.ThrowsExactly<JsonSerializationException>(
                delegate
                {
                    binder.BindToType(typeof(object).Assembly.FullName, "System.Object");
                });
            Assert.ThrowsExactly<JsonSerializationException>(
                delegate
                {
                    binder.BindToType(typeof(string).Assembly.FullName, "System.String");
                });
            Assert.ThrowsExactly<JsonSerializationException>(
                delegate
                {
                    binder.BindToType(typeof(int).Assembly.FullName, "System.Int32");
                });
        }

        [TestMethod]
        [DataRow("System.Private.CoreLib")]
        [DataRow("mscorlib")]
        public void A_Generic_Type_Allowed_By_Type_Matches_The_Name_Json_Net_Writes(string coreLibrary)
        {
            //Type.FullName qualifies the argument with a version; the payload names it simply, and a
            //message written on .NET Framework names mscorlib
            var binder = Create();
            binder.AddAllowedType(typeof(List<string>));

            var result = binder.BindToType(typeof(List<string>).Assembly.GetName().Name,
                $"System.Collections.Generic.List`1[[System.String, {coreLibrary}]]");

            Assert.AreEqual(typeof(List<string>), result);
        }

        [TestMethod]
        public void A_Generic_Type_Allowed_By_Type_Matches_Its_Full_Name()
        {
            var binder = Create();
            binder.AddAllowedType(typeof(Dictionary<string, List<int>>));

            var result = binder.BindToType(typeof(Dictionary<string, List<int>>).Assembly.FullName,
                typeof(Dictionary<string, List<int>>).FullName);

            Assert.AreEqual(typeof(Dictionary<string, List<int>>), result);
        }

        [TestMethod]
        public void A_Generic_Type_Allowed_By_Type_Does_Not_Match_Other_Arguments()
        {
            var binder = Create();
            binder.AddAllowedType(typeof(List<string>));
            Assert.ThrowsExactly<JsonSerializationException>(
                delegate
                {
                    binder.BindToType(typeof(List<int>).Assembly.GetName().Name,
                        "System.Collections.Generic.List`1[[System.Int32, System.Private.CoreLib]]");
                });
        }

        [TestMethod]
        public void A_Polymorphic_Generic_Member_Round_Trips_Once_Allowed()
        {
            var binder = Create();
            binder.AddAllowedType(typeof(Holder));
            binder.AddAllowedType(typeof(List<string>));
            var serializer = new DotNetWorkQueue.Serialization.JsonSerializer(binder);
            var headers = new Dictionary<string, object>();

            var back = serializer.ConvertBytesToMessage<Holder>(
                serializer.ConvertMessageToBytes(new Holder { Value = new List<string> { "a" } }, headers), headers);

            Assert.AreSequenceEqual(new[] { "a" }, (List<string>)back.Value);
        }

        [TestMethod]
        [DataRow("System.String", "System.String")]
        [DataRow("A.B`1[[C.D, E, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]", "A.B`1[[C.D, E]]")]
        [DataRow("A.B`2[[C.D, E],[F.G`1[[H.I, J]], K]]", "A.B`2[[C.D, E],[F.G`1[[H.I, J]], K]]")]
        [DataRow("A.B`1[[C.D[,], E]][]", "A.B`1[[C.D[,], E]][]")]
        [DataRow("A.B`1[[System.String, mscorlib]]", "A.B`1[[System.String, System.Private.CoreLib]]")]
        [DataRow("A.B`1[[System.String, netstandard, Version=2.0.0.0]]", "A.B`1[[System.String, System.Private.CoreLib]]")]
        [DataRow("List`1[Tuple`2[String,Int32]]", "List`1[[Tuple`2[[String],[Int32]]]]")]
        [DataRow(@"A.B`1[[C\,D, E]]", @"A.B`1[[C\,D, E]]")]
        [DataRow("A.B`1[[C.D, E]", "A.B`1[[C.D, E]")]
        [DataRow(@"A.B`1[[C.D\", @"A.B`1[[C.D\")]
        [DataRow("A.B`1[[, E]]", "A.B`1[[, E]]")]
        [DataRow("A.B[x", "A.B[x")]
        [DataRow("A.B`1[[C.D, E[F]]]", "A.B`1[[C.D, E[F]]]")]
        [DataRow("A.B`1[[C.D, ]]", "A.B`1[[C.D, ]]")]
        [DataRow("A.B`1[[C.D, E] ", "A.B`1[[C.D, E] ")]
        [DataRow("A.B`1[[C.D, E]]x", "A.B`1[[C.D, E]]x")]
        public void CanonicalName_Keeps_Argument_Assemblies_Without_Their_Versions(string name, string expected)
        {
            Assert.AreEqual(expected, AllowListSerializationBinder.CanonicalName(name));
        }

        [TestMethod]
        public void A_Generic_Argument_From_Another_Assembly_Is_Rejected()
        {
            //the argument's assembly is part of the match; only its version is not
            var binder = Create();
            binder.AddAllowedType(typeof(List<Holder>));
            var name = typeof(List<Holder>).FullName;
            var otherAssembly = name.Replace(typeof(Holder).Assembly.GetName().Name + ",", "Other.Assembly,");
            Assert.AreNotEqual(name, otherAssembly);

            AssertNotAllowed(
                delegate { binder.BindToType(typeof(List<Holder>).Assembly.GetName().Name, otherAssembly); });
        }

        [TestMethod]
        public void A_Single_Bracket_Generic_Does_Not_Match_Other_Arguments()
        {
            //the comma between a nested generic's arguments is not an assembly separator
            var binder = Create();
            binder.AddAllowedType("System.Collections.Generic.List`1[System.Tuple`2[System.String,System.Int32]]");

            AssertNotAllowed(
                delegate { binder.BindToType(null, "System.Collections.Generic.List`1[System.Tuple`2[System.String,System.Boolean]]"); });
            Assert.AreEqual(typeof(List<Tuple<string, int>>),
                binder.BindToType(null, "System.Collections.Generic.List`1[System.Tuple`2[System.String,System.Int32]]"));
        }

        [TestMethod]
        public void An_Escaped_Comma_Is_Part_Of_The_Name()
        {
            var binder = Create();
            binder.AddAllowedType(@"A.B`1[[C\,D, E]]");
            AssertNotAllowed(
                delegate { binder.BindToType("A", @"A.B`1[[C\,X, E]]"); });
            //read without the escape, both of these would be "C\" from assembly "D"
            AssertNotAllowed(
                delegate { binder.BindToType("A", @"A.B`1[[C\,D, X]]"); });
        }

        [TestMethod]
        public void A_Deeply_Nested_Name_Is_Rejected_Without_Running_Out_Of_Stack()
        {
            //each level would be another recursive call; the parser stops at its limit instead
            var name = string.Concat(Enumerable.Repeat("A`1[[", 20000)) + "B" + string.Concat(Enumerable.Repeat("]]", 20000));
            Assert.AreEqual(name, AllowListSerializationBinder.CanonicalName(name));
            AssertNotAllowed(delegate { Create().BindToType("A", name); });
        }

        [TestMethod]
        public void Nesting_Within_The_Limit_Is_Still_Read()
        {
            var name = "B";
            for (var level = 0; level < 30; level++)
                name = $"A`1[[{name}, Asm, Version=1.0.0.0]]";

            //read, not returned as written: the versions are gone
            Assert.AreEqual(name.Replace(", Version=1.0.0.0", string.Empty), AllowListSerializationBinder.CanonicalName(name));
        }

        [TestMethod]
        public void A_Null_Type_Name_Is_Rejected_As_A_Serialization_Error()
        {
            var binder = Create();
            Assert.ThrowsExactly<JsonSerializationException>(delegate { binder.BindToType("A", null); });
        }

        //a rejection by the allow-list, not a later failure to load a type that does not exist
        private static void AssertNotAllowed(Action bind)
        {
            var error = Assert.ThrowsExactly<JsonSerializationException>(bind);
            StringAssert.Contains(error.Message, "is not allowed");
        }

        private class Holder
        {
            public object Value { get; set; }
        }

        private static AllowListSerializationBinder Create()
        {
            return new AllowListSerializationBinder();
        }
    }
}
