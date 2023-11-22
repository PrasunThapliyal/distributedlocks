22 Nov 2023
===========

Implement Distributed Locks - For use in a micro-service environment
	Use Postgresql Advisory Locks

-----------------------------------------
Initial Commit
	Create new ASP.NET Core Web API using dotnet 6
-----------------------------------------
Add EFCore
	Add DB Context
-----------------------------------------
Add Migration to create DB
	Since we are planning to use Advisory Locks, there should be no need for a table
	However, advisory locks work on an integer value, and we are going to create a table just to create a map of Guid (Entitiy Id) to Long (Advisory Lock Id)

-----------------------------------------
-----------------------------------------
-----------------------------------------
-----------------------------------------
-----------------------------------------
-----------------------------------------
-----------------------------------------
-----------------------------------------
-----------------------------------------



