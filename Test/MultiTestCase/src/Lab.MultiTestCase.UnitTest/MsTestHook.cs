﻿// using Microsoft.VisualStudio.TestTools.UnitTesting;
//
// namespace Lab.MultiTestCase.UnitTest;
//
// [TestClass]
// public class MsTestHook
// {
//     [AssemblyCleanup]
//     public static void Cleanup()
//     {
//         TestInstanceManager.SetTestEnvironmentVariable();
//         var db = TestInstanceManager.EmployeeDbContextFactory.CreateDbContext();
//         if (db.Database.CanConnect())
//         {
//             db.Database.EnsureDeleted();
//         }
//     }
//
//     [AssemblyInitialize]
//     public static void Setup(TestContext context)
//     {
//         TestInstanceManager.SetTestEnvironmentVariable();
//         var db = TestInstanceManager.EmployeeDbContextFactory.CreateDbContext();
//         if (db.Database.CanConnect())
//         {
//             db.Database.EnsureDeleted();
//         }
//
//         db.Database.EnsureCreated();
//     }
// }