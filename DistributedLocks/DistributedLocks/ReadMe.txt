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
Implement Locking
	This implementation uses Postgresql feature of Advisory Locks
	
	ref: 9.24. System Administration Functions
		https://www.postgresql.org/docs/9.1/functions-admin.html
		pg_advisory_lock locks an application-defined resource, which can be identified either by a single 64-bit key value or two 32-bit key values (note that these two key spaces do not overlap). If another session already holds a lock on the same resource identifier, this function will wait until the resource becomes available. The lock is exclusive. Multiple lock requests stack, so that if the same resource is locked three times it must then be unlocked three times to be released for other sessions' use.
		pg_advisory_lock_shared works the same as pg_advisory_lock, except the lock can be shared with other sessions requesting shared locks. Only would-be exclusive lockers are locked out.
		pg_advisory_unlock will release a previously-acquired exclusive session level advisory lock. It returns true if the lock is successfully released. If the lock was not held, it will return false, and in addition, an SQL warning will be reported by the server.
		pg_advisory_unlock_shared works the same as pg_advisory_unlock, except it releases a shared session level advisory lock.

	ref: What are Postgres advisory locks and their use cases
		https://blog.devgenius.io/what-are-postgres-advisory-locks-and-their-use-cases-71ace601e06b

	Some notes on Advisory Locks

	(#) Once acquired at session level, an advisory lock is held until explicitly released or the session ends.
		Session-level locks (pg_advisory_lock function) do not depend on current transactions 
		and are held until they are unlocked manually (with pg_advisory_unlock function) or at the end of the session.

		Note: %%
			I'm not sure in terms of EF Core how a session varies viz-a-viz a connection
			But we'll find out by debugging ..

	(#) PG commands
		Shared Locks: SELECT pg_advisory_lock_shared(1)
		Exclusive lock: SELECT pg_advisory_lock(1)

	Elsewhere:
		ref: Tutorial: Handle concurrency - ASP.NET MVC with EF Core
		https://learn.microsoft.com/en-us/aspnet/core/data/ef-mvc/concurrency?view=aspnetcore-8.0
	Pessimistic concurrency (locking)
		If your application does need to prevent accidental data loss in concurrency scenarios, 
		one way to do that is to use database locks. This is called pessimistic concurrency. 
		For example, before you read a row from a database, you request a lock for read-only or for update access. 
		If you lock a row for update access, no other users are allowed to lock the row either for read-only or update access, 
		because they would get a copy of data that's in the process of being changed. 
		If you lock a row for read-only access, others can also lock it for read-only access but not for update.

		Not all database management systems support pessimistic concurrency. 
		Entity Framework Core provides no built-in support for it.

	Elsewhere:
		ref: CRUD operations on PostgreSQL using C# and Npgsql
			https://www.code4it.dev/blog/postgres-crud-operations-npgsql/

	My attempts with running SELECT pg_advisory_lock(1) etc using EFCore DBContext didn't work, even when I kept the same DBContext for the entire lock duration
	Not sure why, but EFCore logging showed Connection opened/Connection closed before and after running each query
	I would assume, due to this the session is over after each call, and therefore the locks get implicitly released.
	These attempts are in v1 and v2

	Anyhow, I finally used raw NpgsqlConnection etc to run queries, and it worked
-----------------------------------------
Summary:
	With Postgresql server running on my local machine, the timing to acquire/release lock is varying from 3 ms to 7 ms
	This is pretty good for a production deployment.
	Therefore, need to test this with AWS RDS
-----------------------------------------
-----------------------------------------
-----------------------------------------
-----------------------------------------
-----------------------------------------
-----------------------------------------
-----------------------------------------



