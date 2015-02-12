using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary1
{
    public static class Class1
    {
        public static void None(this Customer c)
        {
        }

        public static Customer Get(this Customer c)
        {
            return c;
        }

        public static string Simple(this Customer c)
        {
            return c.CompanyName;
        }

        public static string Double(this Customer c)
        {
            return c.CompanyName + " - " + c.ContactName;
        }

        public static DateTime Depth2(this OrderDetail od)
        {
            return od.Order.Date;
        }

        public static string Depth3(this OrderDetail od)
        {
            return od.Order.Customer.CompanyName;
        }

        public static double Lamda(this Order o)
        {
            return o.OrderDetails.Sum(od => od.UnitPrice * od.Quantity * (1 - od.Discount));
        }

        public static double LambdaLambda(this Customer c)
        {
            return c.Orders.Sum(o => o.OrderDetails.Sum(od => od.UnitPrice * od.Quantity * (1 - od.Discount)));
        }

        public static Customer GetCustomer(this OrderDetail od)
        {
            return od.Order.Customer;
        }

        public static void CallMethod(this OrderDetail od)
        {
            var test = GetCustomer(od).CompanyName;
        }

        public static void CallMethod2(this Order o)
        {
            var test = GetCustomer(o.OrderDetails.First()).CompanyName;
        }

        public static void Variable(this OrderDetail od)
        {
            var o = od.Order;
            var c = o.Customer;
        }

        public static void LINQSelect(this Order o)
        {
            var test = from od in o.OrderDetails
                       select od.UnitPrice * od.Quantity;
        }

        public static void LINQWhere(this Order o)
        {
            var test = from od in o.OrderDetails
                       where od.UnitPrice * od.Quantity > 100
                       select od;
        }

        public static void LINQWhereAndSelect(this Order o)
        {
            var test = from od in o.OrderDetails
                       where od.UnitPrice * od.Quantity > 100
                       select od.Discount + od.UnitPrice;
        }

        public static void LINQOrderBy(this Order o)
        {
            var test = from od in o.OrderDetails
                       orderby od.UnitPrice * od.Quantity > 100, od.Order.Date descending
                       select od;
        }

        public static void LINQOrderByAndSelect(this Order o)
        {
            var test = from od in o.OrderDetails
                       orderby od.UnitPrice * od.Quantity > 100, od.Order.Date descending
                       select od.Discount + od.UnitPrice;
        }

        public static void LINQLet(this Order o)
        {
            var test = from od in o.OrderDetails
                       let order = od.Order
                       where order.Customer == o.Customer
                       select new { order.Date, od.UnitPrice };
        }

        public static void LINQGroupBy(this Customer c)
        {
            var test = from o in c.Orders
                       group o by o.Date
                           into g
                           select new { Date = g.Key, Total = g.Sum(o => Lamda(o)), Customer = g.First().Customer };
            var test2 = test.First().Customer.CompanyName;
        }

        public static void LINQGroupBy2(this Order o)
        {
            var test = from od in o.OrderDetails
                       group od by od.Order.Customer into g
                       select new { CompanyName = g.Key.CompanyName, Total = g.Count(od => od.Quantity > 10) };
        }

        public static void LINQDoubleFrom(this Customer c)
        {
            var test = from o in c.Orders
                       from od in o.OrderDetails
                       select od.Quantity;
        }

        public static void LINQJoin(this Customer c)
        {
            var test = from o in c.Orders
                       join o2 in c.Orders2 on o.Id equals o2.Id
                       select new { o.Date, Date2 = o2.Date };
        }

        public static void LINQSelectMethod(this Order o)
        {
            var test = o.OrderDetails.Select(od => od.UnitPrice * od.Quantity);
        }

        public static void LINQSelectMethod2(this Order o)
        {
            var test = o.OrderDetails.Select(od => (int?)od.OrderId).First().Value;
        }

        public static void LINQWhereMethod(this Order o)
        {
            var test = o.OrderDetails.Where(od => od.UnitPrice * od.Quantity > 100);
        }

        public static void LINQWhereAndSelectMethod(this Order o)
        {
            var test = o.OrderDetails.Where(od => od.UnitPrice * od.Quantity > 100).Select(od => od.Discount + od.UnitPrice);
        }

        public static void LINQOrderByMethod(this Order o)
        {
            var test = o.OrderDetails.OrderBy(od => od.UnitPrice * od.Quantity > 100)
                .ThenByDescending(od => od.Order.Date);
        }

        public static void LINQOrderByMethod2(this Order o)
        {
            var test = o.OrderDetails.OrderByDescending(od => od.UnitPrice * od.Quantity > 100)
                .ThenBy(od => od.Order.Date);
        }

        public static void LINQOrderByAndSelectMethod(this Order o)
        {
            var test =
                o.OrderDetails.OrderBy(od => od.UnitPrice * od.Quantity > 100)
                    .ThenByDescending(od => od.Order.Date)
                    .Select(od => od.Discount + od.UnitPrice);
        }

        public static void LINQLetMethod(this Order o)
        {
            var test =
                o.OrderDetails.Select(od => new { OrderDetail = od, Order = od.Order })
                    .Where(od => od.Order.Customer == o.Customer)
                    .Select(od => new { od.Order.Date, od.OrderDetail.UnitPrice });
        }

        public static void LINQGroupByMethod(this Customer c)
        {
            var test =
                c.Orders.GroupBy(o => o.Date)
                    .Select(g => new { Date = g.Key, Total = g.Sum(o => Lamda(o)), Customer = g.First().Customer });
            var test2 = test.First().Customer.CompanyName;
        }

        public static void LINQDoubleFromMethod(this Customer c)
        {
            var test = c.Orders.SelectMany(o => o.OrderDetails).Select(od => od.Quantity);
        }

        public static void LINQJoinMethod(this Customer c)
        {
            var test = c.Orders.Join(c.Orders2, o => o.Id, o => o.Id, (o, o2) => new { o.Date, Date2 = o2.Date });
        }

        public static C GetC(OrderDetail od)
        {
            return new C { Id = od.Id, OrderId = od.OrderId, OrderDetail = od };
        }

        public static IEnumerable<C> GetCs(IEnumerable<OrderDetail> ods)
        {
            return ods.Select(od => GetC(od));
        }

        public static void TestGetCs(Order o)
        {
            foreach (var c in GetCs(o.OrderDetails))
            {
                var test = c.Id;
                var test2 = c.OrderDetail.Quantity;
            }
        }

        public static void TestGetCs2(Order o)
        {
            var values = GetCs(o.OrderDetails);
            foreach (var c in values)
            {
                var test = c.Id;
                var test2 = c.OrderDetail.Quantity;
            }
            foreach (var _ in values.Select(c => c.OrderDetail.Order)) ;
        }

        public static IEnumerable<Order> GetOrders(this Customer c)
        {
            if (c.Id != 0)
                return c.Orders.Where(o =>
                {
                    return o.Date.Year == 2000;
                });
            return c.Orders2.SelectMany(o => o.OrderDetails).Select(od => od.Order);
        }

        public static void TestReturnMethodWithLINQ(this Customer c)
        {
            var test = GetOrders(c).Select(o => o.Id);
        }

        public static C GetC2(this Customer c, Order o)
        {
            if (c.Id > 10)
                return new C { Id = c.Id, OrderId = o.Id, OrderDetail = c.Orders.First().OrderDetails.First() };
            return new C { Id = c.Id, OrderId = c.Orders2.First().Id, OrderDetail = c.Orders2.First().OrderDetails.First() };
        }

        public static void TestGetC2(Order o)
        {
            var test = GetC2(o.Customer, o).OrderDetail.UnitPrice;
        }

        public static void TestUnion(Customer c)
        {
            var test = c.Orders.Union(c.Orders2).Select(o => o.Id);
        }

        public static IEnumerable<Order> GetCustomer(Order o)
        {
            if (o == null)
                return null;
            return o.Customer.Orders.Where(o2 => o2.Id > 10);
        }

        public static void CallTwice(OrderDetail od)
        {
            var test = GetCustomer(od.Order).Where(o => o.Date.Year == 2000 && o.CustomerId > 10);
            var test2 = GetCustomer(od.Order).Where(o => o.Date.Year == 2000 && o.CustomerId > 10);
        }

        public static void LINQJoinInto(Customer c)
        {
            var q = from o in c.Orders
                    join o2 in c.Orders2 on o.Id equals o2.Id into os
                    from o2 in os.DefaultIfEmpty()
                    select new { o.Date, Date2 = o2.Date };
        }

        public static void LINQJoinIntoMethod(Customer c)
        {
            var q = c.Orders.GroupJoin(c.Orders2, o => o.Id, o2 => o2.Id, (o, o2s) => new { O = o, Os = o2s }).SelectMany(oo => oo.Os.DefaultIfEmpty(), (o, o2) => new {OrderOrder = o, Order2 = o2 }).Select(oo => new { oo.OrderOrder.O.Date, Date2 = oo.Order2.Date });
        }

        public static void TestAs(this Customer c)
        {
            int i = 0;
            var vipCustomer = c as VIPCustomer;
            if (vipCustomer != null)
                i += vipCustomer.SpecialOrders.Count;
        }

        public static HistoriqueTauxTva GetHistoriqueTauxTvaDateDelivrance(this PrestationElementaire pel)
        {
            return GetHistoriqueTvaDateDelivrance(pel) == null
                ? null
                : GetHistoriqueTvaDateDelivrance(pel)
                               .CodeTva
                               .HistoriqueTauxTvas
                               .Where(h => h.DateDebut <= pel.Prestation.DateDelivrance)
                               .OrderByDescending(h => h.DateDebut)
                               .FirstOrDefault();
        }

        public static HistoriqueTva GetHistoriqueTvaDateDelivrance(this PrestationElementaire pel)
        {
            return pel.BienEtServiceTarifie != null
                ? pel
                    .BienEtServiceTarifie
                    .HistoriqueTvas
                    .Where(h => h.DateDebut <= pel.Prestation.DateDelivrance)
                    .OrderByDescending(h => h.DateDebut)
                    .FirstOrDefault()
                : null;
        }

        public static int? GetQteACder(this Bes_Asso_Fou baf)
        {
            return baf == null || baf.BienEtService == null ? null :
                 (int?)(baf.BienEtService.BesoinCommandes.Where(bc => !bc.IdLigneCommande.HasValue
                  && bc.IdFournisseur == baf.IdFournisseur)
                 .Sum(bc => bc.QuantiteACommander * bc.BienEtService.CoefNbBoitesEnNbUnites)
              + baf.BienEtService.LigneCommandes.Where(lc => lc.Commande != null
                 && lc.Commande.IdFournisseur == baf.IdFournisseur
                 && (lc.Commande.IdEtatCommande == 1
                 || lc.Commande.IdEtatCommande == 2
                 || lc.Commande.IdEtatCommande == 3))
                 .Sum(l => l.QuantiteCommandee * l.BienEtService.CoefNbBoitesEnNbUnites));
        }

        public static OrderDetail GetFirstOD(this Order o)
        {
            return o.OrderDetails.First();
        }

        public static double GetQuantite2(this OrderDetail od)
        {
            return od.Quantity;
        }

        public static double GetFirsOrderQuantity(this Order o)
        {
            return GetQuantite2(GetFirstOD(o));
        }


        public static IEnumerable<Order> GetFirsOrderQuantity(this Customer c)
        {
            return c.Orders.Where(o => GetQuantite2(GetFirstOD(o)) > 10);
        }


        public static IEnumerable<Order> GetFirsOrderQuantity2(this Customer c)
        {
            return c.Orders.Where(o => GetQuantite2(GetFirstOD(o)) > 10);
        }


        public static IEnumerable<Order> GetFirsOrderQuantity3(this Customer c)
        {
            return c.Orders.Where(o => GetQuantite2(GetFirstOD(o)) > GetQuantite2(c.Orders.First().OrderDetails.First()));
        }

        public static IEnumerable<KeyValuePair<string, OrderDetail>> TestOnDictionary(Dictionary<string, OrderDetail> dico)
        {
            return dico.Where(d => GetQuantite2(d.Value) > 10);
        }

        public static IEnumerable<KeyValuePair<string, OrderDetail>> TestOnDictionary2(Dictionary<string, OrderDetail> dico)
        {
            return dico.Where(d => d.Value.Order.Date.Year > 2000);
        }

        public static int? TestOnConditionalAccessExpression(this Order o)
        {
            return o.Customer?.CompanyName?.Length;
        }
    }

    public class Customer
    {
        public int Id { get; set; }
        public string CompanyName { get; set; }
        public string ContactName { get; set; }
        public List<Order> Orders { get; set; }
        public List<Order> Orders2 { get; set; }
    }

    public class VIPCustomer : Customer
    {
        public List<SpecialOrder> SpecialOrders { get; set; }
    }

    public class SpecialOrder : Order
    {
        public VIPCustomer VIPCustomer { get; set; }
    }

    public class Order
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public Customer Customer { get; set; }
        public List<OrderDetail> OrderDetails { get; set; }
        public int CustomerId { get; set; }
    }

    public class OrderDetail
    {
        public int Id { get; set; }
        public double Quantity { get; set; }
        public double UnitPrice { get; set; }
        public double Discount { get; set; }
        public int OrderId { get; set; }
        public Order Order { get; set; }
    }

    public class C
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public OrderDetail OrderDetail { get; set; }
    }

    public class HistoriqueTauxTva
    {
        public System.DateTime DateDebut { get; set; }
    }

    public class Prestation
    {
        public DateTime? DateDelivrance { get; set; }
    }

    public class PrestationElementaire
    {
        public Prestation Prestation { get; set; }
        public BienEtService BienEtServiceTarifie { get; set; }
    }

    public class BienEtService
    {
        public List<HistoriqueTva> HistoriqueTvas { get; set; }
        public List<BesoinCommande> BesoinCommandes { get; set; }
        public List<LigneCommande> LigneCommandes { get; set; }
        public int CoefNbBoitesEnNbUnites { get; set; }
    }

    public class HistoriqueTva
    {
        public CodeTva CodeTva { get; set; }
        public System.DateTime DateDebut { get; set; }
    }

    public class CodeTva
    {
        public List<HistoriqueTauxTva> HistoriqueTauxTvas { get; set; }
    }

    public class Bes_Asso_Fou
    {
        public BienEtService BienEtService { get; set; }
        public int IdFournisseur { get; set; }
    }

    public class BesoinCommande
    {
        public int? IdLigneCommande { get; set; }
        public int IdFournisseur { get; set; }
        public int QuantiteACommander { get; set; }
        public BienEtService BienEtService { get; set; }
    }

    public class LigneCommande
    {
        public Commande Commande { get; set; }
        public BienEtService BienEtService { get; set; }
        public int QuantiteCommandee { get; set; }
    }

    public class Commande
    {
        public int IdFournisseur { get; set; }
        public int IdEtatCommande { get; set; }
    }
}
