﻿using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Terminals.Connections;
using Terminals.Data;
using DB = Terminals.Data.DB;
using Favorite = Terminals.Data.DB.Favorite;

namespace Tests
{
    /// <summary>
    ///This is a test class for database implementation of Favorites
    ///</summary>
    [TestClass]
    public class SqlFavoritesTest
    {
        private SqlTestsLab lab;

        private int addedCount;
        private int updatedCount;
        private int deletedCount;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestInitialize()
        {
            this.lab = new SqlTestsLab();
            this.lab.InitializeTestLab();
            this.lab.Persistence.Dispatcher.FavoritesChanged += new FavoritesChangedEventHandler(Dispatcher_FavoritesChanged);
        }

        [TestCleanup]
        public void TestClose()
        {
            this.lab.ClearTestLab();
        }

        private void Dispatcher_FavoritesChanged(FavoritesChangedEventArgs args)
        {
            addedCount += args.Added.Count;
            deletedCount += args.Removed.Count;
            updatedCount += args.Updated.Count;
        }

        /// <summary>
        ///A test for Add
        ///</summary>
        [TestMethod]
        public void AddFavoriteTest()
        {
            Favorite favorite = this.lab.CreateTestFavorite();
            Favorite favorite2 = this.lab.CreateTestFavorite();
            int before = this.lab.CheckDatabase.Favorites.Count();
            this.lab.Persistence.Favorites.Add(favorite);
            this.lab.Persistence.Favorites.Add(favorite2);

            int after = this.lab.CheckDatabase.Favorites.Count();
            string protocolProperties = this.lab.CheckDatabase.GetFavoriteProtocolProperties(favorite.Id).FirstOrDefault();
            IFavorite checkFavorite = this.lab.SecondaryPersistence.Favorites.FirstOrDefault();

            Assert.AreNotEqual(before, after, -2, "Favorites didnt reach the database");
            Assert.IsTrue(!string.IsNullOrEmpty(protocolProperties), "Protocol properties are null");
            Assert.IsNotNull(checkFavorite.Security, "Security is null");
            Assert.IsNotNull(checkFavorite.Display, "Display is null");
            Assert.IsNotNull(checkFavorite.ExecuteBeforeConnect, "ExecuteBeforeConnect is null");
            Assert.AreEqual(2, addedCount, "Event wasnt delivered");
        }

        /// <summary>
        ///A test for Delete
        ///</summary>
        [TestMethod]
        public void DeleteFavoriteTest()
        {
            Favorite favorite = this.lab.AddFavoriteToPrimaryPersistence();
            this.lab.Persistence.Favorites.Delete(favorite);

            int after = this.lab.CheckDatabase.Favorites.Count();
            Assert.AreEqual(0, after, "Favorite wasnt deleted");
            int displayOptions = this.lab.CheckDatabase.DisplayOptions.Count();
            Assert.AreEqual(0, displayOptions, "DisplayOptions wasnt deleted");
            int security = this.lab.CheckDatabase.Security.Count();
            Assert.AreEqual(0, security, "Security wasnt deleted");
            int execute = this.lab.CheckDatabase.BeforeConnectExecute.Count();
            Assert.AreEqual(0, execute, "BeforeConnectExecute wasnt deleted");
            Assert.AreEqual(1, deletedCount, "Event wasnt delivered");
        }

        /// <summary>
        ///A test for Update
        ///</summary>
        [TestMethod]
        public void UpdateFavoriteTest()
        {
            IFavorite favorite = this.lab.AddFavoriteToPrimaryPersistence();
            favorite.Protocol = ConnectionManager.VNC;
            favorite.Display.Colors = Terminals.Colors.Bits24;
            this.lab.Persistence.Favorites.Update(favorite);

            IFavorite target = this.lab.SecondaryPersistence.Favorites.FirstOrDefault();
            Assert.IsTrue(target.Protocol == ConnectionManager.VNC, "Protocol wasnt updated");
            Assert.IsTrue(target.Display.Colors == Terminals.Colors.Bits24, "Colors property wasnt updated");

            var testOptions = target.ProtocolProperties as VncOptions;
            Assert.IsNotNull(testOptions, "Protocol properties werent updated");
            Assert.AreEqual(1, updatedCount, "Event wasnt delivered");
        }

        /// <summary>
        ///A test for UpdateFavorite
        ///</summary>
        [TestMethod]
        public void UpdateFavoriteWithGroupsTest()
        {
            IFavorite favorite = this.lab.AddFavoriteToPrimaryPersistence();
            IFactory labFactory = this.lab.Persistence.Factory;
            IGroup groupToDelete = labFactory.CreateGroup("TestGroupToDelete");
            this.lab.Persistence.Groups.Add(groupToDelete);
            this.lab.Persistence.Favorites.UpdateFavorite(favorite, new List<IGroup> { groupToDelete });
            IGroup groupToAdd = labFactory.CreateGroup("TestGroupToAdd");
            this.lab.Persistence.Favorites.UpdateFavorite(favorite, new List<IGroup> { groupToAdd });

            Favorite checkFavorite = this.lab.CheckDatabase.Favorites.FirstOrDefault();
            Assert.AreEqual(1, checkFavorite.Groups.Count, "Child group is missing");
            DB.Group group = checkFavorite.Groups.FirstOrDefault();
            Assert.IsTrue(group.Name == "TestGroupToAdd", "wrong merge of groups");
            int targetGroupsCount = this.lab.CheckDatabase.Groups.Count();
            Assert.AreEqual(1, targetGroupsCount, "Empty groups wern't deleted");
            Assert.AreEqual(2, updatedCount, "Event wasnt delivered");
        }

        [TestMethod]
        public void LoadSaveFavoriteIconsTest()
        {
            IFavorite favorite = this.lab.CreateTestFavorite();
            // try to access on not saved favorite
            favorite.ToolBarIconFile = @"Data\ControlPanel.png";
            Image favoriteIcon = favorite.ToolBarIconImage;
            this.lab.Persistence.Favorites.Add(favorite);
            Favorite checkFavorite = this.lab.CheckDatabase.Favorites.FirstOrDefault();

            Assert.IsNotNull(favoriteIcon, "Icon wasnt assigned successfully");
            Assert.IsNotNull(checkFavorite.ToolBarIconImage, "Icon didnt reach the database");
        }
    }
}