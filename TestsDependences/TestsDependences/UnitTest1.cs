using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;

namespace TestsDependences
{
    [TestClass]
    public class UnitTest1
    {
        private List<List<PropertySymbolInfo>> GetDependentProperties(string methodName)
        {
            var solution = Solution.Load(Path.GetFullPath(@"..\..\..\TestsDependences.sln"));
            var project =
                solution.Projects.First(
                    p => p.FilePath == Path.GetFullPath(@"..\..\..\ClassLibrary1\ClassLibrary1.csproj"));
            var document = project.Documents.First(d => d.Name == "Class1.cs");
            var @class =
                ((CompilationUnitSyntax) document.GetSyntaxRoot()).Members
                    .OfType<NamespaceDeclarationSyntax>()
                    .First()
                    .Members
                    .OfType<ClassDeclarationSyntax>()
                    .First();
            var semanticModel = project.GetCompilation().GetSemanticModel(document.GetSyntaxTree());
            var semanticModelsPerMethod = @class.Members.OfType<MethodDeclarationSyntax>().ToDictionary(m => m, m => semanticModel);
            var methodsPerMethodSymbol =
                @class.Members.OfType<MethodDeclarationSyntax>()
                    .ToDictionary(m => semanticModel.GetDeclaredSymbol(m).ToString());
            var method = @class.Members.OfType<MethodDeclarationSyntax>().First(m => m.Identifier.ValueText == methodName);
            var methodSymbol = (MethodSymbol) semanticModel.GetDeclaredSymbol(method);
            var getMembersVisitor = new GetMembersVisitor(semanticModel, new SpecificationsElements(), methodSymbol, methodSymbol.Parameters[0].Name, "Server.Fx.DAL.Interfaces", semanticModelsPerMethod, methodsPerMethodSymbol, new Dictionary<string, List<MethodDeclarationSyntax>>(), @class.Members.OfType<MethodDeclarationSyntax>().ToList());
            getMembersVisitor.Visit(method);
            return getMembersVisitor.GetProperties();
        }

        [TestMethod]
        public void TestNone()
        {
            var properties = GetDependentProperties("None");
            Assert.AreEqual(0, properties.Count);
        }

        [TestMethod]
        public void TestGet()
        {
            var properties = GetDependentProperties("Get");
            Assert.AreEqual(0, properties.Count);
        }

        [TestMethod]
        public void TestSimple()
        {
            var properties = GetDependentProperties("Simple");
            Assert.AreEqual(1, properties.Count);
            Assert.AreEqual(1, properties[0].Count);
            Assert.AreEqual("CompanyName", properties[0][0].Name);
        }

        [TestMethod]
        public void TestDouble()
        {
            var properties = GetDependentProperties("Double");
            Assert.AreEqual(2, properties.Count);
            Assert.AreEqual(1, properties[0].Count);
            Assert.AreEqual("CompanyName", properties[0][0].Name);
            Assert.AreEqual(1, properties[1].Count);
            Assert.AreEqual("ContactName", properties[1][0].Name);
        }

        [TestMethod]
        public void TestDepth2()
        {
            var properties = GetDependentProperties("Depth2");
            Assert.AreEqual(2, properties.Count);
            Assert.AreEqual(1, properties[0].Count);
            Assert.AreEqual("Order", properties[0][0].Name);
            Assert.AreEqual(2, properties[1].Count);
            Assert.AreEqual("Order", properties[1][0].Name);
            Assert.AreEqual("Date", properties[1][1].Name);
        }

        [TestMethod]
        public void TestDepth3()
        {
            var properties = GetDependentProperties("Depth3");
            Assert.AreEqual(3, properties.Count);
            Assert.AreEqual(1, properties[0].Count);
            Assert.AreEqual("Order", properties[0][0].Name);
            Assert.AreEqual(2, properties[1].Count);
            Assert.AreEqual("Order", properties[1][0].Name);
            Assert.AreEqual("Customer", properties[1][1].Name);
            Assert.AreEqual(3, properties[2].Count);
            Assert.AreEqual("Order", properties[2][0].Name);
            Assert.AreEqual("Customer", properties[2][1].Name);
            Assert.AreEqual("CompanyName", properties[2][2].Name);
        }

        [TestMethod]
        public void TestLamda()
        {
            var properties = GetDependentProperties("Lamda");
            Assert.AreEqual(4, properties.Count);
            Assert.AreEqual(1, properties[0].Count);
            Assert.AreEqual("OrderDetails", properties[0][0].Name);
            Assert.AreEqual(2, properties[1].Count);
            Assert.AreEqual("OrderDetails", properties[1][0].Name);
            Assert.AreEqual("UnitPrice", properties[1][1].Name);
            Assert.AreEqual(2, properties[2].Count);
            Assert.AreEqual("OrderDetails", properties[2][0].Name);
            Assert.AreEqual("Quantity", properties[2][1].Name);
            Assert.AreEqual(2, properties[3].Count);
            Assert.AreEqual("OrderDetails", properties[3][0].Name);
            Assert.AreEqual("Discount", properties[3][1].Name);
        }

        [TestMethod]
        public void TestCallMethod()
        {
            var properties = GetDependentProperties("CallMethod");
            Assert.AreEqual(3, properties.Count);
            Assert.AreEqual(1, properties[0].Count);
            Assert.AreEqual("Order", properties[0][0].Name);
            Assert.AreEqual(2, properties[1].Count);
            Assert.AreEqual("Order", properties[1][0].Name);
            Assert.AreEqual("Customer", properties[1][1].Name);
            Assert.AreEqual(3, properties[2].Count);
            Assert.AreEqual("Order", properties[2][0].Name);
            Assert.AreEqual("Customer", properties[2][1].Name);
            Assert.AreEqual("CompanyName", properties[2][2].Name);
        }

        [TestMethod]
        public void TestCallMethod2()
        {
            var properties = GetDependentProperties("CallMethod2");
            Assert.AreEqual(4, properties.Count);
            Assert.AreEqual(1, properties[0].Count);
            Assert.AreEqual("OrderDetails", properties[0][0].Name);
            Assert.AreEqual(2, properties[1].Count);
            Assert.AreEqual("OrderDetails", properties[1][0].Name);
            Assert.AreEqual("Order", properties[1][1].Name);
            Assert.AreEqual(3, properties[2].Count);
            Assert.AreEqual("OrderDetails", properties[2][0].Name);
            Assert.AreEqual("Order", properties[2][1].Name);
            Assert.AreEqual("Customer", properties[2][2].Name);
            Assert.AreEqual(4, properties[3].Count);
            Assert.AreEqual("OrderDetails", properties[3][0].Name);
            Assert.AreEqual("Order", properties[3][1].Name);
            Assert.AreEqual("Customer", properties[3][2].Name);
            Assert.AreEqual("CompanyName", properties[3][3].Name);
        }

        [TestMethod]
        public void TestVariable()
        {
            var properties = GetDependentProperties("Variable");
            Assert.AreEqual(2, properties.Count);
            Assert.AreEqual(1, properties[0].Count);
            Assert.AreEqual("Order", properties[0][0].Name);
            Assert.AreEqual(2, properties[1].Count);
            Assert.AreEqual("Order", properties[1][0].Name);
            Assert.AreEqual("Customer", properties[1][1].Name);
        }

        [TestMethod]
        public void TestLINQSelect()
        {
            var properties = GetDependentProperties("LINQSelect");
            Assert.AreEqual(3, properties.Count);
            Assert.AreEqual(1, properties[0].Count);
            Assert.AreEqual("OrderDetails", properties[0][0].Name);
            Assert.AreEqual(2, properties[1].Count);
            Assert.AreEqual("OrderDetails", properties[1][0].Name);
            Assert.AreEqual("UnitPrice", properties[1][1].Name);
            Assert.AreEqual(2, properties[2].Count);
            Assert.AreEqual("OrderDetails", properties[2][0].Name);
            Assert.AreEqual("Quantity", properties[2][1].Name);
        }

        [TestMethod]
        public void TestLINQWhere()
        {
            var properties = GetDependentProperties("LINQWhere");
            Assert.AreEqual(3, properties.Count);
            Assert.AreEqual(1, properties[0].Count);
            Assert.AreEqual("OrderDetails", properties[0][0].Name);
            Assert.AreEqual(2, properties[1].Count);
            Assert.AreEqual("OrderDetails", properties[1][0].Name);
            Assert.AreEqual("UnitPrice", properties[1][1].Name);
            Assert.AreEqual(2, properties[2].Count);
            Assert.AreEqual("OrderDetails", properties[2][0].Name);
            Assert.AreEqual("Quantity", properties[2][1].Name);
        }

        [TestMethod]
        public void TestLINQWhereAndSelect()
        {
            var properties = GetDependentProperties("LINQWhereAndSelect");
            Assert.AreEqual(4, properties.Count);
            Assert.AreEqual(1, properties[0].Count);
            Assert.AreEqual("OrderDetails", properties[0][0].Name);
            Assert.AreEqual(2, properties[1].Count);
            Assert.AreEqual("OrderDetails", properties[1][0].Name);
            Assert.AreEqual("UnitPrice", properties[1][1].Name);
            Assert.AreEqual(2, properties[2].Count);
            Assert.AreEqual("OrderDetails", properties[2][0].Name);
            Assert.AreEqual("Quantity", properties[2][1].Name);
            Assert.AreEqual(2, properties[3].Count);
            Assert.AreEqual("OrderDetails", properties[3][0].Name);
            Assert.AreEqual("Discount", properties[3][1].Name);
        }

        [TestMethod]
        public void TestLINQOrderBy()
        {
            var properties = GetDependentProperties("LINQOrderBy");
            Assert.AreEqual(5, properties.Count);
            Assert.AreEqual(1, properties[0].Count);
            Assert.AreEqual("OrderDetails", properties[0][0].Name);
            Assert.AreEqual(2, properties[1].Count);
            Assert.AreEqual("OrderDetails", properties[1][0].Name);
            Assert.AreEqual("UnitPrice", properties[1][1].Name);
            Assert.AreEqual(2, properties[2].Count);
            Assert.AreEqual("OrderDetails", properties[2][0].Name);
            Assert.AreEqual("Quantity", properties[2][1].Name);
            Assert.AreEqual(2, properties[3].Count);
            Assert.AreEqual("OrderDetails", properties[3][0].Name);
            Assert.AreEqual("Order", properties[3][1].Name);
            Assert.AreEqual(3, properties[4].Count);
            Assert.AreEqual("OrderDetails", properties[4][0].Name);
            Assert.AreEqual("Order", properties[4][1].Name);
            Assert.AreEqual("Date", properties[4][2].Name);
        }

        [TestMethod]
        public void TestLINQOrderByAndSelect()
        {
            var properties = GetDependentProperties("LINQOrderByAndSelect");
            Assert.AreEqual(6, properties.Count);
            Assert.AreEqual(1, properties[0].Count);
            Assert.AreEqual("OrderDetails", properties[0][0].Name);
            Assert.AreEqual(2, properties[1].Count);
            Assert.AreEqual("OrderDetails", properties[1][0].Name);
            Assert.AreEqual("UnitPrice", properties[1][1].Name);
            Assert.AreEqual(2, properties[2].Count);
            Assert.AreEqual("OrderDetails", properties[2][0].Name);
            Assert.AreEqual("Quantity", properties[2][1].Name);
            Assert.AreEqual(2, properties[3].Count);
            Assert.AreEqual("OrderDetails", properties[3][0].Name);
            Assert.AreEqual("Order", properties[3][1].Name);
            Assert.AreEqual(3, properties[4].Count);
            Assert.AreEqual("OrderDetails", properties[4][0].Name);
            Assert.AreEqual("Order", properties[4][1].Name);
            Assert.AreEqual("Date", properties[4][2].Name);
            Assert.AreEqual(2, properties[5].Count);
            Assert.AreEqual("OrderDetails", properties[5][0].Name);
            Assert.AreEqual("Discount", properties[5][1].Name);
        }

        [TestMethod]
        public void TestLINQLet()
        {
            var properties = GetDependentProperties("LINQLet");
            Assert.AreEqual(6, properties.Count);
            Assert.AreEqual(1, properties[0].Count);
            Assert.AreEqual("OrderDetails", properties[0][0].Name);
            Assert.AreEqual(2, properties[1].Count);
            Assert.AreEqual("OrderDetails", properties[1][0].Name);
            Assert.AreEqual("Order", properties[1][1].Name);
            Assert.AreEqual(3, properties[2].Count);
            Assert.AreEqual("OrderDetails", properties[2][0].Name);
            Assert.AreEqual("Order", properties[2][1].Name);
            Assert.AreEqual("Customer", properties[2][2].Name);
            Assert.AreEqual(1, properties[3].Count);
            Assert.AreEqual("Customer", properties[3][0].Name);
            Assert.AreEqual(3, properties[4].Count);
            Assert.AreEqual("OrderDetails", properties[4][0].Name);
            Assert.AreEqual("Order", properties[4][1].Name);
            Assert.AreEqual("Date", properties[4][2].Name);
            Assert.AreEqual(2, properties[5].Count);
            Assert.AreEqual("OrderDetails", properties[5][0].Name);
            Assert.AreEqual("UnitPrice", properties[5][1].Name);
        }

        [TestMethod]
        public void TestLINQGroupBy()
        {
            var properties = GetDependentProperties("LINQGroupBy");
            Assert.AreEqual(8, properties.Count);
            Assert.AreEqual(1, properties[0].Count);
            Assert.AreEqual("Orders", properties[0][0].Name);
            Assert.IsTrue(properties.Any(ps => 2 == ps.Count && "Orders" == ps[0].Name && "Date" == ps[1].Name));
            Assert.IsTrue(properties.Any(ps => 2 == ps.Count && "Orders" == ps[0].Name && "OrderDetails" == ps[1].Name));
            Assert.IsTrue(properties.Any(ps => 3 == ps.Count && "Orders" == ps[0].Name && "OrderDetails" == ps[1].Name && "UnitPrice" == ps[2].Name));
            Assert.IsTrue(properties.Any(ps => 3 == ps.Count && "Orders" == ps[0].Name && "OrderDetails" == ps[1].Name && "Quantity" == ps[2].Name));
            Assert.IsTrue(properties.Any(ps => 3 == ps.Count && "Orders" == ps[0].Name && "OrderDetails" == ps[1].Name && "Discount" == ps[2].Name));
            Assert.IsTrue(properties.Any(ps => 2 == ps.Count && "Orders" == ps[0].Name && "Customer" == ps[1].Name));
            Assert.IsTrue(properties.Any(ps => 3 == ps.Count && "Orders" == ps[0].Name && "Customer" == ps[1].Name && "CompanyName" == ps[2].Name));
        }

        [TestMethod]
        public void TestLINQGroupBy2()
        {
            var properties = GetDependentProperties("LINQGroupBy2");
            Assert.AreEqual(5, properties.Count);
            Assert.IsTrue(properties.Any(ps => 1 == ps.Count && "OrderDetails" == ps[0].Name));
            Assert.IsTrue(properties.Any(ps => 2 == ps.Count && "OrderDetails" == ps[0].Name && "Order" == ps[1].Name));
            Assert.IsTrue(properties.Any(ps => 3 == ps.Count && "OrderDetails" == ps[0].Name && "Order" == ps[1].Name && "Customer" == ps[2].Name));
            Assert.IsTrue(properties.Any(ps => 4 == ps.Count && "OrderDetails" == ps[0].Name && "Order" == ps[1].Name && "Customer" == ps[2].Name && "CompanyName" == ps[3].Name));
            Assert.IsTrue(properties.Any(ps => 2 == ps.Count && "OrderDetails" == ps[0].Name && "Quantity" == ps[1].Name));
        }

        [TestMethod]
        public void TestLINQDoubleFrom()
        {
            var properties = GetDependentProperties("LINQDoubleFrom");
            Assert.AreEqual(3, properties.Count);
            Assert.AreEqual(1, properties[0].Count);
            Assert.AreEqual("Orders", properties[0][0].Name);
            Assert.AreEqual(2, properties[1].Count);
            Assert.AreEqual("Orders", properties[1][0].Name);
            Assert.AreEqual("OrderDetails", properties[1][1].Name);
            Assert.AreEqual(3, properties[2].Count);
            Assert.AreEqual("Orders", properties[2][0].Name);
            Assert.AreEqual("OrderDetails", properties[2][1].Name);
            Assert.AreEqual("Quantity", properties[2][2].Name);
        }

        [TestMethod]
        public void TestLINQJoin()
        {
            var properties = GetDependentProperties("LINQJoin");
            Assert.AreEqual(6, properties.Count);
            Assert.AreEqual(1, properties[0].Count);
            Assert.AreEqual("Orders", properties[0][0].Name);
            Assert.AreEqual(1, properties[1].Count);
            Assert.AreEqual("Orders2", properties[1][0].Name);
            Assert.AreEqual(2, properties[2].Count);
            Assert.AreEqual("Orders", properties[2][0].Name);
            Assert.AreEqual("Id", properties[2][1].Name);
            Assert.AreEqual(2, properties[3].Count);
            Assert.AreEqual("Orders2", properties[3][0].Name);
            Assert.AreEqual("Id", properties[3][1].Name);
            Assert.AreEqual(2, properties[4].Count);
            Assert.AreEqual("Orders", properties[4][0].Name);
            Assert.AreEqual("Date", properties[4][1].Name);
            Assert.AreEqual(2, properties[5].Count);
            Assert.AreEqual("Orders2", properties[5][0].Name);
            Assert.AreEqual("Date", properties[5][1].Name);
        }

        [TestMethod]
        public void TestLINQSelectMethod()
        {
            var properties = GetDependentProperties("LINQSelectMethod");
            Assert.AreEqual(3, properties.Count);
            Assert.AreEqual(1, properties[0].Count);
            Assert.AreEqual("OrderDetails", properties[0][0].Name);
            Assert.AreEqual(2, properties[1].Count);
            Assert.AreEqual("OrderDetails", properties[1][0].Name);
            Assert.AreEqual("UnitPrice", properties[1][1].Name);
            Assert.AreEqual(2, properties[2].Count);
            Assert.AreEqual("OrderDetails", properties[2][0].Name);
            Assert.AreEqual("Quantity", properties[2][1].Name);
        }

        [TestMethod]
        public void TestLINQSelectMethod2()
        {
            var properties = GetDependentProperties("LINQSelectMethod2");
            Assert.AreEqual(2, properties.Count);
            Assert.AreEqual(1, properties[0].Count);
            Assert.AreEqual("OrderDetails", properties[0][0].Name);
            Assert.AreEqual(2, properties[1].Count);
            Assert.AreEqual("OrderDetails", properties[1][0].Name);
            Assert.AreEqual("OrderId", properties[1][1].Name);
        }

        [TestMethod]
        public void TestLINQWhereMethod()
        {
            var properties = GetDependentProperties("LINQWhere");
            Assert.AreEqual(3, properties.Count);
            Assert.AreEqual(1, properties[0].Count);
            Assert.AreEqual("OrderDetails", properties[0][0].Name);
            Assert.AreEqual(2, properties[1].Count);
            Assert.AreEqual("OrderDetails", properties[1][0].Name);
            Assert.AreEqual("UnitPrice", properties[1][1].Name);
            Assert.AreEqual(2, properties[2].Count);
            Assert.AreEqual("OrderDetails", properties[2][0].Name);
            Assert.AreEqual("Quantity", properties[2][1].Name);
        }

        [TestMethod]
        public void TestLINQWhereAndSelectMethod()
        {
            var properties = GetDependentProperties("LINQWhereAndSelectMethod");
            Assert.AreEqual(4, properties.Count);
            Assert.AreEqual(1, properties[0].Count);
            Assert.AreEqual("OrderDetails", properties[0][0].Name);
            Assert.AreEqual(2, properties[1].Count);
            Assert.AreEqual("OrderDetails", properties[1][0].Name);
            Assert.AreEqual("UnitPrice", properties[1][1].Name);
            Assert.AreEqual(2, properties[2].Count);
            Assert.AreEqual("OrderDetails", properties[2][0].Name);
            Assert.AreEqual("Quantity", properties[2][1].Name);
            Assert.AreEqual(2, properties[3].Count);
            Assert.AreEqual("OrderDetails", properties[3][0].Name);
            Assert.AreEqual("Discount", properties[3][1].Name);
        }

        [TestMethod]
        public void TestLINQOrderByMethod()
        {
            var properties = GetDependentProperties("LINQOrderByMethod");
            Assert.AreEqual(5, properties.Count);
            Assert.AreEqual(1, properties[0].Count);
            Assert.AreEqual("OrderDetails", properties[0][0].Name);
            Assert.AreEqual(2, properties[1].Count);
            Assert.AreEqual("OrderDetails", properties[1][0].Name);
            Assert.AreEqual("UnitPrice", properties[1][1].Name);
            Assert.AreEqual(2, properties[2].Count);
            Assert.AreEqual("OrderDetails", properties[2][0].Name);
            Assert.AreEqual("Quantity", properties[2][1].Name);
            Assert.AreEqual(2, properties[3].Count);
            Assert.AreEqual("OrderDetails", properties[3][0].Name);
            Assert.AreEqual("Order", properties[3][1].Name);
            Assert.AreEqual(3, properties[4].Count);
            Assert.AreEqual("OrderDetails", properties[4][0].Name);
            Assert.AreEqual("Order", properties[4][1].Name);
            Assert.AreEqual("Date", properties[4][2].Name);
        }

        [TestMethod]
        public void TestLINQOrderByMethod2()
        {
            var properties = GetDependentProperties("LINQOrderByMethod2");
            Assert.AreEqual(5, properties.Count);
            Assert.AreEqual(1, properties[0].Count);
            Assert.AreEqual("OrderDetails", properties[0][0].Name);
            Assert.AreEqual(2, properties[1].Count);
            Assert.AreEqual("OrderDetails", properties[1][0].Name);
            Assert.AreEqual("UnitPrice", properties[1][1].Name);
            Assert.AreEqual(2, properties[2].Count);
            Assert.AreEqual("OrderDetails", properties[2][0].Name);
            Assert.AreEqual("Quantity", properties[2][1].Name);
            Assert.AreEqual(2, properties[3].Count);
            Assert.AreEqual("OrderDetails", properties[3][0].Name);
            Assert.AreEqual("Order", properties[3][1].Name);
            Assert.AreEqual(3, properties[4].Count);
            Assert.AreEqual("OrderDetails", properties[4][0].Name);
            Assert.AreEqual("Order", properties[4][1].Name);
            Assert.AreEqual("Date", properties[4][2].Name);
        }

        [TestMethod]
        public void TestLINQOrderByAndSelectMethod()
        {
            var properties = GetDependentProperties("LINQOrderByAndSelectMethod");
            Assert.AreEqual(6, properties.Count);
            Assert.AreEqual(1, properties[0].Count);
            Assert.AreEqual("OrderDetails", properties[0][0].Name);
            Assert.AreEqual(2, properties[1].Count);
            Assert.AreEqual("OrderDetails", properties[1][0].Name);
            Assert.AreEqual("UnitPrice", properties[1][1].Name);
            Assert.AreEqual(2, properties[2].Count);
            Assert.AreEqual("OrderDetails", properties[2][0].Name);
            Assert.AreEqual("Quantity", properties[2][1].Name);
            Assert.AreEqual(2, properties[3].Count);
            Assert.AreEqual("OrderDetails", properties[3][0].Name);
            Assert.AreEqual("Order", properties[3][1].Name);
            Assert.AreEqual(3, properties[4].Count);
            Assert.AreEqual("OrderDetails", properties[4][0].Name);
            Assert.AreEqual("Order", properties[4][1].Name);
            Assert.AreEqual("Date", properties[4][2].Name);
            Assert.AreEqual(2, properties[5].Count);
            Assert.AreEqual("OrderDetails", properties[5][0].Name);
            Assert.AreEqual("Discount", properties[5][1].Name);
        }

        [TestMethod]
        public void TestLINQLetMethod()
        {
            var properties = GetDependentProperties("LINQLetMethod");
            Assert.AreEqual(6, properties.Count);
            Assert.AreEqual(1, properties[0].Count);
            Assert.AreEqual("OrderDetails", properties[0][0].Name);
            Assert.AreEqual(2, properties[1].Count);
            Assert.AreEqual("OrderDetails", properties[1][0].Name);
            Assert.AreEqual("Order", properties[1][1].Name);
            Assert.AreEqual(3, properties[2].Count);
            Assert.AreEqual("OrderDetails", properties[2][0].Name);
            Assert.AreEqual("Order", properties[2][1].Name);
            Assert.AreEqual("Customer", properties[2][2].Name);
            Assert.AreEqual(1, properties[3].Count);
            Assert.AreEqual("Customer", properties[3][0].Name);
            Assert.AreEqual(3, properties[4].Count);
            Assert.AreEqual("OrderDetails", properties[4][0].Name);
            Assert.AreEqual("Order", properties[4][1].Name);
            Assert.AreEqual("Date", properties[4][2].Name);
            Assert.AreEqual(2, properties[5].Count);
            Assert.AreEqual("OrderDetails", properties[5][0].Name);
            Assert.AreEqual("UnitPrice", properties[5][1].Name);
        }

        [TestMethod]
        public void TestLINQGroupByMethod()
        {
            var properties = GetDependentProperties("LINQGroupByMethod");
            Assert.AreEqual(8, properties.Count);
            Assert.AreEqual(1, properties[0].Count);
            Assert.AreEqual("Orders", properties[0][0].Name);
            Assert.AreEqual(2, properties[1].Count);
            Assert.AreEqual("Orders", properties[1][0].Name);
            Assert.AreEqual("Date", properties[1][1].Name);
            Assert.AreEqual(2, properties[2].Count);
            Assert.AreEqual("Orders", properties[2][0].Name);
            Assert.AreEqual("OrderDetails", properties[2][1].Name);
            Assert.AreEqual(3, properties[3].Count);
            Assert.AreEqual("Orders", properties[3][0].Name);
            Assert.AreEqual("OrderDetails", properties[3][1].Name);
            Assert.AreEqual("UnitPrice", properties[3][2].Name);
            Assert.AreEqual(3, properties[4].Count);
            Assert.AreEqual("Orders", properties[4][0].Name);
            Assert.AreEqual("OrderDetails", properties[4][1].Name);
            Assert.AreEqual("Quantity", properties[4][2].Name);
            Assert.AreEqual(3, properties[5].Count);
            Assert.AreEqual("Orders", properties[5][0].Name);
            Assert.AreEqual("OrderDetails", properties[5][1].Name);
            Assert.AreEqual("Discount", properties[5][2].Name);
            Assert.AreEqual(2, properties[6].Count);
            Assert.AreEqual("Orders", properties[6][0].Name);
            Assert.AreEqual("Customer", properties[6][1].Name);
            Assert.AreEqual(3, properties[7].Count);
            Assert.AreEqual("Orders", properties[7][0].Name);
            Assert.AreEqual("Customer", properties[7][1].Name);
            Assert.AreEqual("CompanyName", properties[7][2].Name);
        }

        [TestMethod]
        public void TestLINQDoubleFromMethod()
        {
            var properties = GetDependentProperties("LINQDoubleFromMethod");
            Assert.AreEqual(3, properties.Count);
            Assert.AreEqual(1, properties[0].Count);
            Assert.AreEqual("Orders", properties[0][0].Name);
            Assert.AreEqual(2, properties[1].Count);
            Assert.AreEqual("Orders", properties[1][0].Name);
            Assert.AreEqual("OrderDetails", properties[1][1].Name);
            Assert.AreEqual(3, properties[2].Count);
            Assert.AreEqual("Orders", properties[2][0].Name);
            Assert.AreEqual("OrderDetails", properties[2][1].Name);
            Assert.AreEqual("Quantity", properties[2][2].Name);
        }

        [TestMethod]
        public void TestLINQJoinMethod()
        {
            var properties = GetDependentProperties("LINQJoinMethod");
            Assert.AreEqual(6, properties.Count);
            Assert.AreEqual(1, properties[0].Count);
            Assert.AreEqual("Orders", properties[0][0].Name);
            Assert.AreEqual(1, properties[1].Count);
            Assert.AreEqual("Orders2", properties[1][0].Name);
            Assert.AreEqual(2, properties[2].Count);
            Assert.AreEqual("Orders", properties[2][0].Name);
            Assert.AreEqual("Id", properties[2][1].Name);
            Assert.AreEqual(2, properties[3].Count);
            Assert.AreEqual("Orders2", properties[3][0].Name);
            Assert.AreEqual("Id", properties[3][1].Name);
            Assert.AreEqual(2, properties[4].Count);
            Assert.AreEqual("Orders", properties[4][0].Name);
            Assert.AreEqual("Date", properties[4][1].Name);
            Assert.AreEqual(2, properties[5].Count);
            Assert.AreEqual("Orders2", properties[5][0].Name);
            Assert.AreEqual("Date", properties[5][1].Name);
        }

        [TestMethod]
        public void TestGetC()
        {
            var properties = GetDependentProperties("GetC");
            Assert.AreEqual(2, properties.Count);
            Assert.AreEqual(1, properties[0].Count);
            Assert.AreEqual("Id", properties[0][0].Name);
            Assert.AreEqual(1, properties[1].Count);
            Assert.AreEqual("OrderId", properties[1][0].Name);
        }

        [TestMethod]
        public void TestGetCs()
        {
            var properties = GetDependentProperties("GetCs");
            Assert.AreEqual(2, properties.Count);
            Assert.AreEqual(1, properties[0].Count);
            Assert.AreEqual("Id", properties[0][0].Name);
            Assert.AreEqual(1, properties[1].Count);
            Assert.AreEqual("OrderId", properties[1][0].Name);
        }

        [TestMethod]
        public void TestTestGetCs()
        {
            var properties = GetDependentProperties("TestGetCs");
            Assert.AreEqual(4, properties.Count);
            Assert.AreEqual(1, properties[0].Count);
            Assert.AreEqual("OrderDetails", properties[0][0].Name);
            Assert.AreEqual(2, properties[1].Count);
            Assert.AreEqual("OrderDetails", properties[1][0].Name);
            Assert.AreEqual("Id", properties[1][1].Name);
            Assert.AreEqual(2, properties[2].Count);
            Assert.AreEqual("OrderDetails", properties[2][0].Name);
            Assert.AreEqual("Quantity", properties[2][1].Name);
            Assert.AreEqual(2, properties[3].Count);
            Assert.AreEqual("OrderDetails", properties[3][0].Name);
            Assert.AreEqual("OrderId", properties[3][1].Name);
        }

        [TestMethod]
        public void TestTestGetCs2()
        {
            var properties = GetDependentProperties("TestGetCs2");
            Assert.AreEqual(5, properties.Count);
            Assert.AreEqual(1, properties[0].Count);
            Assert.AreEqual("OrderDetails", properties[0][0].Name);
            Assert.AreEqual(2, properties[1].Count);
            Assert.AreEqual("OrderDetails", properties[1][0].Name);
            Assert.AreEqual("Id", properties[1][1].Name);
            Assert.AreEqual(2, properties[2].Count);
            Assert.AreEqual("OrderDetails", properties[2][0].Name);
            Assert.AreEqual("Quantity", properties[2][1].Name);
            Assert.AreEqual(2, properties[3].Count);
            Assert.AreEqual("OrderDetails", properties[3][0].Name);
            Assert.AreEqual("Order", properties[3][1].Name);
            Assert.AreEqual(2, properties[4].Count);
            Assert.AreEqual("OrderDetails", properties[4][0].Name);
            Assert.AreEqual("OrderId", properties[4][1].Name);
        }

        [TestMethod]
        public void TestGetOrders()
        {
            var properties = GetDependentProperties("GetOrders");
            Assert.AreEqual(6, properties.Count);
            Assert.AreEqual(1, properties[0].Count);
            Assert.AreEqual("Id", properties[0][0].Name);
            Assert.AreEqual(1, properties[1].Count);
            Assert.AreEqual("Orders", properties[1][0].Name);
            Assert.AreEqual(2, properties[2].Count);
            Assert.AreEqual("Orders", properties[2][0].Name);
            Assert.AreEqual("Date", properties[2][1].Name);
            Assert.AreEqual(1, properties[3].Count);
            Assert.AreEqual("Orders2", properties[3][0].Name);
            Assert.AreEqual(2, properties[4].Count);
            Assert.AreEqual("Orders2", properties[4][0].Name);
            Assert.AreEqual("OrderDetails", properties[4][1].Name);
            Assert.AreEqual(3, properties[5].Count);
            Assert.AreEqual("Orders2", properties[5][0].Name);
            Assert.AreEqual("OrderDetails", properties[5][1].Name);
            Assert.AreEqual("Order", properties[5][2].Name);
        }

        [TestMethod]
        public void TestTestReturnMethodWithLINQ()
        {
            var properties = GetDependentProperties("TestReturnMethodWithLINQ");
            Assert.AreEqual(8, properties.Count);
            Assert.AreEqual(1, properties[0].Count);
            Assert.AreEqual("Id", properties[0][0].Name);
            Assert.AreEqual(1, properties[1].Count);
            Assert.AreEqual("Orders", properties[1][0].Name);
            Assert.AreEqual(2, properties[2].Count);
            Assert.AreEqual("Orders", properties[2][0].Name);
            Assert.AreEqual("Date", properties[2][1].Name);
            Assert.AreEqual(1, properties[3].Count);
            Assert.AreEqual("Orders2", properties[3][0].Name);
            Assert.AreEqual(2, properties[4].Count);
            Assert.AreEqual("Orders2", properties[4][0].Name);
            Assert.AreEqual("OrderDetails", properties[4][1].Name);
            Assert.AreEqual(3, properties[5].Count);
            Assert.AreEqual("Orders2", properties[5][0].Name);
            Assert.AreEqual("OrderDetails", properties[5][1].Name);
            Assert.AreEqual("Order", properties[5][2].Name);
            Assert.AreEqual(2, properties[6].Count);
            Assert.AreEqual("Orders", properties[6][0].Name);
            Assert.AreEqual("Id", properties[6][1].Name);
            Assert.AreEqual(4, properties[7].Count);
            Assert.AreEqual("Orders2", properties[7][0].Name);
            Assert.AreEqual("OrderDetails", properties[7][1].Name);
            Assert.AreEqual("Order", properties[7][2].Name);
            Assert.AreEqual("Id", properties[7][3].Name);
        }

        [TestMethod]
        public void TestTestGetC2()
        {
            var properties = GetDependentProperties("TestGetC2");
            Assert.AreEqual(10, properties.Count);
            Assert.AreEqual(1, properties[0].Count);
            Assert.AreEqual("Customer", properties[0][0].Name);
            Assert.AreEqual(2, properties[1].Count);
            Assert.AreEqual("Customer", properties[1][0].Name);
            Assert.AreEqual("Id", properties[1][1].Name);
            Assert.AreEqual(2, properties[2].Count);
            Assert.AreEqual("Customer", properties[2][0].Name);
            Assert.AreEqual("Orders", properties[2][1].Name);
            Assert.AreEqual(3, properties[3].Count);
            Assert.AreEqual("Customer", properties[3][0].Name);
            Assert.AreEqual("Orders", properties[3][1].Name);
            Assert.AreEqual("OrderDetails", properties[3][2].Name);
            Assert.AreEqual(2, properties[4].Count);
            Assert.AreEqual("Customer", properties[4][0].Name);
            Assert.AreEqual("Orders2", properties[4][1].Name);
            Assert.AreEqual(3, properties[5].Count);
            Assert.AreEqual("Customer", properties[5][0].Name);
            Assert.AreEqual("Orders2", properties[5][1].Name);
            Assert.AreEqual("OrderDetails", properties[5][2].Name);
            Assert.AreEqual(4, properties[6].Count);
            Assert.AreEqual("Customer", properties[6][0].Name);
            Assert.AreEqual("Orders", properties[6][1].Name);
            Assert.AreEqual("OrderDetails", properties[6][2].Name);
            Assert.AreEqual("UnitPrice", properties[6][3].Name);
            Assert.AreEqual(4, properties[7].Count);
            Assert.AreEqual("Customer", properties[7][0].Name);
            Assert.AreEqual("Orders2", properties[7][1].Name);
            Assert.AreEqual("OrderDetails", properties[7][2].Name);
            Assert.AreEqual("UnitPrice", properties[7][3].Name);
            Assert.AreEqual(1, properties[8].Count);
            Assert.AreEqual("Id", properties[8][0].Name);
            Assert.AreEqual(3, properties[9].Count);
            Assert.AreEqual("Customer", properties[9][0].Name);
            Assert.AreEqual("Orders2", properties[9][1].Name);
            Assert.AreEqual("Id", properties[9][2].Name);
        }

        [TestMethod]
        public void TestTestUnion()
        {
            var properties = GetDependentProperties("TestUnion");
            Assert.AreEqual(4, properties.Count);
            Assert.AreEqual(1, properties[0].Count);
            Assert.AreEqual("Orders", properties[0][0].Name);
            Assert.AreEqual(1, properties[1].Count);
            Assert.AreEqual("Orders2", properties[1][0].Name);
            Assert.AreEqual(2, properties[2].Count);
            Assert.AreEqual("Orders", properties[2][0].Name);
            Assert.AreEqual("Id", properties[2][1].Name);
            Assert.AreEqual(2, properties[3].Count);
            Assert.AreEqual("Orders2", properties[3][0].Name);
            Assert.AreEqual("Id", properties[3][1].Name);
        }

        [TestMethod]
        public void TestCallTwice()
        {
            var properties = GetDependentProperties("CallTwice");
            Assert.AreEqual(6, properties.Count);
            Assert.AreEqual(1, properties[0].Count);
            Assert.AreEqual("Order", properties[0][0].Name);
            Assert.AreEqual(2, properties[1].Count);
            Assert.AreEqual("Order", properties[1][0].Name);
            Assert.AreEqual("Customer", properties[1][1].Name);
            Assert.AreEqual(3, properties[2].Count);
            Assert.AreEqual("Order", properties[2][0].Name);
            Assert.AreEqual("Customer", properties[2][1].Name);
            Assert.AreEqual("Orders", properties[2][2].Name);
            Assert.AreEqual(4, properties[3].Count);
            Assert.AreEqual("Order", properties[3][0].Name);
            Assert.AreEqual("Customer", properties[3][1].Name);
            Assert.AreEqual("Orders", properties[3][2].Name);
            Assert.AreEqual("Id", properties[3][3].Name);
            Assert.AreEqual(4, properties[4].Count);
            Assert.AreEqual("Order", properties[4][0].Name);
            Assert.AreEqual("Customer", properties[4][1].Name);
            Assert.AreEqual("Orders", properties[4][2].Name);
            Assert.AreEqual("Date", properties[4][3].Name);
            Assert.AreEqual(4, properties[5].Count);
            Assert.AreEqual("Order", properties[5][0].Name);
            Assert.AreEqual("Customer", properties[5][1].Name);
            Assert.AreEqual("Orders", properties[5][2].Name);
            Assert.AreEqual("CustomerId", properties[5][3].Name);
        }

        [TestMethod]
        public void TestLINQJoinInto()
        {
            var properties = GetDependentProperties("LINQJoinInto");
            Assert.AreEqual(6, properties.Count);
            Assert.AreEqual(1, properties[0].Count);
            Assert.AreEqual("Orders", properties[0][0].Name);
            Assert.AreEqual(1, properties[1].Count);
            Assert.AreEqual("Orders2", properties[1][0].Name);
            Assert.AreEqual(2, properties[2].Count);
            Assert.AreEqual("Orders", properties[2][0].Name);
            Assert.AreEqual("Id", properties[2][1].Name);
            Assert.AreEqual(2, properties[3].Count);
            Assert.AreEqual("Orders2", properties[3][0].Name);
            Assert.AreEqual("Id", properties[3][1].Name);
            Assert.AreEqual(2, properties[4].Count);
            Assert.AreEqual("Orders", properties[4][0].Name);
            Assert.AreEqual("Date", properties[4][1].Name);
            Assert.AreEqual(2, properties[5].Count);
            Assert.AreEqual("Orders2", properties[5][0].Name);
            Assert.AreEqual("Date", properties[5][1].Name);
        }

        [TestMethod]
        public void TestLINQJoinIntoMethod()
        {
            var properties = GetDependentProperties("LINQJoinIntoMethod");
            Assert.AreEqual(6, properties.Count);
            Assert.AreEqual(1, properties[0].Count);
            Assert.AreEqual("Orders", properties[0][0].Name);
            Assert.AreEqual(1, properties[1].Count);
            Assert.AreEqual("Orders2", properties[1][0].Name);
            Assert.AreEqual(2, properties[2].Count);
            Assert.AreEqual("Orders", properties[2][0].Name);
            Assert.AreEqual("Id", properties[2][1].Name);
            Assert.AreEqual(2, properties[3].Count);
            Assert.AreEqual("Orders2", properties[3][0].Name);
            Assert.AreEqual("Id", properties[3][1].Name);
            Assert.AreEqual(2, properties[4].Count);
            Assert.AreEqual("Orders", properties[4][0].Name);
            Assert.AreEqual("Date", properties[4][1].Name);
            Assert.AreEqual(2, properties[5].Count);
            Assert.AreEqual("Orders2", properties[5][0].Name);
            Assert.AreEqual("Date", properties[5][1].Name);
        }


        [TestMethod]
        public void TestTestAs()
        {
            var properties = GetDependentProperties("TestAs");
            Assert.AreEqual(1, properties.Count);
            Assert.AreEqual("SpecialOrders", properties[0][0].Name);
        }

        [TestMethod]
        public void TestGetHistoriqueTauxTvaDateDelivrance()
        {
            var properties = GetDependentProperties("GetHistoriqueTauxTvaDateDelivrance");
            Assert.AreEqual(8, properties.Count);
            Assert.IsTrue(properties.Any(ps => ps.Count == 1 && ps[0].Name == "BienEtServiceTarifie"));
            Assert.IsTrue(properties.Any(ps => ps.Count == 2 && ps[0].Name == "BienEtServiceTarifie" && ps[1].Name == "HistoriqueTvas"));
            Assert.IsTrue(properties.Any(ps => ps.Count == 3 && ps[0].Name == "BienEtServiceTarifie" && ps[1].Name == "HistoriqueTvas" && ps[2].Name == "DateDebut"));
            Assert.IsTrue(properties.Any(ps => ps.Count == 1 && ps[0].Name == "Prestation"));
            Assert.IsTrue(properties.Any(ps => ps.Count == 2 && ps[0].Name == "Prestation" && ps[1].Name == "DateDelivrance"));
            Assert.IsTrue(properties.Any(ps => ps.Count == 3 && ps[0].Name == "BienEtServiceTarifie" && ps[1].Name == "HistoriqueTvas" && ps[2].Name == "CodeTva"));
            Assert.IsTrue(properties.Any(ps => ps.Count == 4 && ps[0].Name == "BienEtServiceTarifie" && ps[1].Name == "HistoriqueTvas" && ps[2].Name == "CodeTva" && ps[3].Name == "HistoriqueTauxTvas"));
            Assert.IsTrue(properties.Any(ps => ps.Count == 5 && ps[0].Name == "BienEtServiceTarifie" && ps[1].Name == "HistoriqueTvas" && ps[2].Name == "CodeTva" && ps[3].Name == "HistoriqueTauxTvas" && ps[4].Name == "DateDebut"));
        }

        [TestMethod]
        public void TestGetTauxTvaDateDelivrance()
        {
            var properties = GetDependentProperties("GetTauxTvaDateDelivrance");
            Assert.AreEqual(9, properties.Count);
            Assert.IsTrue(properties.Any(ps => ps.Count == 1 && ps[0].Name == "BienEtServiceTarifie"));
            Assert.IsTrue(properties.Any(ps => ps.Count == 2 && ps[0].Name == "BienEtServiceTarifie" && ps[1].Name == "HistoriqueTvas"));
            Assert.IsTrue(properties.Any(ps => ps.Count == 3 && ps[0].Name == "BienEtServiceTarifie" && ps[1].Name == "HistoriqueTvas" && ps[2].Name == "DateDebut"));
            Assert.IsTrue(properties.Any(ps => ps.Count == 1 && ps[0].Name == "Prestation"));
            Assert.IsTrue(properties.Any(ps => ps.Count == 2 && ps[0].Name == "Prestation" && ps[1].Name == "DateDelivrance"));
            Assert.IsTrue(properties.Any(ps => ps.Count == 3 && ps[0].Name == "BienEtServiceTarifie" && ps[1].Name == "HistoriqueTvas" && ps[2].Name == "CodeTva"));
            Assert.IsTrue(properties.Any(ps => ps.Count == 4 && ps[0].Name == "BienEtServiceTarifie" && ps[1].Name == "HistoriqueTvas" && ps[2].Name == "CodeTva" && ps[3].Name == "HistoriqueTauxTvas"));
            Assert.IsTrue(properties.Any(ps => ps.Count == 5 && ps[0].Name == "BienEtServiceTarifie" && ps[1].Name == "HistoriqueTvas" && ps[2].Name == "CodeTva" && ps[3].Name == "HistoriqueTauxTvas" && ps[4].Name == "DateDebut"));
            Assert.IsTrue(properties.Any(ps => ps.Count == 5 && ps[0].Name == "BienEtServiceTarifie" && ps[1].Name == "HistoriqueTvas" && ps[2].Name == "CodeTva" && ps[3].Name == "HistoriqueTauxTvas" && ps[4].Name == "Taux"));
        }
    }
}
