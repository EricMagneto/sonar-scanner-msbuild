﻿/*
 * SonarQube Scanner for MSBuild
 * Copyright (C) 2016-2017 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using Microsoft.Build.Construction;
using Microsoft.Build.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using TestUtilities;

namespace SonarQube.MSBuild.Tasks.IntegrationTests.TargetsTests
{
    [TestClass]
    public class SonarIntegrationTargetsTests
    {
        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestInitialize()
        {
            HackForVs2017Update3.Enable();
        }

        #region Tests

        [TestMethod]
        [Description("Checks the properties are not set if the temp folder is not set")]
        public void IntTargets_TempFolderIsNotSet()
        {
            // Arrange
            string rootInputFolder = TestUtils.CreateTestSpecificFolder(this.TestContext, "Inputs");
            ProjectRootElement projectRoot = BuildUtilities.CreateValidProjectRoot(this.TestContext, rootInputFolder, null);

            // Act
            ProjectInstance projectInstance = new ProjectInstance(projectRoot.FullPath);

            // Assert
            BuildAssertions.AssertPropertyDoesNotExist(projectInstance, TargetProperties.SonarQubeOutputPath);
            BuildAssertions.AssertPropertyDoesNotExist(projectInstance, TargetProperties.SonarQubeConfigPath);
        }

        [TestMethod]
        [Description("Checks the SonarQube paths are not set when the TeamBuild build directories are missing")]
        public void IntTargets_SonarPaths_TeamBuildBuildDirNotSet()
        {
            // Arrange
            string rootInputFolder = TestUtils.CreateTestSpecificFolder(this.TestContext, "Inputs");

            WellKnownProjectProperties preImportProperties = new WellKnownProjectProperties
            {
                TeamBuildLegacyBuildDirectory = "",
                TeamBuild2105BuildDirectory = ""
            };
            ProjectRootElement projectRoot = BuildUtilities.CreateValidProjectRoot(this.TestContext, rootInputFolder, preImportProperties);

            // Act
            ProjectInstance projectInstance = new ProjectInstance(projectRoot.FullPath);

            // Assert
            BuildAssertions.AssertPropertyDoesNotExist(projectInstance, TargetProperties.SonarQubeOutputPath);
            BuildAssertions.AssertPropertyDoesNotExist(projectInstance, TargetProperties.SonarQubeConfigPath);
        }

        [TestMethod]
        [Description("Checks the SonarQube paths are set correctly when the legacy TeamBuild directory is provided")]
        public void IntTargets_SonarPaths_TeamBuildPropertySet_Legacy()
        {
            // Arrange
            string rootInputFolder = TestUtils.CreateTestSpecificFolder(this.TestContext, "Inputs");

            WellKnownProjectProperties preImportProperties = new WellKnownProjectProperties
            {
                SonarQubeTempPath = @"t:\TeamBuildDir_Legacy\.sonarqube",
                TeamBuildLegacyBuildDirectory = @"t:\TeamBuildDir_Legacy",
                TeamBuild2105BuildDirectory = ""
            };
            ProjectRootElement projectRoot = BuildUtilities.CreateValidProjectRoot(this.TestContext, rootInputFolder, preImportProperties);

            // Act
            ProjectInstance projectInstance = new ProjectInstance(projectRoot.FullPath);

            // Assert
            BuildAssertions.AssertExpectedPropertyValue(projectInstance, TargetProperties.SonarQubeOutputPath, @"t:\TeamBuildDir_Legacy\.sonarqube\out");
            BuildAssertions.AssertExpectedPropertyValue(projectInstance, TargetProperties.SonarQubeConfigPath, @"t:\TeamBuildDir_Legacy\.sonarqube\conf");
        }

        [TestMethod]
        [Description("Checks the SonarQube paths are set correctly when the new TeamBuild build directory is provided")]
        public void IntTargets_SonarPaths_TeamBuildPropertySet_NonLegacy()
        {
            // Arrange
            string rootInputFolder = TestUtils.CreateTestSpecificFolder(this.TestContext, "Inputs");

            WellKnownProjectProperties preImportProperties = new WellKnownProjectProperties
            {
                SonarQubeTempPath = @"t:\TeamBuildDir_NonLegacy\.sonarqube", // FIXME
                TeamBuildLegacyBuildDirectory = "",
                TeamBuild2105BuildDirectory = @"t:\TeamBuildDir_NonLegacy"
            };
            ProjectRootElement projectRoot = BuildUtilities.CreateValidProjectRoot(this.TestContext, rootInputFolder, preImportProperties);

            // Act
            ProjectInstance projectInstance = new ProjectInstance(projectRoot.FullPath);

            // Assert
            BuildAssertions.AssertExpectedPropertyValue(projectInstance, TargetProperties.SonarQubeOutputPath, @"t:\TeamBuildDir_NonLegacy\.sonarqube\out");
            BuildAssertions.AssertExpectedPropertyValue(projectInstance, TargetProperties.SonarQubeConfigPath, @"t:\TeamBuildDir_NonLegacy\.sonarqube\conf");
        }

        [TestMethod]
        [Description("Checks the SonarQube paths are set correctly when the SonarQubeTempPath property is provided")]
        public void IntTargets_SonarPaths_TempPathSet()
        {
            // Arrange
            string rootInputFolder = TestUtils.CreateTestSpecificFolder(this.TestContext, "Inputs");

            WellKnownProjectProperties preImportProperties = new WellKnownProjectProperties
            {
                SonarQubeTempPath = @"c:\sonarQTemp",
                TeamBuildLegacyBuildDirectory = @"t:\Legacy TeamBuildPath\", // SonarQubeTempPath setting should take precedence
                TeamBuild2105BuildDirectory = @"x:\New Team Build Path\"
            };

            ProjectRootElement projectRoot = BuildUtilities.CreateValidProjectRoot(this.TestContext, rootInputFolder, preImportProperties);

            // Act
            ProjectInstance projectInstance = new ProjectInstance(projectRoot.FullPath);

            // Assert
            BuildAssertions.AssertExpectedPropertyValue(projectInstance, TargetProperties.SonarQubeOutputPath, @"c:\sonarQTemp\out");
            BuildAssertions.AssertExpectedPropertyValue(projectInstance, TargetProperties.SonarQubeConfigPath, @"c:\sonarQTemp\conf");
        }

        [TestMethod]
        [Description("Tests that the explicit property values for the output and config paths are used if supplied")]
        public void IntTargets_SonarPaths_OutputAndConfigPathsAreSet()
        {
            // The SonarQubeTempPath and TeamBuild paths should be ignored if the output and config are set explicitly

            // Arrange
            string rootInputFolder = TestUtils.CreateTestSpecificFolder(this.TestContext, "Inputs");

            WellKnownProjectProperties preImportProperties = new WellKnownProjectProperties
            {
                SonarQubeOutputPath = @"c:\output",
                SonarQubeConfigPath = @"c:\config",
                SonarQubeTempPath = @"c:\sonarQTemp",
                TeamBuildLegacyBuildDirectory = @"t:\Legacy TeamBuildPath\",
                TeamBuild2105BuildDirectory = @"t:\New TeamBuildPath\"
            };

            ProjectRootElement projectRoot = BuildUtilities.CreateValidProjectRoot(this.TestContext, rootInputFolder, preImportProperties);

            // Act
            ProjectInstance projectInstance = new ProjectInstance(projectRoot.FullPath);

            // Assert
            BuildAssertions.AssertExpectedPropertyValue(projectInstance, TargetProperties.SonarQubeOutputPath, @"c:\output");
            BuildAssertions.AssertExpectedPropertyValue(projectInstance, TargetProperties.SonarQubeConfigPath, @"c:\config");
        }

        #endregion
    }
}
