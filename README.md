# WAQS documentation is available on [https://github.com/MatthieuMEZIL/waqs-workshops/wiki](https://github.com/MatthieuMEZIL/waqs-workshops/wiki)

# What is WAQS?
WAQS is super useful to speed up C# data centric software development.

With most of "classic" software development, too much time is lost on plumbing code.
This code is often the most complex and risky.
WAQS avoids the complexity generating the plumbing code.

Basically, WAQS could be used as
* a super layer on top of Entity Framework
* a remote Entity Framework context on steroids
* a way to simplify the business logic using intentional programming with some specifications written in C#

So WAQS is a Framework generator. In this sense, it’s better to say that it’s a "meta Framework". Indeed, WAQS massively uses meta-programming, using intensively T4 templates and Roslyn, in order to build a framework fitted with your domain.

WAQS gives you a better productivity with excellent performances, taking in charge technical aspect using well-tried patterns, as well as flexibility and a robust code for your business rules implementation. In this last point, WAQS approach is an efficient alternative or supplement to DDD conception.

You have the choice with WAQS: you can use specific parts or you can use it for all the layers of the application. WAQS is an “Opinionated Framework”: it makes some choices but let the flexibility of your architecture choices.

The initial idea of WAQS was to reproduce Entity Framework ObjectContext features on the client, so having the flexibility and the power of remotely querying your data and having transparent changes tracked in order to be able to persist them later in one transaction without thinking about it. 

WAQS extends Entity Framework possibilities in the server but also in the client (for 3-Tiers application). 

Thus, WAQS has evolved into a more comprehensive code generation solution that abstracts away the technical code in order to let developers focus on value add tasks: screens and business code.

 WAQS proposes:
* Querying from the client with an asynchronous way, 
* Changes transparent tracking and persistence of them from the client, 
* Improving Entity Framework coverage
* A solution to avoid « spaghetti » code and code duplication, particularly for business code, 
* Simplifies MVVM. 

With a manager point of view, WAQS allows: 
* To industrialize and homogenize development process with a flexible and customizable solution, 
* To improve a lot team productivity, 
* To improve reliability,
* To improve code readability and maintainability of the solution, 
* To reduce risks and costs. 

And this is possible
* Without fundamentally change developer habits, WAQS having been thought for them, 
* Without using a private tool or engine. WAQS only generates some code in the solution just using Microsoft tools in order to ensure the maximum of durability and reducing the risk. 
